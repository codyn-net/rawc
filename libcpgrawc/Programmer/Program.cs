																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																																	using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer
{
	public class Program
	{
		private Options d_options;
		private List<Computation.INode> d_source;
		private List<Function> d_functions;
		private List<Tree.Embedding> d_embeddings;
		private List<Computation.INode> d_initialization;
		private List<Cpg.Function> d_usedCustomFunctions;
		private Dictionary<string, Function> d_functionMap;
		private List<State> d_updateStates;
		private int d_stateIntegratedIndex;
		private int d_stateIntegratedUpdateIndex;
		private Dictionary<DataTable.DataItem, State> d_integrateTable;
		private List<Computation.Loop> d_loops;
		private List<Computation.Loop> d_initLoops;
		private Dictionary<Tree.Embedding, Function> d_embeddingFunctionMap;
		private DataTable d_statetable;
		private DataTable d_delayedCounters;
		private DataTable d_delayedCountersSize;
		private List<DataTable> d_indexTables;
		private Dictionary<State, Tree.Node> d_equations;

		public Program(Options options, IEnumerable<Tree.Embedding> embeddings, Dictionary<State, Tree.Node> equations)
		{
			// Write out equations and everything
			d_statetable = new DataTable("ss", true);

			d_functions = new List<Function>();
			d_embeddings = new List<Tree.Embedding>(embeddings);
			d_embeddingFunctionMap = new Dictionary<Tree.Embedding, Function>();
			d_source = new List<Computation.INode>();
			d_initialization = new List<Computation.INode>();
			d_usedCustomFunctions = new List<Cpg.Function>();
			d_functionMap = new Dictionary<string, Function>();
			d_updateStates = new List<State>();
			d_integrateTable = new Dictionary<DataTable.DataItem, State>();
			d_loops = new List<Computation.Loop>();
			d_initLoops = new List<Computation.Loop>();
			d_indexTables = new List<DataTable>();

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
			// Add direct state variables
			foreach (State state in Knowledge.Instance.DirectStates)
			{
				d_statetable.Add(state).Type |= (DataTable.DataItem.Flags.State | DataTable.DataItem.Flags.Direct);
			}
			
			d_stateIntegratedIndex = d_statetable.Count;

			// Add integrated state variables
			foreach (State state in Knowledge.Instance.IntegratedStates)
			{
				d_statetable.Add(state).Type |= (DataTable.DataItem.Flags.State | DataTable.DataItem.Flags.Integrated);
			}
			
			d_stateIntegratedUpdateIndex = d_statetable.Count;
			
			// Add update values for integrated state variables
			foreach (State state in Knowledge.Instance.IntegratedStates)
			{
				State update = new State(State.Flags.Update);
				
				d_updateStates.Add(update);
				d_statetable.Add(update).Type |= (DataTable.DataItem.Flags.State |
				                                  DataTable.DataItem.Flags.Integrated |
				                                  DataTable.DataItem.Flags.Update);

				d_integrateTable[d_statetable[state]] = update;
			}
			
			// Add in variables
			foreach (Cpg.Property prop in Knowledge.Instance.InProperties)
			{
				d_statetable.Add(prop).Type |= (DataTable.DataItem.Flags.State | DataTable.DataItem.Flags.In);
			}
			
			// Add out variables
			foreach (Cpg.Property prop in Knowledge.Instance.OutProperties)
			{
				d_statetable.Add(prop).Type |= (DataTable.DataItem.Flags.State | DataTable.DataItem.Flags.Out);
			}
			
			// Add delayed
			foreach (State state in Knowledge.Instance.DelayedStates)
			{
				d_statetable.Add(state).Type |= (DataTable.DataItem.Flags.State | DataTable.DataItem.Flags.Delayed);
				
				DelayedState.Size size = ((DelayedState)state).Count;

				d_delayedCounters.Add(size).Type = DataTable.DataItem.Flags.Counter;
				d_delayedCounters.MaxSize = size - 1;
				
				d_delayedCountersSize.Add((uint)size).Type = DataTable.DataItem.Flags.Size;
				d_delayedCountersSize.MaxSize = size;
			}
			
			// Add intialized variables
			foreach (State state in Knowledge.Instance.InitializeStates)
			{
				d_statetable.Add(state).Type |= (DataTable.DataItem.Flags.State | DataTable.DataItem.Flags.Initialization);
			}
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
		
		public IEnumerable<T> CollectInstructions<T>() where T : Cpg.Instruction
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

			public CustomFunctionNode(Cpg.Function func)
			{
				Node = Tree.Node.Create(null, func.Expression.Instructions);
				Nodes = new List<Tree.Node>();
			}
		}
		
		private void CustomFunctionUsage(Tree.Node node, Dictionary<Cpg.Function, CustomFunctionNode> usage)
		{
			CustomFunctionNode lst;
			Cpg.Function f = ((Cpg.InstructionCustomFunction)node.Instruction).Function;
				
			if (!usage.TryGetValue(f, out lst))
			{
				lst = new CustomFunctionNode(f);
				usage[f] = lst;

				d_usedCustomFunctions.Add(f);

				// Recurse
				foreach (Tree.Node child in lst.Node.Collect<Cpg.InstructionCustomFunction>())
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
			Dictionary<Cpg.Function, CustomFunctionNode > usage = new Dictionary<Cpg.Function, CustomFunctionNode>();
			
			// Calculate map from a custom function to the nodes that use that function
			foreach (KeyValuePair<State, Tree.Node> eq in d_equations)
			{
				foreach (Tree.Node node in eq.Value.Collect<Cpg.InstructionCustomFunction>())
				{
					CustomFunctionUsage(node, usage);
				}
			}
			
			// Check also in the generated function implementations
			foreach (Function function in d_functions)
			{
				foreach (Tree.Node node in function.Expression.Collect<Cpg.InstructionCustomFunction>())
				{
					CustomFunctionUsage(node, usage);
				}
			}

			// Foreach custom function that is used
			foreach (Cpg.Function function in usage.Keys)
			{
				// Create a new node for the custom function expression
				Tree.Node node = usage[function].Node;

				// Calculate all the paths to where the arguments for this function
				// are used in the expression. All arguments are implemented as properties
				List<Cpg.Property> arguments = new List<Cpg.Property>();
				
				foreach (Cpg.FunctionArgument arg in function.Arguments)
				{
					arguments.Add(function.Property(arg.Name));
				}
				
				List<Tree.Embedding.Argument> args = new List<Tree.Embedding.Argument>();

				foreach (Tree.Node child in node.Collect<InstructionProperty>())
				{
					InstructionProperty prop = child.Instruction as InstructionProperty;
					int idx = arguments.IndexOf(prop.Property);
					
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
					Cpg.InstructionNumber num = subnode.Instruction as Cpg.InstructionNumber;
					
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
			get
																										{ return d_initLoops.Count; }
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
				
					if (loop.Instances.Count >= Cpg.RawC.Options.Instance.MinimumLoopSize)
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
		
		private void ProgramSource()
		{		
			Cpg.Property dtprop = Knowledge.Instance.Network.Integrator.Property("dt");
			DataTable.DataItem dt = d_statetable[dtprop];

			if (d_options.FixedStepSize <= 0)
			{
				Tree.Node dteq = new Tree.Node(null, new Instructions.Variable("timestep"));
			
				// Set dt
				d_source.Add(new Computation.Comment("Set timestep"));
				d_source.Add(new Computation.Assignment(null, dt, dteq));
				d_source.Add(new Computation.Empty());
			}

			// Precompute for out properties
			if (Knowledge.Instance.PrecomputeBeforeDirectStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Out properties that depend on IN states and are needed by direct calculations"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.PrecomputeBeforeDirectStates));
				d_source.Add(new Computation.Empty());
			}
			
			// Direct links	
			if (Knowledge.Instance.DirectStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Direct equations"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.DirectStates));
				d_source.Add(new Computation.Empty());
			}
			
			// Precompute for out properties
			if (Knowledge.Instance.PrecomputeBeforeIntegratedStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Out properties that depend on direct states and are needed by integration calculations"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.PrecomputeBeforeIntegratedStates));
				d_source.Add(new Computation.Empty());
			}
			
			// Integrated links			
			if (Knowledge.Instance.IntegratedStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Integration equations"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.IntegratedStates));
				d_source.Add(new Computation.Empty());
			}

			int num = d_stateIntegratedUpdateIndex - d_stateIntegratedIndex;
			
			if (num > 0)
			{
				d_source.Add(new Computation.Comment("Make copy of current integrated state"));
				d_source.Add(new Computation.CopyTable(d_statetable, d_statetable, d_stateIntegratedIndex, num, d_stateIntegratedUpdateIndex));
				d_source.Add(new Computation.Empty());
			}

			// Increase time before post computing out properties
			Cpg.Property tprop = Knowledge.Instance.Network.Integrator.Property("t");
			DataTable.DataItem t = d_statetable[tprop];

			Tree.Node eq = new Tree.Node(null, new InstructionOperator((int)Cpg.MathOperatorType.Plus, "+", 2));
			
			eq.Add(new Tree.Node(null, new InstructionProperty(tprop, InstructionPropertyBinding.None)));
			eq.Add(new Tree.Node(null, new InstructionProperty(dtprop, InstructionPropertyBinding.None)));

			d_source.Add(new Computation.Comment("Increase time"));
			d_source.Add(new Computation.Assignment(null, t, eq));
			d_source.Add(new Computation.Empty());

			// Postcompute for out properties
			if (Knowledge.Instance.PrecomputeAfterIntegratedStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Out properties that depend on integrated states or IN states"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.PrecomputeAfterIntegratedStates, null));
				d_source.Add(new Computation.Empty());
			}
			
			// Compute delayed values using the current counters
			if (Knowledge.Instance.DelayedStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Write values of delayed expressions"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.DelayedStates));
				d_source.Add(new Computation.Empty());
				
				d_source.Add(new Computation.Comment("Increment delayed counters"));
				d_source.Add(new Computation.IncrementDelayedCounters(d_delayedCounters, d_delayedCountersSize));
				d_source.Add(new Computation.Empty());
			}
		}
		
		private void ProgramInitialization()
		{
			if (d_options.FixedStepSize > 0)
			{
				Cpg.Property dtprop = Knowledge.Instance.Network.Integrator.Property("dt");
				DataTable.DataItem dt = d_statetable[dtprop];
				
				Tree.Node node = new Tree.Node(null, new Cpg.InstructionNumber(d_options.FixedStepSize));
				
				d_initialization.Add(new Computation.Comment("Set the fixed time step"));
				d_initialization.Add(new Computation.Assignment(null, dt, node));
				
				if (Knowledge.Instance.InitializeStatesCount > 0)
				{
					d_initialization.Add(new Computation.Empty());
				}
			}
			
			List<State > init = new List<State>();

			foreach (State state in Knowledge.Instance.InitializeStates)
			{
				if (!d_statetable.Contains(state))
				{
					continue;
				}
				
				init.Add(state);
			}

			// Do not generate loops for now, otherwise use d_initLoops
			d_initialization.AddRange(AssignmentStates(init, null));
		}
		
		public IEnumerable<Cpg.Function> UsedCustomFunctions
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

