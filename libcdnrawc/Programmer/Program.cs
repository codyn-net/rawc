																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																	using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class Program
	{
		private Options d_options;
		private List<Computation.INode> d_source;
		private List<Function> d_functions;
		private List<Tree.Embedding> d_embeddings;
		private List<Computation.INode> d_initialization;
		private List<Cdn.Function> d_usedCustomFunctions;
		private Dictionary<string, Function> d_functionMap;
		private List<State> d_updateStates;
		private Dictionary<DataTable.DataItem, State> d_integrateTable;
		private List<Computation.Loop> d_loops;
		private List<Computation.Loop> d_initLoops;
		private Dictionary<Tree.Embedding, Function> d_embeddingFunctionMap;
		private DataTable d_statetable;
		private DataTable d_delayedCounters;
		private DataTable d_delayedCountersSize;
		private List<DataTable> d_indexTables;
		private List<DataTable> d_delayHistoryTables;
		private Dictionary<DelayedState, DataTable> d_delayHistoryMap;
		private Dictionary<State, Tree.Node> d_equations;
		private List<DelayedState> d_delayedStates;

		public Program(Options options, IEnumerable<Tree.Embedding> embeddings, Dictionary<State, Tree.Node> equations)
		{
			// Write out equations and everything
			d_statetable = new DataTable("ss", true);

			d_functions = new List<Function>();
			d_embeddings = new List<Tree.Embedding>(embeddings);
			d_embeddingFunctionMap = new Dictionary<Tree.Embedding, Function>();
			d_source = new List<Computation.INode>();
			d_initialization = new List<Computation.INode>();
			d_usedCustomFunctions = new List<Cdn.Function>();
			d_functionMap = new Dictionary<string, Function>();
			d_updateStates = new List<State>();
			d_integrateTable = new Dictionary<DataTable.DataItem, State>();
			d_loops = new List<Computation.Loop>();
			d_initLoops = new List<Computation.Loop>();
			d_indexTables = new List<DataTable>();
			d_delayHistoryTables = new List<DataTable>();
			d_delayedStates = new List<DelayedState>();
			d_delayHistoryMap = new Dictionary<DelayedState, DataTable>();

			d_delayedCounters = new DataTable("delay_counters", true);
			d_delayedCounters.IntegerType = true;
			
			d_delayedCountersSize = new DataTable("delayed_counters_size", true);
			d_delayedCountersSize.IntegerType = true;
			d_delayedCountersSize.IsConstant = true;

			d_equations = equations;
			d_options = options;

			ProgramDataTables();

			ProgramFunctions();
			ProgramCustomFunctions();
			ProgramInitialization();
			ProgramSource();

			foreach (DataTable table in DataTables)
			{
				table.Lock();
			}
		}
		
		public DataTable StateTable
		{
			get
			{
				return d_statetable;
			}
		}
		
		public Options Options
		{
			get
			{
				return d_options;
			}
		}
		
		public Tree.Node Lookup(State state)
		{
			Tree.Node node = null;
			
			d_equations.TryGetValue(state, out node);
			return node;
		}
		
		public IEnumerable<Function> Functions
		{
			get
			{
				return d_functions;
			}
		}
		
		public Dictionary<DataTable.DataItem, State> IntegrateTable
		{
			get
			{
				return d_integrateTable;
			}
		}

		private void ProgramDataTables()
		{
			bool addedups = false;

			foreach (var state in Knowledge.Instance.States)
			{
				Variable v = state.Object as Variable;

				if (v != null &&
				    !v.Integrated &&
				    d_updateStates.Count != 0 && !addedups)
				{
					addedups = true;

					foreach (State update in d_updateStates)
					{
						d_statetable.Add(update).Type |= (DataTable.DataItem.Flags.State |
						                                  DataTable.DataItem.Flags.Integrated |
						                                  DataTable.DataItem.Flags.Update);
					}
				}

				DataTable.DataItem item = d_statetable.Add(state);

				if (v != null)
				{
					item.Type |= DataTable.DataItem.Flags.State;

					if (v.Integrated)
					{
						State update = new State(State.Flags.Update);
	
						d_updateStates.Add(update);
						d_integrateTable[d_statetable[state]] = update;
	
						item.Type |= DataTable.DataItem.Flags.Integrated;
					}
	
					if ((v.Flags & VariableFlags.In) != 0)
					{
						item.Type |= DataTable.DataItem.Flags.In;
					}
	
					if ((v.Flags & VariableFlags.Out) != 0)
					{
						item.Type |= DataTable.DataItem.Flags.Out;
					}

					if ((v.Flags & VariableFlags.Once) != 0)
					{
						item.Type |= DataTable.DataItem.Flags.Once;
					}
				}

				DelayedState ds = state as DelayedState;

				if (ds != null)
				{
					DelayedState.Size size = ds.Count;

					d_delayedCounters.Add(size).Type = DataTable.DataItem.Flags.Counter;
					d_delayedCounters.MaxSize = size - 1;

					d_delayedCountersSize.Add((uint)size).Type = DataTable.DataItem.Flags.Size;
					d_delayedCountersSize.MaxSize = size;

					var dt = new DataTable(String.Format("delay_{0}", d_delayHistoryTables.Count), true);

					d_delayHistoryTables.Add(dt);
					d_delayedStates.Add(ds);

					d_delayHistoryMap[ds] = dt;

					for (int i = 0; i < size; ++i)
					{
						dt.Add(new State(i, (Cdn.Expression)null, State.Flags.None)).Type |= DataTable.DataItem.Flags.Delayed;
					}
				}
			}
		}

		public DataTable DelayHistoryTable(DelayedState state)
		{
			DataTable ret;

			if (d_delayHistoryMap.TryGetValue(state, out ret))
			{
				return ret;
			}

			return null;
		}
		
		private string GenerateFunctionName(string templ)
		{
			int num = 0;
			string name = templ;
			
			while (d_functionMap.ContainsKey(name))
			{
				name = String.Format("{0}{1}", name, ++num);
			}
			
			return name;
		}
		
		private void ProgramFunctions()
		{
			// Generate functions for all the embeddings
			foreach (Tree.Embedding embedding in d_embeddings)
			{
				string name = GenerateFunctionName(String.Format("f_{0}", d_functions.Count + 1));
				Function function = new Function(name, embedding);
				
				foreach (Tree.Node instance in embedding.Instances)
				{
					instance.Instruction = new Instructions.Function(function);
				}

				Add(embedding, function);
			}
		}
		
		public IEnumerable<T> CollectInstructions<T>() where T : Cdn.Instruction
		{
			foreach (KeyValuePair<State, Tree.Node> eq in d_equations)
			{
				foreach (Tree.Node node in eq.Value.Collect<T>())
				{
					yield return (T)node.Instruction;
				}
			}
			
			foreach (Function function in d_functions)
			{
				foreach (Tree.Node node in function.Expression.Collect<T>())
				{
					yield return (T)node.Instruction;
				}
			}
		}

		private class CustomFunctionNode
		{
			public Tree.Node Node { get; set; }

			public List<Tree.Node> Nodes { get; set; }

			public CustomFunctionNode(Cdn.Function func)
			{
				Node = Tree.Node.Create(null, func.Expression.Instructions);
				Nodes = new List<Tree.Node>();
			}
		}
		
		private void CustomFunctionUsage(Tree.Node node, Dictionary<Cdn.Function, CustomFunctionNode> usage)
		{
			CustomFunctionNode lst;
			Cdn.Function f = ((Cdn.InstructionCustomFunction)node.Instruction).Function;
				
			if (!usage.TryGetValue(f, out lst))
			{
				lst = new CustomFunctionNode(f);
				usage[f] = lst;

				d_usedCustomFunctions.Add(f);

				// Recurse
				foreach (Tree.Node child in lst.Node.Collect<Cdn.InstructionCustomFunction>())
				{
					CustomFunctionUsage(child, usage);
				}
			}

			lst.Nodes.Add(node);
		}
		
		private void Add(Tree.Embedding embedding, Function function)
		{
			d_functions.Add(function);
			d_functionMap[function.Name] = function;
			d_embeddingFunctionMap[embedding] = function;
		}

		private void ProgramCustomFunctions()
		{			
			Dictionary<Cdn.Function, CustomFunctionNode > usage = new Dictionary<Cdn.Function, CustomFunctionNode>();
			
			// Calculate map from a custom function to the nodes that use that function
			foreach (KeyValuePair<State, Tree.Node> eq in d_equations)
			{
				foreach (Tree.Node node in eq.Value.Collect<Cdn.InstructionCustomFunction>())
				{
					CustomFunctionUsage(node, usage);
				}
			}
			
			// Check also in the generated function implementations
			foreach (Function function in d_functions)
			{
				foreach (Tree.Node node in function.Expression.Collect<Cdn.InstructionCustomFunction>())
				{
					CustomFunctionUsage(node, usage);
				}
			}

			// Foreach custom function that is used
			foreach (Cdn.Function function in usage.Keys)
			{
				// Create a new node for the custom function expression
				Tree.Node node = usage[function].Node;

				// Calculate all the paths to where the arguments for this function
				// are used in the expression. All arguments are implemented as properties
				List<Cdn.Variable> arguments = new List<Cdn.Variable>();
				
				foreach (Cdn.FunctionArgument arg in function.Arguments)
				{
					arguments.Add(function.Variable(arg.Name));
				}
				
				List<Tree.Embedding.Argument> args = new List<Tree.Embedding.Argument>();

				foreach (Tree.Node child in node.Collect<InstructionVariable>())
				{
					InstructionVariable prop = child.Instruction as InstructionVariable;
					int idx = arguments.IndexOf(prop.Variable);
					
					if (idx == -1)
					{
						continue;
					}

					// Create new embedding argument
					args.Add(new Tree.Embedding.Argument(child.RelPath(node), (uint)idx));
				}

				// Here it's a little messy, maybe can be improved. For now we create an
				// embedding for the custom function as if it's a normal function. Then
				// we create instances of that embedding for all the nodes where the custom
				// function is used.
				Tree.Embedding embedding = new Tree.Embedding(node, args);

				string name = GenerateFunctionName(String.Format("cf_{0}", function.Id.ToLower()));

				Function func = new Function(name, embedding, true);
				Add(embedding, func);

				d_embeddings.Add(embedding);
				
				foreach (Tree.Node nn in usage[function].Nodes)
				{
					embedding.Embed(nn);

					nn.Instruction = new Instructions.Function(func);
				}
			}
		}
		
		private class LoopData
		{
			public Tree.Embedding Embedding;
			public Function Function;
			public List<Tree.Node> Instances;
			public bool AllRoots;
			
			public LoopData(Tree.Embedding embedding, Function function)
			{
				Embedding = embedding;
				Function = function;
				Instances = new List<Tree.Node>();
				AllRoots = true;
			}
			
			public void Add(Tree.Node node)
			{
				Instances.Add(node);
				
				if (node.Parent != null)
				{
					AllRoots = false;
				}
			}
		}
		
		private Computation.Loop CreateLoop(List<State> states, LoopData loop)
		{
			DataTable dt = new DataTable(String.Format("ssi_{0}", d_indexTables.Count), true, loop.Function.NumArguments + 1);
			
			dt.IsConstant = true;
			dt.IntegerType = true;
			
			d_indexTables.Add(dt);

			Computation.Loop ret = new Computation.Loop(this, dt, loop.Embedding, loop.Function);
			ret.IsIntegrated = (states[0].Type & State.Flags.Integrated) != 0;

			List<Tree.Node> nodes = new List<Tree.Node>();
			
			// Promote any argument of the embedding that is not in the table and not the same value
			// for all instances
			foreach (Tree.Node node in loop.Instances)
			{
				Tree.Node cloned = (Tree.Node)node.Clone();
				nodes.Add(cloned);

				foreach (Tree.Embedding.Argument arg in loop.Function.Arguments)
				{
					Tree.Node subnode = node.FromPath(arg.Path);
					Cdn.InstructionNumber num = subnode.Instruction as Cdn.InstructionNumber;
					
					if (num != null)
					{
						// Promote to data table
						DataTable.DataItem ditem = d_statetable.Add(num.Value);
						
						ditem.Type = DataTable.DataItem.Flags.Constant;
						subnode.Instruction = new Instructions.State(ditem);
					}
				}
			}

			for (int i = 0; i < loop.Instances.Count; ++i)
			{
				Tree.Node cloned = nodes[i];
				Tree.Node node = loop.Instances[i];

				if (loop.AllRoots)
				{
					ret.Add(d_statetable[node.State], cloned);
					states.Remove(node.State);
				}
				else
				{
					// Create new temporary state for this computation
					DataTable.DataItem item = d_statetable.Add(cloned);
					item.Type = DataTable.DataItem.Flags.Temporary;
					
					ret.Add(item, cloned);
					
					// Create new instruction that references this state
					Instructions.State inst = new Instructions.State(item);
					
					// Replace embedding instance in the node with the temporary state instruction
					node.Instruction = inst;
				}
			}
			
			ret.Close();
			
			return ret;
		}

		public int InitLoopsCount
		{
			get { return d_initLoops.Count; }
		}
		
		public int LoopsCount
		{
			get
			{
				return d_loops.Count;
			}
		}
		
		public IEnumerable<Computation.Loop> Loops
		{
			get
			{
				return d_loops;
			}
		}

		private List<Computation.INode> AssignmentStates(IEnumerable<State> states)
		{
			return AssignmentStates(states, d_loops);
		}

		private List<Computation.INode> AssignmentStates(IEnumerable<State> states, List<Computation.Loop> loops)
		{
			List<Computation.INode> ret = new List<Computation.INode>();
			List<State > st = new List<State>(states);

			if (loops != null)
			{
				// Extract loops from states. Scan for embeddings and replace them with
				// looped stuff, creating temporary variables on the fly if needed
				foreach (Tree.Embedding embedding in d_embeddings)
				{
					Function function = d_embeddingFunctionMap[embedding];
				
					if (function.IsCustom)
					{
						continue;
					}

					LoopData loop = new LoopData(embedding, function);

					foreach (Tree.Node node in embedding.Instances)
					{
						if (st.Contains(node.State))
						{
							loop.Add(node);
						}
					}
				
					if (loop.Instances.Count >= Cdn.RawC.Options.Instance.MinimumLoopSize)
					{
						// Create loop for this thing
						Computation.Loop l = CreateLoop(st, loop);
					
						ret.Add(l);
						loops.Add(l);
					}
				}
			}

			foreach (State state in st)
			{
				ret.Add(new Computation.Assignment(state, d_statetable[state], d_equations[state]));
			}

			return ret;
		}

		private delegate object ObjectSelector(State state);

		private HashSet<State> FilterDependsDirection(IEnumerable<State> states,
		                                              IEnumerable<State> depon,
		                                              bool ison,
			                                          HashSet<State> rest,
			                                          ObjectSelector selector)
		{
			HashSet<State> ret = new HashSet<State>();

			foreach (State s in states)
			{
				bool found = false;

				foreach (State dep in depon)
				{
					object obj;

					if (selector == null)
					{
						obj = dep.Object;
					}
					else
					{
						obj = selector(dep);
					}

					var isdep = Knowledge.Instance.DependsOn(s, obj);

					if (isdep)
					{
						if (ison)
						{
							ret.Add(s);
							found = true;
							break;
						}
						else
						{
							ret.Add(dep);
						}
					}
					else if (!ison && rest != null)
					{
						rest.Add(dep);
					}
				}

				if (!found && ison && rest != null)
				{
					rest.Add(s);
				}
			}

			return ret;
		}

		private HashSet<State> FilterDependsMe(
			IEnumerable<State> states,
			IEnumerable<State> depme,
			HashSet<State> rest,
			ObjectSelector selector)
		{
			return FilterDependsDirection(states, depme, false, rest, selector);
		}

		private HashSet<State> FilterDependsMe(
			IEnumerable<State> states,
			IEnumerable<State> depme,
			HashSet<State> rest)
		{
			return FilterDependsMe(states, depme, rest, null);
		}

		private HashSet<State> FilterDependsMe(
			IEnumerable<State> states,
			IEnumerable<State> depme)
		{
			return FilterDependsMe(states, depme, null);
		}

		private HashSet<State> FilterDependsOn(
			IEnumerable<State> states,
			IEnumerable<State> depon,
			HashSet<State> rest)
		{
			return FilterDependsOn(states, depon, rest, null);
		}

		private HashSet<State> FilterDependsOn(
			IEnumerable<State> states,
			IEnumerable<State> depon)
		{
			return FilterDependsOn(states, depon, null);
		}

		private HashSet<State> FilterDependsOn(
			IEnumerable<State> states,
			IEnumerable<State> depon,
			HashSet<State> rest,
			ObjectSelector selector)
		{
			return FilterDependsDirection(states, depon, true, rest, selector);
		}

		private List<State> InitialModSet
		{
			get
			{
				List<State> ret = new List<State>();

				ret.Add(new State(Knowledge.Instance.Network.Integrator.Variable("dt")));

				foreach (State o in Knowledge.Instance.States)
				{
					Variable v = o.Object as Variable;

					if (v != null && (v.Flags & VariableFlags.In) != 0)
					{
						ret.Add(o);
					}
				}

				return ret;
			}
		}
		
		private void ProgramSource()
		{		
			Cdn.Variable dtprop = Knowledge.Instance.Network.Integrator.Variable("dt");
			DataTable.DataItem dt = d_statetable[dtprop];

			Tree.Node dteq;

			dteq = new Tree.Node(null, new Instructions.Variable("timestep"));

			// Set dt
			d_source.Add(new Computation.Comment("Set timestep"));
			d_source.Add(new Computation.Assignment(null, dt, dteq));
			d_source.Add(new Computation.Empty());

			// Auxiliary states are outs and temporaries (i.e. things that
			// ended up in the state table and will be used from there)
			List<State> auxset = new List<State>(Knowledge.Instance.AuxiliaryStates);

			// Current modset is dt and in-variables
			List<State> modset = InitialModSet;
			List<State> integrated = new List<State>(Knowledge.Instance.FlaggedStates(VariableFlags.Integrated));

			IEnumerable<State> deps = FilterDependsMe(integrated, FilterDependsOn(auxset, modset));

			// Extract states from aux that need to be calculated because they
			// depend on modset and are used by the integrated set
			foreach (List<State> grp in Knowledge.Instance.SortOnDependencies(deps))
			{
				d_source.Add(new Computation.Comment("Dependencies of integrated variables that depend on dt or IN"));
				d_source.AddRange(AssignmentStates(grp));
				d_source.Add(new Computation.Empty());
			}

			modset.Clear();

			// Integrated links
			if (integrated.Count > 0)
			{
				d_source.Add(new Computation.Comment("Integration equations"));
				d_source.AddRange(AssignmentStates(integrated));
				d_source.Add(new Computation.Empty());

				d_source.Add(new Computation.Comment("Make copy of current integrated state"));
				d_source.Add(new Computation.CopyTable(d_statetable, d_statetable, 0, integrated.Count, integrated.Count));
				d_source.Add(new Computation.Empty());

				modset.AddRange(integrated);
			}

			// Increase time
			Cdn.Variable tprop = Knowledge.Instance.Network.Integrator.Variable("t");
			DataTable.DataItem t = d_statetable[tprop];

			Tree.Node eq = new Tree.Node(null, new InstructionFunction((int)Cdn.MathFunctionType.Plus, "+", 2));
			
			eq.Add(new Tree.Node(null, new InstructionVariable(tprop, InstructionVariableBinding.None)));
			eq.Add(new Tree.Node(null, new InstructionVariable(dtprop, InstructionVariableBinding.None)));

			d_source.Add(new Computation.Comment("Increase time"));
			d_source.Add(new Computation.Assignment(null, t, eq));
			d_source.Add(new Computation.Empty());

			// Generate new random values
			List<State> rands = new List<State>(Knowledge.Instance.RandStates);

			if (rands.Count > 0)
			{
				d_source.Add(new Computation.Comment("Compute new random values"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.RandStates));
				d_source.Add(new Computation.Empty());

				modset.AddRange(rands);
			}

			// Postcompute aux
			modset.Add(new State(tprop));
			modset.AddRange(Knowledge.Instance.DelayedStates);

			HashSet<State> later = new HashSet<State>();

			// Split postcompute in states that need to be computed before the delays (because delays depend on them)
			// and those states that can be computed after the delays
			HashSet<State> now = FilterDependsMe(Knowledge.Instance.DelayedStates, FilterDependsOn(auxset, modset), later);

			foreach (List<State> grp in Knowledge.Instance.SortOnDependencies(now))
			{
				d_source.Add(new Computation.Comment("Auxiliary variables that depend on t, integrated or rand"));
				d_source.AddRange(AssignmentStates(grp));
				d_source.Add(new Computation.Empty());
			}

			// Update delayed states
			List<List<State>> grps = Knowledge.Instance.SortOnDependencies(Knowledge.Instance.DelayedStates);

			if (grps.Count > 0)
			{
				d_source.Add(new Computation.Empty());
				d_source.Add(new Computation.Comment("Write values of delayed expressions"));

				foreach (List<State> grp in grps)
				{
					d_source.AddRange(AssignmentStates(grp));
					d_source.Add(new Computation.Empty());

					modset.AddRange(grp);
				}

				d_source.Add(new Computation.Comment("Increment delayed counters"));
				d_source.Add(new Computation.IncrementDelayedCounters(d_delayedCounters, d_delayedCountersSize));
				d_source.Add(new Computation.Empty());
			}

			// Update aux variables that depend on delays
			foreach (List<State> grp in Knowledge.Instance.SortOnDependencies(later))
			{
				d_source.Add(new Computation.Comment("Auxiliary variables that depend on delays"));
				d_source.AddRange(AssignmentStates(grp));
				d_source.Add(new Computation.Empty());
			}
		}
		
		private void ProgramInitialization()
		{
			// Initialize values that do not depend on t
			HashSet<State> not = new HashSet<State>();
			HashSet<State> ontime = new HashSet<State>();
			HashSet<DelayedState> dontime = new HashSet<DelayedState>();

			List<State> ddd = new List<State>();

			ddd.AddRange(Array.ConvertAll<DelayedState, State>(d_delayedStates.ToArray(), (a) => { return a; }));
			ddd.Add(Knowledge.Instance.Time);

			ontime = FilterDependsOn(Knowledge.Instance.InitializeStates, ddd, not);

			ontime.RemoveWhere((s) => {
				var ds = s as DelayedState;

				if (ds != null)
				{
					dontime.Add(ds);
					return true;
				}
				else
				{
					return false;
				}
			});

			not.RemoveWhere((s) => {
				return s is DelayedState;
			});

			foreach (List<State> grp in Knowledge.Instance.SortOnDependencies(not))
			{
				d_initialization.Add(new Computation.Empty());
				d_initialization.AddRange(AssignmentStates(grp, null));
				d_initialization.Add(new Computation.Empty());
			}

			HashSet<State> ontimeleft = new HashSet<State>(ontime);

			if (d_delayedStates.Count > 0)
			{
				d_initialization.Add(new Computation.Empty());
				d_initialization.Add(new Computation.Comment("Initialize delayed history"));

				// Generate delay initialization
				for (int i = 0; i < d_delayedStates.Count; ++i)
				{
					DelayedState ds = d_delayedStates[i];
					DelayedState ids = Knowledge.Instance.InitializeState(ds.Object) as DelayedState;

					List<Computation.INode> deps = new List<Computation.INode>();

					if (ds.Operator.InitialValue != null)
					{
						// Find vars on which the initial value depends and which depend on t
						HashSet<State> dd = FilterDependsMe(new State[] {ids}, ontime);

						dd.RemoveWhere((s) => {
							return s is DelayedState;
						});

						foreach (State l in dd)
						{
							ontimeleft.Remove(l);
						}

						deps.AddRange(AssignmentStates(dd, null));
					}

					d_initialization.Add(new Computation.InitializeDelayHistory(ids, d_delayHistoryTables[i], d_equations[ids], deps, dontime.Contains(ids)));
					d_initialization.Add(new Computation.Empty());
				}
			}
			else
			{
				// Ok, now we are ready to set t
				Tree.Node node = new Tree.Node(null, new Instructions.Variable("t"));
				var st = Knowledge.Instance.State(Knowledge.Instance.Network.Integrator.Variable("t"));

				d_initialization.Add(new Computation.Empty());
				d_initialization.Add(new Computation.Comment("Assign t"));
				d_initialization.Add(new Computation.Assignment(st, d_statetable[st], node));
				d_initialization.Add(new Computation.Empty());
			}

			// Finally, initialize those states that depend on t again
			foreach (List<State> grp in Knowledge.Instance.SortOnDependencies(ontimeleft))
			{
				d_initialization.AddRange(AssignmentStates(grp, d_initLoops));
			}
		}
		
		public IEnumerable<Cdn.Function> UsedCustomFunctions
		{
			get
			{
				return d_usedCustomFunctions;
			}
		}
		
		public IEnumerable<Computation.INode> InitializationNodes
		{
			get
			{
				return d_initialization;
			}
		}

		public bool NodeIsInitialization(Computation.INode node)
		{
			return d_initialization.Contains(node);
		}

		public IEnumerable<Computation.INode> SourceNodes
		{
			get
			{
				return d_source;
			}
		}
		
		public IEnumerable<DataTable> DataTables
		{
			get
			{
				yield return d_statetable;
				
				foreach (DataTable table in d_indexTables)
				{
					yield return table;
				}
				
				yield return d_delayedCounters;
				yield return d_delayedCountersSize;

				foreach (DataTable table in d_delayHistoryTables)
				{
					yield return table;
				}
			}
		}
		
		public DataTable DelayedCounters
		{
			get
			{
				return d_delayedCounters;
			}
		}
		
		public DataTable DelayedCountersSize
		{
			get
			{
				return d_delayedCountersSize;
			}
		}
	}
}

