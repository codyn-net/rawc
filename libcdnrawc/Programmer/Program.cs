using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class Program
	{
		private Options d_options;

		private APIFunction d_apiTDT;
		private APIFunction d_apiPre;
		private APIFunction d_apiPreDiff;
		private APIFunction d_apiDiff;
		private APIFunction d_apiPost;
		private APIFunction d_apiInit;
		private APIFunction d_apiPrepare;
		private APIFunction d_apiReset;
		private APIFunction d_apiEvents;
		private APIFunction d_apiEventsDistance;
		private APIFunction d_apiEventsEvaluate;

		private List<Function> d_functions;
		private List<Tree.Embedding> d_embeddings;
		private List<Cdn.Function> d_usedCustomFunctions;
		private Dictionary<string, Function> d_functionMap;
		private List<Computation.Loop> d_loops;
		private List<Computation.Loop> d_initLoops;
		private Dictionary<Tree.Embedding, Function> d_embeddingFunctionMap;
		private DataTable d_statetable;
		private DataTable d_delayedCounters;
		private DataTable d_delayedCountersSize;
		private DataTable d_constants;
		private DataTable d_eventStates;
		private DataTable d_initialEventStates;
		private List<DataTable> d_indexTables;
		private List<DataTable> d_delayHistoryTables;
		private Dictionary<DelayedState, DataTable> d_delayHistoryMap;
		private Dictionary<State, Tree.Node> d_equations;
		private List<DelayedState> d_delayedStates;
		private DataTable d_randSeedTable;
		private DependencyGraph d_dependencyGraph;
		private HashSet<State> d_initStates;
		private DependencyFilter d_preparedStates;
		private Dictionary<Cdn.Event, Computation.INode> d_eventPrograms;
		private Dictionary<string, Tree.Node> d_zeroNumberExpression;

		public Program(Options options, IEnumerable<Tree.Embedding> embeddings, Dictionary<State, Tree.Node> equations)
		{
			// Write out equations and everything
			d_statetable = new DataTable("ss", true);
			d_constants = new DataTable("constants", true);
			d_constants.IsConstant = true;

			d_eventStates = new DataTable("event_states", false);
			d_initialEventStates = new DataTable("initial_event_states", true);
			d_initialEventStates.IsConstant = true;
			d_initialEventStates.Unique = false;

			d_functions = new List<Function>();
			d_embeddings = new List<Tree.Embedding>(embeddings);
			d_embeddingFunctionMap = new Dictionary<Tree.Embedding, Function>();

			d_zeroNumberExpression = new Dictionary<string, Tree.Node>();

			d_apiTDT = new APIFunction("tdtdeps", "void");
			d_apiTDT.Private = true;

			d_apiPre = new APIFunction("pre", "void", "ValueType", "t", "ValueType", "dt");
			d_apiPreDiff = new APIFunction("prediff", "void");
			d_apiDiff = new APIFunction("diff", "void", "ValueType", "t", "ValueType", "dt");
			d_apiPost = new APIFunction("post", "void", "ValueType", "t", "ValueType", "dt");
			d_apiInit = new APIFunction("init", "void", "ValueType", "t");
			d_apiPrepare = new APIFunction("prepare", "void", "ValueType", "t");
			d_apiReset = new APIFunction("reset", "void", "ValueType", "t");
			d_apiEvents = new APIFunction("events_update", "void");
			d_apiEventsEvaluate = new APIFunction("events_evaluate", "void");
			d_apiEventsDistance = new APIFunction("events_update_distance", "void");

			d_usedCustomFunctions = new List<Cdn.Function>();
			d_functionMap = new Dictionary<string, Function>();
			d_loops = new List<Computation.Loop>();
			d_initLoops = new List<Computation.Loop>();
			d_indexTables = new List<DataTable>();
			d_delayHistoryTables = new List<DataTable>();
			d_delayedStates = new List<DelayedState>();
			d_delayHistoryMap = new Dictionary<DelayedState, DataTable>();
			d_eventPrograms = new Dictionary<Event, Computation.INode>();

			d_delayedCounters = new DataTable("delay_counters", true);

			d_delayedCountersSize = new DataTable("delayed_counters_size", true);
			d_delayedCountersSize.IsConstant = true;

			d_equations = equations;
			d_options = options;

			ProgramDataTables();

			ProgramFunctions();
			ProgramCustomFunctions();

			ComputeDependencies();

			ProgramPrepare();
			ProgramInit();
			ProgramReset();
			ProgramSource();
			ProgramEvents();

			d_statetable.Lock();

			foreach (DataTable table in DataTables)
			{
				table.Lock();
			}
		}

		private Tree.Node ZeroNumberExpression(Cdn.Dimension d)
		{
			string key = String.Format("{0}x{1}", d.Rows, d.Columns);
			Tree.Node n;

			if (d_zeroNumberExpression.TryGetValue(key, out n))
			{
				return n;
			}

			if (d.IsOne)
			{
				n = new Tree.Node(null, new InstructionNumber("0"));
			}
			else
			{
				var num = d.Size();

				n = new Tree.Node(null, new InstructionMatrix(new Cdn.StackArgs(num), d));

				for (int i = 0; i < num; ++i)
				{
					n.Children.Add(new Tree.Node(null, new InstructionNumber("0")));
				}
			}

			d_zeroNumberExpression[key] = n;
			return n;
		}

		public int[] StateRange(IEnumerable<State> states)
		{
			return StateRange(states, null);
		}

		public int[] StateRange(IEnumerable<State> states, int[] def)
		{
			var enu = states.GetEnumerator();

			if (!enu.MoveNext())
			{
				return def;
			}
			else
			{
				var cur = enu.Current;
				var start = d_statetable[cur].DataIndex;

				while (enu.MoveNext())
				{
					cur = enu.Current;
				}

				var item = d_statetable[cur];
				var end = item.DataIndex + item.Dimension.Size();

				return new int[] {start, end};
			}
		}

		private void ComputeDependencies()
		{
			var lst = new List<Tree.Embedding>();

			foreach (var embedding in d_embeddings)
			{
				var f = d_embeddingFunctionMap[embedding];

				if (!f.IsCustom)
				{
					lst.Add(embedding);
				}
			}

			d_dependencyGraph = new DependencyGraph(d_statetable, lst);

			Dictionary<object, State> initmap = new Dictionary<object, State>();

			foreach (State st in Knowledge.Instance.InitializeStates)
			{
				initmap[st.DataKey] = st;
			}

			// Add constraint states
			foreach (var st in Knowledge.Instance.ExternalConstraintStates)
			{
				d_dependencyGraph.Add(st, null);
			}

			// Add event set states
			foreach (var pair in Knowledge.Instance.EventSetStates)
			{
				foreach (var st in pair.Value)
				{
					d_dependencyGraph.Add(st, null);
				}
			}

			// Add initial states
			foreach (State st in Knowledge.Instance.InitializeStates)
			{
				d_dependencyGraph.Add(st, initmap);
			}

			if (Cdn.RawC.Options.Instance.DependencyGraph != null)
			{
				d_dependencyGraph.WriteDot(Cdn.RawC.Options.Instance.DependencyGraph);
			}

			d_initStates = new HashSet<State>(Knowledge.Instance.InitializeStates);
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

		public Computation.INode EventProgram(Cdn.Event ev)
		{
			Computation.INode ret;

			if (d_eventPrograms.TryGetValue(ev, out ret))
			{
				return ret;
			}

			return null;
		}

		private void ProgramDataTables()
		{
			foreach (var state in Knowledge.Instance.States)
			{
				Variable v = state.Object as Variable;
				DataTable.DataItem item = d_statetable.Add(state);

				if (v != null)
				{
					item.Type |= DataTable.DataItem.Flags.State;

					if (v.HasFlag(VariableFlags.Integrated))
					{
						item.Type |= DataTable.DataItem.Flags.Integrated;
					}

					if ((state.Type & State.Flags.Derivative) != 0)
					{
						item.Type |= DataTable.DataItem.Flags.Derivative;
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
					d_delayedCounters.IntegerTypeSize = size;

					d_delayedCountersSize.Add((uint)size).Type = DataTable.DataItem.Flags.Size;
					d_delayedCountersSize.IntegerTypeSize = size;

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

			foreach (var st in Knowledge.Instance.ExternalConstraintStates)
			{
				d_statetable.AddAlias(st, st.Object);
			}

			foreach (var kv in Knowledge.Instance.EventStatesMap)
			{
				d_eventStates.Add(kv.Key);

				if (kv.Key.InitialState != null)
				{
					Knowledge.EventState evstate;

					if (Knowledge.Instance.TryGetEventState(kv.Key, kv.Key.InitialState, out evstate))
					{
						d_initialEventStates.Add((uint)(evstate.Index));
					}
					else
					{
						d_initialEventStates.Add((uint)0);
					}
				}
				else
				{
					d_initialEventStates.Add((uint)0);
				}
			}

			d_eventStates.IntegerTypeSize = (ulong)Knowledge.Instance.EventStates.Count;
			d_initialEventStates.IntegerTypeSize = (ulong)Knowledge.Instance.EventStates.Count;

			if (Cdn.RawC.Options.Instance.Validate)
			{
				d_randSeedTable = new DataTable("rand_seeds", true);
				d_randSeedTable.IsConstant = true;
				d_randSeedTable.IntegerTypeSize = UInt32.MaxValue - 1;

				foreach (State r in Knowledge.Instance.RandStates)
				{
					InstructionRand rr = r.Instructions[0] as InstructionRand;

					d_randSeedTable.Add(rr.Seed).Type = DataTable.DataItem.Flags.RandSeed;
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
			public bool IsOperator { get; set; }
			public Cdn.Function Function { get; set; }
			public Cdn.Function Template { get; set; }

			public CustomFunctionNode(Cdn.Function func)
			{
				Node = Tree.Node.Create(null, func.Expression.Instructions);
				Nodes = new List<Tree.Node>();
				Function = func;
			}
		}

		private void CustomFunctionUsage(Tree.Node node, Dictionary<object, CustomFunctionNode> usage)
		{
			CustomFunctionNode lst;
			Cdn.Function f;
			bool isop = false;

			Cdn.InstructionCustomFunction func = node.Instruction as Cdn.InstructionCustomFunction;
			Cdn.Function templ = null;
			object id;

			if (func != null)
			{
				f = func.Function;
				id = Tree.Node.FunctionId(f, out templ);
			}
			else
			{
				var op = (Cdn.InstructionCustomOperator)node.Instruction;
				f = op.Operator.PrimaryFunction;
				isop = true;

				if (f == null)
				{
					return;
				}

				id = f;
			}

			if (!usage.TryGetValue(id, out lst))
			{
				lst = new CustomFunctionNode(f);
				lst.Template = templ;
				lst.IsOperator = isop;
				usage[id] = lst;

				d_usedCustomFunctions.Add(f);

				// Recurse
				foreach (Tree.Node child in lst.Node.Collect<Cdn.InstructionCustomFunction>())
				{
					CustomFunctionUsage(child, usage);
				}

				foreach (Tree.Node child in lst.Node.Collect<Cdn.InstructionCustomOperator>())
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
			var usage = new Dictionary<object, CustomFunctionNode>();

			// Calculate map from a custom function to the nodes that use that function
			foreach (KeyValuePair<State, Tree.Node> eq in d_equations)
			{
				foreach (Tree.Node node in eq.Value.Collect<Cdn.InstructionCustomFunction>())
				{
					CustomFunctionUsage(node, usage);
				}

				foreach (Tree.Node node in eq.Value.Collect<Cdn.InstructionCustomOperator>())
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

				foreach (Tree.Node node in function.Expression.Collect<Cdn.InstructionCustomOperator>())
				{
					CustomFunctionUsage(node, usage);
				}
			}

			// Foreach custom function that is used
			foreach (var pair in usage)
			{
				var fn = pair.Value;

				// Create a new node for the custom function expression
				Tree.Node node = fn.Node;

				// Calculate all the paths to where the arguments for this function
				// are used in the expression. All arguments are implemented as properties
				List<Cdn.Variable> arguments = new List<Cdn.Variable>();
				List<Cdn.FunctionArgument> aa = new List<FunctionArgument>();

				foreach (Cdn.FunctionArgument arg in fn.Function.Arguments)
				{
					if (!arg.Unused)
					{
						arguments.Add(fn.Function.Variable(arg.Name));
						aa.Add(arg);
					}
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

				var fforname = fn.Template != null ? fn.Template : fn.Function;

				var cfname = fforname.FullIdForDisplay.Replace(".", "_").ToLower();
				string name = GenerateFunctionName(String.Format("cf_{0}", cfname));

				Function func = new Function(name, embedding, aa, !fn.IsOperator);
				Add(embedding, func);

				d_embeddings.Add(embedding);

				foreach (Tree.Node nn in pair.Value.Nodes)
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

		private Computation.Loop CreateLoop(LoopData loop)
		{
			DataTable dt = new DataTable(String.Format("ssi_{0}", d_indexTables.Count), true, loop.Function.NumArguments + 1);
			dt.IsConstant = true;

			d_indexTables.Add(dt);

			Computation.Loop ret = new Computation.Loop(this, dt, loop.Embedding, loop.Function);
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
						var st = new State(num.Value, new Instruction[] {num}, State.Flags.Constant);

						DataTable.DataItem ditem = d_statetable.Add(st);
						d_constants.Add(st);

						d_equations[st] = Tree.Node.Create(st);

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

		public DataTable Constants
		{
			get { return d_constants; }
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

		private List<Computation.INode> AssignmentStates(IEnumerable<State> states, Tree.Embedding embedding)
		{
			return AssignmentStates(states, embedding, d_loops);
		}

		private List<Computation.INode> AssignmentStates(IEnumerable<State> states,
		                                                 Tree.Embedding embedding,
		                                                 List<Computation.Loop> loops)
		{
			List<Computation.INode> ret = new List<Computation.INode>();

			if (loops != null && embedding != null)
			{
				LoopData loop = new LoopData(embedding, d_embeddingFunctionMap[embedding]);

				foreach (State st in states)
				{
					// TODO: partial?
					loop.Add(d_equations[st]);

					if ((d_statetable[st].Type & (DataTable.DataItem.Flags.Once | DataTable.DataItem.Flags.In)) != 0)
					{
						d_initStates.Remove(st);
					}
				}

				if (loop.Instances.Count >= Cdn.RawC.Options.Instance.MinimumLoopSize)
				{
					// Create loop for this thing. Note that CreateLoop
					// removes the states relevant for the loop from 'states'
					Computation.Loop l = CreateLoop(loop);

					ret.Add(l);
					loops.Add(l);

					return ret;
				}
			}

			foreach (State state in states)
			{
				DataTable.DataItem item;

				if (state is ConstraintState)
				{
					item = d_statetable[state.Object];
				}
				else
				{
					item = d_statetable[state];
				}

				ret.Add(new Computation.Assignment(state, item, d_equations[state]));

				if ((d_statetable[state].Type & (DataTable.DataItem.Flags.Once | DataTable.DataItem.Flags.In)) != 0)
				{
					d_initStates.Remove(state);
				}
			}

			return ret;
		}

		private DependencyFilter TDTModSet
		{
			get
			{
				var ret = new DependencyFilter(d_dependencyGraph);

				ret.Add(Knowledge.Instance.Time);
				ret.Add(Knowledge.Instance.TimeStep);

				return ret;
			}
		}

		private void ProgramSetTDT(APIFunction func)
		{
			ProgramSetTDT(func, false);
		}

		private void ProgramSetTDT(APIFunction func, bool dtzero)
		{
			Cdn.Variable tvar = Knowledge.Instance.Network.Integrator.Variable("t");
			DataTable.DataItem t = d_statetable[tvar];

			Cdn.Variable dtvar = Knowledge.Instance.Network.Integrator.Variable("dt");
			DataTable.DataItem dt = d_statetable[dtvar];

			Tree.Node teq = new Tree.Node(null, new Instructions.Variable("t"));
			Tree.Node dteq;

			if (dtzero)
			{
				dteq = ZeroNumberExpression(new Cdn.Dimension { Rows = 1, Columns = 1 });
			}
			else
			{
				dteq = new Tree.Node(null, new Instructions.Variable("dt"));
			}

			func.Body.Add(new Computation.Comment("Set t and dt"));
			func.Body.Add(new Computation.Assignment(null, t, teq));
			func.Body.Add(new Computation.Assignment(null, dt, dteq));
			func.Body.Add(new Computation.Empty());
		}

		private void ProgramTDTDeps(DependencyFilter deps)
		{
			deps = deps.DependsOn(TDTModSet);

			foreach (var grp in d_dependencyGraph.Sort(deps))
			{
				if (grp.Count > 0)
				{
					d_apiTDT.Body.Add(new Computation.Comment("Dependencies of integrated variables that depend on t or dt"));
					d_apiTDT.Body.AddRange(AssignmentStates(grp, grp.Embedding));
					d_apiTDT.Body.Add(new Computation.Empty());
				}
			}
		}

		private void ProgramDependencies(Computation.IBlock api, DependencyFilter deps, string comment)
		{
			ProgramDependencies(api, deps, comment, null);
		}

		private void ProgramDependencies(Computation.IBlock api, DependencyFilter deps, string comment, List<State> ret)
		{
			bool first = true;

			Knowledge.EventStateGroup evgrp = null;
			Computation.StateConditional cond = null;
			Computation.IBlock block = api;

			foreach (var grp in d_dependencyGraph.Sort(deps))
			{
				if (grp.Count == 0)
				{
					continue;
				}

				if (first && !string.IsNullOrEmpty(comment))
				{
					api.Body.Add(new Computation.Comment(comment));
				}

				first = false;

				if (evgrp != grp.EventStateGroup && cond != null)
				{
					cond = null;
					block = api;
				}

				if (grp.EventStateGroup != null && cond == null)
				{
					cond = new Computation.StateConditional(grp.EventStateGroup);
					api.Body.Add(cond);
					block = cond;

					api.NeedsEvents = true;
				}

				if (cond != null)
				{
					foreach (var s in grp)
					{
						var item = d_statetable[s];
						cond.Else.Add(new Computation.Assignment(s, item, ZeroNumberExpression(item.Dimension)));
					}
				}

				evgrp = grp.EventStateGroup;

				var eq = AssignmentStates(grp, grp.Embedding);

				if (ret != null)
				{
					ret.AddRange(grp);
				}

				block.Body.AddRange(eq);
				block.Body.Add(new Computation.Empty());
			}
		}

		private void ProgramPre(DependencyFilter deps,
		                        DependencyFilter derivatives)
		{
			ProgramSetTDT(d_apiPre);

			// All the instates
			var ins = Knowledge.Instance.FlaggedStates(VariableFlags.In);
			var instates = new DependencyFilter(d_dependencyGraph);

			foreach (var i in ins)
			{
				Cdn.Variable v = (Cdn.Variable)i.Object;

				if ((v.Flags & VariableFlags.Once) == 0)
				{
					instates.Add(i);
				}
			}

			// The instates filtered by those that are dependencies of integrated
			// states
			instates.Filter().DependencyOf(derivatives);

			deps = new DependencyFilter(d_dependencyGraph, deps);
			deps.AddRange(Knowledge.Instance.ExternalConstraintStates);

			var diffdeps = deps.Filter().DependsOn(instates);

			ProgramDependencies(d_apiPre,
			                    diffdeps,
			                    "Dependencies of derivatives that depend on <in>");

			instates.Not();
			deps.Not();

			// Check for derivative parts which are only "active" in a particular
			// state (events)
			foreach (var grp in Knowledge.Instance.EventStateGroups)
			{
				if (instates.Count == 0 || deps.Count == 0)
				{
					break;
				}

				var states = new DependencyFilter(d_dependencyGraph, grp.States);

				instates.DependencyOf(states);
				deps.DependsOn(instates);

				if (deps.Count > 0)
				{
					var cond = new Computation.StateConditional(grp);

					ProgramDependencies(cond,
					                    deps,
					                    "Dependencies of event state dependent derivatives that depend on <in>");

					d_apiPre.Body.Add(cond);
					d_apiPre.Body.Add(new Computation.Empty());
				}

				instates.Not();
				deps.Not();
			}
		}

		private void ProgramPreDiff(DependencyFilter deps, DependencyFilter derivatives)
		{
			// PreDiff is called from the integrator just before every
			// diff, except for the first call to diff. This is used to
			// do any computation for which states are assumed to have changed
			// but not other values (like ins)

			// Compute set of nodes which depend on real states and
			// which in turn are dependencies for the derivatives
			var states = new DependencyFilter(d_dependencyGraph,
			                                  Knowledge.Instance.Integrated);

			deps = deps.DependsOn(states).Filter().DependencyOf(derivatives);
			ProgramDependencies(d_apiPreDiff, deps, "Dependencies of derivatives that depend on states");

			deps.Not();

			var alldiff = new DependencyFilter(d_dependencyGraph, derivatives);

			foreach (var grp in Knowledge.Instance.EventStateGroups)
			{
				alldiff.AddRange(grp.States);

				if (deps.Count == 0)
				{
					continue;
				}

				var evstates = new DependencyFilter(d_dependencyGraph, grp.States);
				deps.DependencyOf(evstates);

				if (deps.Count > 0)
				{
					var cond = new Computation.StateConditional(grp);
					List<State> conds = new List<State>();

					ProgramDependencies(cond,
					                    deps,
					                    "Dependencies of event state dependent derivatives that depend on states",
					                    conds);

					// Set the "Else" of the state conditional to clear all
					// state diffs to 0 when event is not begin active
					foreach (var s in conds)
					{
						var item = d_statetable[s];
						cond.Else.Add(new Computation.Assignment(s, item, ZeroNumberExpression(item.Dimension)));
					}

					d_apiPreDiff.Body.Add(cond);
					d_apiPreDiff.Body.Add(new Computation.Empty());
				}

				deps.Not();
			}

			// Compute constraints on states on which derivatives depend
			states.Filter().DependencyOf(alldiff);

			var filt = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.IntegratedConstraintStates);
			filt.Filter().DependsOn(states);

			ProgramDependencies(d_apiPreDiff, filt, "Constraints on integrated states used for derivation");
		}

		private void ProgramDiff(DependencyFilter deps,
		                         DependencyFilter derivatives)
		{
			ProgramSetTDT(d_apiDiff);

			if (d_apiTDT.Body.Count > 0)
			{
				// Call calculate t/dt integrated dependencies
				d_apiDiff.Body.Add(new Computation.CallAPI(d_apiTDT));
			}

			foreach (var grp in Knowledge.Instance.EventStateGroups)
			{
				var eq = new DependencyFilter(d_dependencyGraph, grp.States);
				eq.RemoveWhere((a) => (a.Type & State.Flags.Derivative) == 0);

				ProgramDependencies(d_apiDiff, eq, "Calculate event state dependent derivatives");

				if (eq.Count > 0)
				{
					d_apiDiff.NeedsEvents = true;
				}
			}

			ProgramDependencies(d_apiDiff, derivatives, "Calculate derivatives");
		}

		private void ProgramPost()
		{
			ProgramSetTDT(d_apiPost);

			// Apply constraints for integrated states
			var states = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.Integrated);
			var constraints = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.IntegratedConstraintStates);

			constraints.Filter().DependsOn(states);

			ProgramDependencies(d_apiPost, constraints, "Integrated states constraints");

			// Generate new random values
			var rands  = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.RandStates);

			if (rands.Count > 0)
			{
				d_apiPost.Body.Add(new Computation.Comment("Compute new random values"));
				d_apiPost.Body.Add(new Computation.Rand(rands));
				d_apiPost.Body.Add(new Computation.Empty());
			}

			// Compute set of things that have changed
			var modset = rands;
			var delays = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.DelayedStates);
			var auxout = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.AuxiliaryStates);

			auxout.IntersectWith(Knowledge.Instance.FlaggedStates(VariableFlags.Out));

			modset.UnionWith(TDTModSet);
			modset.UnionWith(delays);
			modset.UnionWith(Knowledge.Instance.Integrated);
			modset.UnionWith(Knowledge.Instance.FlaggedStates(VariableFlags.In));

			var aux = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.AuxiliaryStates);
			aux.RemoveWhere((s) => (s.Type & State.Flags.EventSet) != 0);
			aux.Filter().DependsOn(modset).Unfilter();

			// Split postcompute in states that need to be computed before the delays (because delays depend on them)
			// and those states that can be computed after the delays
			auxout.Filter().DependsOn(modset).Unfilter();

			var now = auxout.DependencyOf(delays);
			var later = now.Not();

			var nowaux = aux.DependencyOf(now);
			var lateraux = nowaux.Not();

			// Add remaining deps from other aux
			now.UnionWith(nowaux);

			ProgramDependencies(d_apiPost, now, "Auxiliary variables that depend on t, dt, states or rand and on which delays depend");

			// Update delayed states
			var grps = d_dependencyGraph.Sort(delays);

			if (grps.Count > 0)
			{
				d_apiPost.Body.Add(new Computation.Empty());
				d_apiPost.Body.Add(new Computation.Comment("Write values of delayed expressions"));

				foreach (var grp in grps)
				{
					// TODO: check with loops
					d_apiPost.Body.AddRange(AssignmentStates(grp, grp.Embedding));
					d_apiPost.Body.Add(new Computation.Empty());
				}

				d_apiPost.Body.Add(new Computation.Comment("Increment delayed counters"));
				d_apiPost.Body.Add(new Computation.IncrementDelayedCounters(d_delayedCounters, d_delayedCountersSize));
				d_apiPost.Body.Add(new Computation.Empty());
			}

			// Add remaining deps from other aux
			later.UnionWith(lateraux);

			// Update aux variables that depend on delays
			ProgramDependencies(d_apiPost, later, "Auxiliary variables that depend on delays (or just come last)");
		}

		private void ProgramSource()
		{
			// Get the list of integrated states
			var derivatives = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.DerivativeStates);

			// Auxiliary states are outs and temporaries (i.e. things that
			// were promoted to the state table and are not recomputed on the fly)
			var aux = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.AuxiliaryStates);

			// Compute subset of aux on which integrated depends
			var allderiv = new DependencyFilter(d_dependencyGraph, derivatives);

			foreach (var grp in Knowledge.Instance.EventStateGroups)
			{
				allderiv.AddRange(grp.States);
			}

			var deps = aux.DependencyOf(allderiv);

			ProgramPre(deps, derivatives);
			ProgramPreDiff(deps, derivatives);
			ProgramDiff(deps, derivatives);
			ProgramPost();
		}

		private void ProgramPrepare()
		{
			d_apiPrepare.Body.Add(new Computation.Comment("Prepare data"));
			d_apiPrepare.Body.Add(new Computation.ZeroMemory());
			d_apiPrepare.Body.Add(new Computation.Empty());

			ProgramSetTDT(d_apiPrepare, true);

			var rands = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.RandStates);

			if (rands.Count > 0)
			{
				d_apiPrepare.Body.Add(new Computation.Comment("Compute initial random values"));
				d_apiPrepare.Body.Add(new Computation.Rand(rands));
				d_apiPrepare.Body.Add(new Computation.Empty());
			}

			// Initialize constants here
			d_apiPrepare.Body.Add(new Computation.Comment("Copy constants"));
			d_apiPrepare.Body.Add(new Computation.CopyTable(d_constants, d_statetable, 0, d_statetable.Size, -1));
			d_apiPrepare.Body.Add(new Computation.Empty());

			// Set initial states if needed
			if (d_eventStates.Count > 0)
			{
				d_apiPrepare.NeedsEvents = true;

				d_apiPrepare.Body.Add(new Computation.Comment("Copy initial event states"));
				d_apiPrepare.Body.Add(new Computation.CopyTable(d_initialEventStates, d_eventStates, -1));
				d_apiPrepare.Body.Add(new Computation.Empty());
			}

			// Initialize _IN_
			var ins = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.PrepareStates);

			// Create set of aux and init states
			var states = new HashSet<State>(Knowledge.Instance.AuxiliaryStates);

			foreach (var s in d_initStates)
			{
				states.Add(s);
			}

			var deps = new DependencyFilter(d_dependencyGraph, states);
			var indeps = deps.DependencyOf(ins);

			ProgramDependencies(d_apiPrepare, indeps, "Dependencies of _in_ variables");
			ProgramDependencies(d_apiPrepare, new DependencyFilter(d_dependencyGraph, ins), "_in_ variables");

			// Prepared states are indesp and ins
			d_preparedStates = indeps;

			foreach (var s in ins)
			{
				d_preparedStates.Add(s);
			}
		}

		private void ProgramInit()
		{
			// Due to the way that delays are implemented, we are going to first
			// initialize everything without a dependency on either t or dt
			var delaymodset = new DependencyFilter(d_dependencyGraph, d_delayedStates);
			delaymodset.UnionWith(TDTModSet);

			// Make set of prepared states that are already computed in the
			// prepare stage and which do not depend on any _in_ variables.
			// Those can be removed from the init states
			var prepped = d_preparedStates.DependsOn(Knowledge.Instance.FlaggedStates(Cdn.VariableFlags.In)).Not();

			// Remove from initstates those that are already computed in 'prepare'
			var initstates = new DependencyFilter(d_dependencyGraph, d_initStates);
			initstates.RemoveWhere(a => prepped.Contains(a));

			var depontime = initstates.DependsOn(delaymodset);
			var beforetime = depontime.Not();

			var delays = new DependencyFilter(d_dependencyGraph);

			depontime.RemoveWhere((s) => {
				var ds = s as DelayedState;

				if (ds != null)
				{
					delays.Add(ds);
					return true;
				}
				else
				{
					return false;
				}
			});

			depontime.RemoveWhere((s) => {
				return s is DelayedState;
			});

			ProgramDependencies(d_apiInit, beforetime, "Compute initial values _not_ depending on t/dt or delays");

			var depontimeLeft = new DependencyFilter(d_dependencyGraph, depontime);

			if (d_delayedStates.Count > 0)
			{
				d_apiInit.Body.Add(new Computation.Empty());
				d_apiInit.Body.Add(new Computation.Comment("Initialize delayed history"));

				// Generate delay initialization
				for (int i = 0; i < d_delayedStates.Count; ++i)
				{
					DelayedState ds = d_delayedStates[i];
					DelayedState ids = Knowledge.Instance.InitializeState(ds.Object) as DelayedState;

					List<Computation.INode> deps = new List<Computation.INode>();

					if (ds.Operator.InitialValue != null)
					{
						// Find vars on which the initial value depends and which depend on t
						DependencyFilter dd = new DependencyFilter(d_dependencyGraph, new State[] {ids});
						dd.Filter().DependencyOf(depontime);

						dd.RemoveWhere((s) => {
							return s is DelayedState;
						});

						foreach (State l in dd)
						{
							depontimeLeft.Remove(l);
						}

						deps.AddRange(AssignmentStates(dd, null));
					}

					d_apiInit.Body.Add(new Computation.InitializeDelayHistory(ids, d_delayHistoryTables[i], d_equations[ids], deps, delays.Contains(ids)));
					d_apiInit.Body.Add(new Computation.Empty());
				}
			}
			else
			{
				// Ok, now we are ready to set t
				ProgramSetTDT(d_apiInit, true);
			}

			ProgramDependencies(d_apiInit, depontimeLeft, "Finally, compute values that depended on t/dt or delays");
			d_apiInit.Body.Add(new Computation.CallAPI(d_apiEventsEvaluate));

			APIFunction postUpdate = new APIFunction("events_post_update", "void");
			d_apiInit.Body.Add(new Computation.CallAPI(postUpdate));

			d_apiInit.Body.Add(new Computation.CallAPI(d_apiEventsDistance));
		}

		private void ProgramReset()
		{
			var t = new Tree.Node(null, new Instructions.Variable("t"));

			d_apiReset.Body.Add(new Computation.CallAPI(d_apiPrepare, t));
			d_apiReset.Body.Add(new Computation.CallAPI(d_apiInit, t));
		}

		private void ProgramEvents()
		{
			var eq = new DependencyFilter(d_dependencyGraph, Knowledge.Instance.EventEquationStates);
			ProgramDependencies(d_apiEventsEvaluate, eq, "Event conditions");

			d_apiEvents.Body.Add(new Computation.CallAPI(d_apiEventsEvaluate));
			d_apiEvents.Body.Add(new Computation.CallAPI(d_apiEventsDistance));

			var aux = new List<State>(Knowledge.Instance.AuxiliaryStates);
			aux.RemoveAll((s) => (s.Type & State.Flags.Promoted) == 0);

			foreach (var ev in Knowledge.Instance.Events)
			{
				var lst = Knowledge.Instance.EventSetStates[ev];

				if (lst.Count > 0)
				{
					var h = new HashSet<State>(aux);
					h.UnionWith(lst);

					var dg = d_dependencyGraph.Collapse(h);
					var auxpro = new DependencyFilter(dg, aux);

					var b = new Computation.Block();
					var auxdeps = auxpro.DependencyOf(lst);

					ProgramDependencies(b, auxdeps, "Dependencies");

					b.Body.AddRange(AssignmentStates(lst, null));
					d_eventPrograms[ev] = b;
				}
			}
		}

		public IEnumerable<Cdn.Function> UsedCustomFunctions
		{
			get { return d_usedCustomFunctions; }
		}

		public bool NodeIsInitialization(Computation.INode node)
		{
			return d_apiInit.Body.Contains(node) || d_apiPrepare.Body.Contains(node);
		}

		public IEnumerable<APIFunction> APIFunctions
		{
			get
			{
				yield return d_apiPrepare;
				yield return d_apiInit;
				yield return d_apiReset;
				yield return d_apiTDT;
				yield return d_apiPre;
				yield return d_apiPreDiff;
				yield return d_apiDiff;
				yield return d_apiPost;
				yield return d_apiEvents;
				yield return d_apiEventsEvaluate;
			}
		}

		public IEnumerable<DataTable> DataTables
		{
			get
			{
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

				if (d_randSeedTable != null)
				{
					yield return d_randSeedTable;
				}

				yield return d_constants;
				yield return d_initialEventStates;
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

		public DataTable EventStatesTable
		{
			get { return d_eventStates; }
		}

		public DataTable InitialEventStatesTable
		{
			get { return d_initialEventStates; }
		}
	}
}

