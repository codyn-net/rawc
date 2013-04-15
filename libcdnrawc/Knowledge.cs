using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class Knowledge
	{
		private static Knowledge s_instance;
		private Cdn.Network d_network;
		private List<State> d_states;
		private List<State> d_initialize;
		private List<State> d_auxStates;
		private List<State> d_prepareStates;
		private List<State> d_randStates;
		private List<State> d_delayedStates;
		private List<State> d_integratedConstraintStates;
		private List<State> d_externalConstraintStates;
		private Dictionary<Instruction, Instruction> d_instructionMapping;
		private Dictionary<Instruction, double> d_delays;
		private Dictionary<object, State> d_stateMap;
		private Dictionary<object, State> d_initializeMap;
		private Dictionary<object, State> d_derivativeMap;
		private List<State> d_integrated;
		private List<State> d_derivativeStates;
		private Dictionary<Cdn.VariableFlags, List<Cdn.Variable>> d_flaggedVariables;
		private List<Cdn.Variable> d_variables;
		private HashSet<object> d_randStateSet;
		private List<State> d_eventEquationStates;
		private List<EventNodeState> d_eventNodeStates;
		private Dictionary<Cdn.Event, List<EventSetState>> d_eventSetStates;
		private Dictionary<Cdn.Variable, Cdn.EdgeAction[]> d_actionedVariables;
		private List<Cdn.Variable> d_functionHelperVariables;
		
		public class EventState
		{
			public Node Node;
			public string Name;
			public List<Cdn.EdgeAction> ActiveActions;
			public int Index;
		}
		
		public class EventStateGroup
		{
			public List<int> Indices;
			public List<Cdn.EdgeAction> Actions;
			public List<State> States;
		}

		public class EventStateContainer
		{
			public List<string> States;
			public int Index;
		}

		private List<Cdn.Event> d_events;
		private List<EventState> d_eventStates;
		private Dictionary<string, EventState> d_eventStateIdMap;
		private Dictionary<Cdn.EdgeAction, Cdn.Variable> d_eventActionProperties;
		private Dictionary<Cdn.Node, EventStateContainer> d_eventStatesMap;
		private Dictionary<string, EventStateGroup> d_eventStateGroups;

		public static Knowledge Initialize(Cdn.Network network)
		{
			if (s_instance == null || s_instance.Network != network)
			{
				s_instance = new Knowledge(network);
				s_instance.Init();
			}

			return s_instance;
		}
		
		public static Knowledge Instance
		{
			get
			{
				return s_instance;
			}
		}
		
		private void Init()
		{
			d_delays = new Dictionary<Instruction, double>();
			
			d_stateMap = new Dictionary<object, State>();
			d_initializeMap = new Dictionary<object, State>();

			d_variables = new List<Variable>();
			d_integrated = new List<State>();
			d_flaggedVariables = new Dictionary<VariableFlags, List<Variable>>();
			d_states = new List<State>();
			d_auxStates = new List<State>();
			d_prepareStates = new List<State>();
			d_initialize = new List<State>();
			d_randStates = new List<State>();
			d_delayedStates = new List<State>();
			d_derivativeStates = new List<State>();
			d_derivativeMap = new Dictionary<object, State>();
			d_integratedConstraintStates = new List<State>();
			d_externalConstraintStates = new List<State>();
			d_events = new List<Event>();
			d_eventStates = new List<EventState>();
			d_eventStatesMap = new Dictionary<Node, EventStateContainer>();
			d_eventActionProperties = new Dictionary<Cdn.EdgeAction, Cdn.Variable>();
			d_eventStateIdMap = new Dictionary<string, EventState>();
			d_eventStateGroups = new Dictionary<string, EventStateGroup>();
			d_eventEquationStates = new List<State>();
			d_eventNodeStates = new List<EventNodeState>();
			d_eventSetStates = new Dictionary<Event, List<EventSetState>>();
			d_actionedVariables = new Dictionary<Variable, EdgeAction[]>();
			d_functionHelperVariables = new List<Variable>();

			d_instructionMapping = new Dictionary<Instruction, Instruction>();

			d_eventStates.Add(new EventState {
				Index = 0,
			});

			Scan();
		}

		private delegate State StateCreator(Variable v, EdgeAction[] actions);
		
		private string EventStateId(Node node, string state)
		{
			return String.Format("{0}@{1}", node.Handle.ToString(), state);
		}

		private State ExpandedState(Variable prop)
		{
			return ExpandedState(prop, (v, actions) => {
				return new State(v, actions);
			});
		}

		private State ExpandedState(Variable prop, StateCreator creator)
		{
			Cdn.EdgeAction[] actions;

			if (d_actionedVariables.TryGetValue(prop, out actions))
			{
				return creator(prop, actions);
			}
			else
			{
				return creator(prop, new Cdn.EdgeAction[] {});
			}
		}

		public void UpdateInstructionMap(Dictionary<Instruction, Instruction> mapping)
		{
			foreach (KeyValuePair<Instruction, Instruction> pair in mapping)
			{
				d_instructionMapping[pair.Key] = pair.Value;
			}
		}

		private void AddInitialize(State state)
		{
			var v = state.Object as Variable;

			if ((v.Flags & Cdn.VariableFlags.In) != 0)
			{
				d_prepareStates.Add(state);
			}

			d_initializeMap[state.Object] = state;
			d_initialize.Add(state);
		}

		private bool AddState(HashSet<object> unique, State state)
		{
			if (unique == null || unique.Add(state.Object))
			{
				if (state.Object != null)
				{
					d_stateMap[state.Object] = state;
				}

				d_states.Add(state);
				return true;
			}

			return false;
		}

		private void AddAux(State s, HashSet<object> unique)
		{
			if (unique != null && unique.Contains(s.Object))
			{
				return;
			}

			Cdn.Variable v = (Cdn.Variable)s.Object;

			if ((v.Flags & (VariableFlags.In | VariableFlags.Once)) == 0)
			{
				d_auxStates.Add(s);
			}
		}

		private string UniqueVariableName(Cdn.Object obj, string name)
		{
			if (obj.Variable(name) == null)
			{
				return name;
			}

			int i = 0;

			while (true)
			{
				var nm = String.Format("{0}_{1}", name, i);

				if (obj.Variable(nm) == null)
				{
					return nm;
				}

				++i;
			}
		}
		
		public Cdn.Node FindStateNode(Cdn.Node parent)
		{
			while (true)
			{
				if (d_eventStatesMap.ContainsKey(parent))
				{
					return parent;
				}

				var next = parent.Parent;

				if (next == null)
				{
					return parent;
				}

				parent = next;
			}
		}

		private List<State> ExtractEventActionStates(Cdn.Variable v)
		{
			var ret = new List<State>();

			foreach (Cdn.EdgeAction action in v.Actions)
			{
				var ph = action.Phases;
				var eph = action.Edge.Phases;
				
				if (ph.Length != 0 || eph.Length != 0)
				{
					var nm = UniqueVariableName(action.Edge, String.Format("__action_{0}", action.Target));
					var nv = new Cdn.Variable(nm, action.Equation.Copy(), Cdn.VariableFlags.None);

					action.Edge.AddVariable(nv);
					d_eventActionProperties[action] = nv;

					var evst = new EventActionState(action, nv);
					ret.Add(evst);

					HashSet<string> hs;

					if (ph.Length != 0)
					{
						hs = new HashSet<string>(ph);

						if (eph.Length != 0)
						{
							hs.IntersectWith(eph);
						}
					}
					else
					{
						hs = new HashSet<string>(eph);
					}

					var node = FindStateNode(action.Edge.Parent);
					List<int> indices = new List<int>();

					foreach (var st in hs)
					{
						var evstdid = EventStateId(node, st);
						EventState revst;

						if (d_eventStateIdMap.TryGetValue(evstdid, out revst))
						{
							indices.Add(revst.Index);
							revst.ActiveActions.Add(action);
						}
					}

					if (indices.Count != 0)
					{
						indices.Sort();
						string key = String.Join(",", indices.ConvertAll(a => a.ToString()));

						EventStateGroup grp;
					
						if (!d_eventStateGroups.TryGetValue(key, out grp))
						{
							grp = new EventStateGroup {
								Actions = new List<Cdn.EdgeAction>(),
								States = new List<State>(),
								Indices = indices,
							};

							d_eventStateGroups[key] = grp;
						}
					
						grp.Actions.Add(action);
						grp.States.Add(evst);
					}
				}
			}

			return ret;
		}

		public EventState GetEventState(Cdn.Node parent, string state)
		{
			return d_eventStateIdMap[EventStateId(parent, state)];
		}

		private void ExtractStates()
		{
			HashSet<object> unique = new HashSet<object>();
			
			// Add t/dt
			AddState(unique, ExpandedState(Network.Integrator.Variable("t")));
			AddState(unique, ExpandedState(Network.Integrator.Variable("dt")));

			// Add integrated state variables
			var integrated = Knowledge.Instance.FlaggedVariables(VariableFlags.Integrated);
			var evstates = new List<State>();

			foreach (var v in integrated)
			{
				evstates.AddRange(ExtractEventActionStates(v));

				var st = new State(v, null);

				AddState(unique, st);
				d_integrated.Add(st);

				if (v.Constraint != null)
				{
					// Add special state computation for constraint
					var cst = new ConstraintState(v);

					d_integratedConstraintStates.Add(cst);
					d_externalConstraintStates.Add(cst);
				}
			}

			// Add states for the derivatives
			foreach (var v in integrated)
			{
				var st = ExpandedState(v, (vv, actions) => {
					return new DerivativeState(vv, actions);
				});

				// Add directly to the state map
				d_states.Add(st);
				d_derivativeStates.Add(st);
				d_derivativeMap[v] = st;
			}

			// Add states for the event partials
			foreach (var s in evstates)
			{
				AddState(null, s);
			}

			// Add in variables
			foreach (var v in FlaggedVariables(VariableFlags.In))
			{
				if ((v.Flags & VariableFlags.FunctionArgument) == 0)
				{
					if (!unique.Contains(v))
					{
						var s = ExpandedState(v);

						AddState(unique, s);

						if (v.Constraint != null)
						{
							var cst = new ConstraintState(v);
							d_externalConstraintStates.Add(cst);
						}
					}
				}
			}

			HashSet<object> auxset = new HashSet<object>();

			// Add out variables
			foreach (var v in FlaggedVariables(VariableFlags.Out))
			{
				if (!unique.Contains(v))
				{
					State s = ExpandedState(v);
					AddState(unique, s);

					AddAux(s, auxset);
				}
			}

			// Add once variables
			foreach (var v in FlaggedVariables(VariableFlags.Once))
			{
				if (!unique.Contains(v))
				{
					var s = ExpandedState(v);
					AddState(unique, s);
				}
			}
			
			// Add acted upon variables
			foreach (var v in d_actionedVariables)
			{
				if (!unique.Contains(v.Key))
				{
					var s = ExpandedState(v.Key);
					AddState(unique, s);
				}
			}

			// Add multidim variables which are used more than once
			foreach (var v in d_variables)
			{
				if (unique.Contains(v))
				{
					continue;
				}

				if (!v.Dimension.IsOne && v.UseCount() > 1)
				{
					var s = ExpandedState(v);

					AddState(unique, s);
					AddAux(s, auxset);
				}
			}

			// Add function help variables
			foreach (var v in d_functionHelperVariables)
			{
				if (unique.Contains(v))
				{
					continue;
				}

				var s = ExpandedState(v);

				AddState(unique, s);
				AddAux(s, auxset);
			}

			// Add event set variables
			foreach (var kv in d_eventSetStates)
			{
				foreach (var e in kv.Value)
				{
					var s = ExpandedState(e.SetVariable.Variable);

					AddState(unique, s);
					AddAux(s, auxset);
				}
			}

			d_network.ForeachExpression((e) => {
				foreach (var i in e.Instructions)
				{
					var v = i as InstructionVariable;

					if (v != null && !unique.Contains(v.Variable) && v.HasSlice)
					{
						var s = ExpandedState(v.Variable);

						AddState(unique, s);
						AddAux(s, auxset);
					}
				}
			});
		}

		private IEnumerable<State> AllStates
		{
			get
			{
				foreach (State s in d_states)
				{
					yield return s;
				}

				foreach (State s in d_initialize)
				{
					yield return s;
				}
				
				foreach (State s in d_externalConstraintStates)
				{
					yield return s;
				}
			}
		}

		private void ExtractRand()
		{
			d_randStateSet = new HashSet<object>();

			foreach (var state in new List<State>(AllStates))
			{
				foreach (Instruction i in state.Instructions)
				{
					InstructionRand r = i as InstructionRand;

					if (r != null)
					{
						Expression expr = new Expression("rand()");
						expr.Compile(null, null);

						if (Options.Instance.Validate)
						{
							((InstructionRand)expr.Instructions[0]).Seed = r.Seed;
						}

						State rs = new State(r, expr, RawC.State.Flags.None);

						if (AddState(d_randStateSet, rs))
						{
							d_randStates.Add(rs);
						}
					}
				}
			}
		}

		public int CountRandStates
		{
			get { return d_randStates.Count; }
		}

		public IEnumerable<State> States
		{
			get { return d_states; }
		}

		public IEnumerable<State> AuxiliaryStates
		{
			get { return d_auxStates; }
		}

		public IEnumerable<State> PrepareStates
		{
			get { return d_prepareStates; }
		}

		public IEnumerable<State> RandStates
		{
			get { return d_randStates; }
		}

		public IEnumerable<State> DelayedStates
		{
			get { return d_delayedStates; }
		}

		public IEnumerable<State> InitializeStates
		{
			get { return d_initialize; }
		}

		public IEnumerable<State> IntegratedConstraintStates
		{
			get { return d_integratedConstraintStates; }
		}

		public IEnumerable<State> ExternalConstraintStates
		{
			get { return d_externalConstraintStates; }
		}

		private IEnumerable<EventLogicalNode> EventNodes(EventLogicalNode node)
		{
			yield return node;

			if (node.Left != null)
			{
				foreach (var n in EventNodes(node.Left))
				{
					yield return n;
				}
			}

			if (node.Right != null)
			{
				foreach (var n in EventNodes(node.Right))
				{
					yield return n;
				}
			}
		}

		private void AddEventNodeState(EventNodeState s)
		{
			AddState(null, s);
			d_eventNodeStates.Add(s);
		}

		private struct EventNode
		{
			public Cdn.Event Event;
			public Cdn.EventLogicalNode Node;
		}

		private void ExtractEventStates()
		{
			Queue<EventNode> states = new Queue<EventNode>();

			foreach (var ev in d_events)
			{
				var n = new EventNode {
					Event = ev,
					Node = ev.LogicalTree
				};

				states.Enqueue(n);
			}

			// Expand nodes
			while (states.Count > 0)
			{
				var n = states.Dequeue();

				// Add three states for each node to hold the 
				// 1) previous value of the event equation
				// 2) current value of the event equation
				// 3) distance value of the event equation
				AddEventNodeState(new EventNodeState(n.Event, n.Node, EventNodeState.StateType.Previous));

				var current = new EventNodeState(n.Event, n.Node, EventNodeState.StateType.Current);
				AddEventNodeState(current);

				if (n.Node.Expression != null)
				{
					d_eventEquationStates.Add(current);
				}

				AddEventNodeState(new EventNodeState(n.Event, n.Node, EventNodeState.StateType.Distance));

				if (n.Node.Left != null)
				{
					states.Enqueue(new EventNode { Event = n.Event, Node = n.Node.Left});
				}

				if (n.Node.Right != null)
				{
					states.Enqueue(new EventNode { Event = n.Event, Node = n.Node.Right});
				}
			}
		}

		private string SliceKey(Cdn.EdgeAction action)
		{
			var indices = action.Indices;

			if (indices == null)
			{
				return "";
			}
			else
			{
				return String.Join(",", Array.ConvertAll<int, string>(indices, a => a.ToString()));
			}
		}

		private List<List<Cdn.EdgeAction>> SplitActionsPerSlice(IEnumerable<Cdn.EdgeAction> actions)
		{
			var ret = new List<List<Cdn.EdgeAction>>();
			var map = new Dictionary<string, List<Cdn.EdgeAction>>();

			foreach (var action in actions)
			{
				List<Cdn.EdgeAction> lst;
				var key = SliceKey(action);

				if (!map.TryGetValue(key, out lst))
				{
					lst = new List<EdgeAction>();
					map[key] = lst;

					ret.Add(lst);
				}

				lst.Add(action);
			}

			return ret;
		}

		private void PromoteEdgeSlices()
		{
			// This function splits all the edge actions on variables based
			// on the slice on which the operate on the target variable.
			var cp = d_actionedVariables;
			d_actionedVariables = new Dictionary<Variable, EdgeAction[]>();

			foreach (var pair in cp)
			{
				var variable = pair.Key;
				var actions = pair.Value;

				var split = SplitActionsPerSlice(actions);

				if (split.Count == 1)
				{
					// That's ok, no diversity means we can just use the
					// normal code path
					d_actionedVariables[pair.Key] = pair.Value;
					continue;
				}

				// Contains a set of instructions for each value in the matrix
				// representing the final, new equation. Each value in the matrix
				// is a set of instructions because it can be a sum
				List<Cdn.Instruction>[] instructions = new List<Instruction>[variable.Dimension.Size()];

				// Ai! Cool stuff needs to happen here. Split out new
				// variables for these equations and setup a new edge action
				// representing the fully combined set. Complex you say? Indeed!
				foreach (var elem in split)
				{
					// Combine elements with the same indices by simply doing a sum
					var eqs = Array.ConvertAll<Cdn.EdgeAction, Cdn.Expression>(elem.ToArray(), a => a.Equation);
					Cdn.Expression sum = Cdn.Expression.Sum(eqs);

					// Now make a variable for it
					var nv = new Cdn.Variable(UniqueVariableName(variable.Object, String.Format("__d{0}", variable.Name)), sum, VariableFlags.None);
					variable.Object.AddVariable(nv);

					// Add relevant instructions as per slice
					var slice = elem[0].Indices;

					if (slice == null || slice.Length == 0)
					{
						// Empty slice is just the full range
						slice = new int[variable.Dimension.Size()];

						for (int i = 0; i < slice.Length; ++i)
						{
							slice[i] = i;
						}
					}

					for (int i = 0 ; i < slice.Length; ++i)
					{
						if (instructions[slice[i]] == null)
						{
							instructions[slice[i]] = new List<Instruction>();
						}

						var vinstr = new Cdn.InstructionVariable(nv);

						vinstr.SetSlice(new int[] {i}, new Cdn.Dimension { Rows = 1, Columns = 1});
						instructions[slice[i]].Add(vinstr);
					}
				}

				List<Cdn.Instruction> ret = new List<Instruction>();

				// Create substitute edge action
				foreach (var i in instructions)
				{
					if (i == null)
					{
						ret.Add(new Cdn.InstructionNumber("0"));
					}
					else
					{
						// Add indexing instructions
						ret.AddRange(i);

						// Then add simple plus operators
						for (int j = 0; j < i.Count - 1; ++j)
						{
							ret.Add(new Cdn.InstructionFunction((uint)Cdn.MathFunctionType.Plus, null, 2));
						}
					}
				}

				var minstr = new Cdn.InstructionMatrix(new Cdn.StackArgs(instructions.Length), variable.Dimension);
				ret.Add(minstr);

				var retex = new Cdn.Expression("");
				retex.SetInstructionsTake(ret.ToArray());

				var action = new Cdn.EdgeAction(variable.Name, retex);
				((Cdn.Node)variable.Object).SelfEdge.AddAction(action);

				d_actionedVariables[variable] = new EdgeAction[] {action};
			}
		}

		private void Scan()
		{
			// We also scan the integrator because the 't' and 'dt' properties are defined there
			Scan(d_network.Integrator);
			Scan(d_network);

			PromoteEdgeSlices();
			PromoteConstraints();

			ExtractStates();
			ExtractDelayedStates();
			ExtractEventStates();

			ExtractInitialize();
			ExtractRand();
		}
		
		private Cdn.Variable PromoteConstraint(Cdn.Variable variable)
		{
			var c = variable.Constraint;

			if (c == null)
			{
				return null;
			}

			// Create a separate variable for the actual expression of
			// 'variable' and set the new expression of 'variable' to the
			// constraint expression.
			var nv = new Cdn.Variable(String.Format("_{0}_unc", variable.Name), variable.Expression.Copy(), VariableFlags.None);
			variable.Object.AddVariable(nv);
			
			var instrs = variable.Constraint.Instructions;
			
			for (int i = 0; i < instrs.Length; ++i)
			{
				Cdn.InstructionVariable vinstr;

				vinstr = instrs[i] as InstructionVariable;

				if (vinstr != null && vinstr.Variable == variable)
				{
					instrs[i] = (Cdn.Instruction)vinstr.Copy();
					((InstructionVariable)instrs[i]).Variable = nv;
				}
			}

			var expr = variable.Constraint.Copy();
			expr.Instructions = instrs;

			variable.Expression = expr;

			Cdn.EdgeAction[] actions;

			if (!variable.Integrated && d_actionedVariables.TryGetValue(variable, out actions))
			{
				foreach (var action in actions)
				{
					// Redirect edge to the unconstraint variable for direct edges
					action.Target = nv.Name;
				}
			}

			return nv;
		}

		private void PromoteConstraints()
		{
            var added = new List<Variable>();

			foreach (var variable in d_variables)
			{
				var nv = PromoteConstraint(variable);

				if (nv != null)
				{
					added.Add(nv);
				}
			}

			d_variables.AddRange(added);
		}

		private void ExtractInitialize()
		{
			foreach (var state in d_states)
			{
				if ((d_randStateSet != null && d_randStateSet.Contains(state.Object)) || state is DelayedState)
				{
					continue;
				}

				if ((state.Type & Cdn.RawC.State.Flags.Derivative) != 0)
				{
					continue;
				}

				if ((state.Type & Cdn.RawC.State.Flags.EventNode) != 0)
				{
					continue;
				}

				if (state == Time || state == TimeStep)
				{
					continue;
				}

				if (state.Object != null)
				{
					var v = state.Object as Variable;
					Instruction[] instrs;
					Cdn.EdgeAction[] actions = null;

					if (v == null || !v.Integrated || state.Actions.Length == 0)
					{
						instrs = state.Instructions;
						actions = state.Actions;
					}
					else
					{
						instrs = v.Expression.Instructions;
					}

					AddInitialize(new State(state.Object, instrs, state.Type | RawC.State.Flags.Initialization, actions));
				}
			}
		}

		public State Time
		{
			get { return d_stateMap[d_network.Integrator.Variable("t")]; }
		}

		public State TimeStep
		{
			get { return d_stateMap[d_network.Integrator.Variable("dt")]; }
		}

		private double ComputeDelayedDelay(Instruction[] instructions, Expression e, InstructionCustomOperator instr)
		{
			Cdn.Stack stack = new Cdn.Stack(e.StackSize);

			foreach (Cdn.Instruction i in instructions)
			{
				if (i == instr)
				{
					return stack.Pop();
				}

				i.Execute(stack);
			}

			return 0;
		}

		private void ExtractDelayedState(State st, HashSet<DelayedState.Key> same)
		{
			if (st.Instructions == null)
			{
				return;
			}

			foreach (Cdn.Instruction instruction in st.Instructions)
			{
				InstructionCustomOperator op = instruction as InstructionCustomOperator;

				if (op == null || !(op.Operator is OperatorDelayed))
				{
					continue;
				}

				if (Options.Instance.DelayTimeStep <= 0)
				{
					throw new Exception("The network uses the `delayed' operator but no delay time step was specified (--delay-time-step)...");
				}

				double delay = ComputeDelayedDelay(st.Instructions, st.Expression, op);

				OperatorDelayed opdel = (OperatorDelayed)op.Operator;
				DelayedState.Key key = new DelayedState.Key(opdel, delay);

				if (!same.Add(key))
				{
					d_delays.Add(op, delay);
					continue;
				}

				double size;

				size = delay / Options.Instance.DelayTimeStep;

				if (size % 1 > double.Epsilon)
				{
					throw new Exception(String.Format("Time delay `{0}' is not a multiple of the delay time step `{1}'",
					                    delay, Options.Instance.DelayTimeStep));
				}

				d_delays.Add(op, delay);

				DelayedState s = new DelayedState(op, delay, opdel.Expression, Cdn.RawC.State.Flags.None);
				AddState(null, s);

				// Create a new expression for the initial value of this state
				AddInitialize(new DelayedState(op, delay, opdel.InitialValue, Cdn.RawC.State.Flags.Initialization));
				d_delayedStates.Add(s);

				// Recurse into state
				ExtractDelayedState(s, same);
			}
		}
		
		private void ExtractDelayedStates()
		{
			HashSet<DelayedState.Key> same = new HashSet<DelayedState.Key>();

			foreach (State st in new List<State>(States))
			{
				ExtractDelayedState(st, same);
			}
		}

		public bool LookupDelay(Instruction instruction, out double delay)
		{
			while (true)
			{
				Instruction mapped;

				if (d_delays.TryGetValue(instruction, out delay))
				{
					return true;
				}

				if (d_instructionMapping.TryGetValue(instruction, out mapped))
				{
					instruction = mapped;
				}
				else
				{
					return false;
				}
			}
		}

		private void AddFlaggedVariable(Cdn.Variable property)
		{
			foreach (Cdn.VariableFlags flags in Enum.GetValues(typeof(Cdn.VariableFlags)))
			{
				if ((property.Flags & flags) != 0)
				{
					List<Cdn.Variable> lst;

					if (!d_flaggedVariables.TryGetValue(flags, out lst))
					{
						lst = new List<Variable>();
						d_flaggedVariables[flags] = lst;
					}

					lst.Add(property);
				}
			}
		}

		private EventState AddEventState(Cdn.Event ev)
		{
			EventStateContainer states;

			var node = ev.Parent;
			var state = ev.GotoState;

			if (!d_eventStatesMap.TryGetValue(node, out states))
			{
				states = new EventStateContainer {
					States = new List<string>(),
					Index = d_eventStatesMap.Count,
				};

				d_eventStatesMap[node] = states;
			}
			
			if (!states.States.Contains(state))
			{
				states.States.Add(state);

				var evs = new EventState {
					Node = node,
					Name = state,
					ActiveActions = new List<Cdn.EdgeAction>(),
					Index = d_eventStates.Count,
				};

				d_eventStates.Add(evs);
				d_eventStateIdMap[EventStateId(evs.Node, evs.Name)] = evs;
				return evs;
			}
			else
			{
				return d_eventStateIdMap[EventStateId(node, state)];
			}
		}

		private void ScanEvent(Cdn.Event ev)
		{
			if (ev == null)
			{
				return;
			}

			d_events.Add(ev);

			if (!String.IsNullOrEmpty(ev.GotoState))
			{
				AddEventState(ev);
			}

			var vars = ev.SetVariables;
			var lst = new List<EventSetState>();

			foreach (var v in vars)
			{
				lst.Add(new EventSetState(v));
			}

			d_eventSetStates[ev] = lst;
		}

		public Dictionary<Cdn.Event, List<EventSetState>> EventSetStates
		{
			get { return d_eventSetStates; }
		}

		private void Scan(Cdn.Object obj)
		{
			Cdn.Function f = obj as Cdn.Function;

			if (f != null)
			{
				foreach (var v in obj.Variables)
				{
					if ((v.Flags & Cdn.VariableFlags.FunctionArgument) == 0)
					{
						d_functionHelperVariables.Add(v);
					}
				}

				return;
			}
			else
			{
				d_variables.AddRange(obj.Variables);
			}

			ScanEvent(obj as Cdn.Event);

			foreach (Cdn.Variable prop in obj.Variables)
			{
				if (prop.Actions.Length != 0)
				{
					d_actionedVariables[prop] = prop.Actions;
				}

				AddFlaggedVariable(prop);
			}
			
			Cdn.Node grp = obj as Cdn.Node;
			
			if (grp == null)
			{
				return;
			}

			if (grp.HasSelfEdge)
			{
				Scan((Cdn.Object)grp.SelfEdge);
			}

			foreach (Cdn.Object child in grp.Children)
			{
				Scan(child);
			}
		}

		public Knowledge(Cdn.Network network)
		{
			d_network = network;
		}
		
		public IEnumerable<Cdn.Variable> Variables
		{
			get
			{
				return d_variables;
			}
		}

		public int FlaggedVariablesCount(Cdn.VariableFlags flags)
		{
			List<Cdn.Variable> lst;

			if (d_flaggedVariables.TryGetValue(flags, out lst))
			{
				return lst.Count;
			}

			return 0;
		}

		public IEnumerable<Cdn.Variable> FlaggedVariables(Cdn.VariableFlags flags)
		{
			List<Cdn.Variable> lst;

			if (d_flaggedVariables.TryGetValue(flags, out lst))
			{
				foreach (Cdn.Variable prop in lst)
				{
					yield return prop;
				}
			}
		}

		public Dictionary<Cdn.EdgeAction, Cdn.Variable> EventActionProperties
		{
			get { return d_eventActionProperties; }
		}

		public State State(object o)
		{
			if (o == null)
			{
				return null;
			}

			State state = null;
			d_stateMap.TryGetValue(o, out state);
			
			return state;
		}

		public State InitializeState(object o)
		{
			if (o == null)
			{
				return null;
			}

			State state = null;
			d_initializeMap.TryGetValue(o, out state);
			
			return state;
		}

		public IEnumerable<State> FlaggedStates(Cdn.VariableFlags flags)
		{
			foreach (var v in FlaggedVariables(flags))
			{
				State s = this.State(v);

				if (s != null)
				{
					yield return s;
				}
			}
		}

		public IEnumerable<State> DerivativeStates
		{
			get { return d_derivativeStates; }
		}

		public int DerivativeStatesCount
		{
			get { return d_derivativeStates.Count; }
		}

		public State DerivativeState(object o)
		{
			State ret;

			if (d_derivativeMap.TryGetValue(o, out ret))
			{
				return ret;
			}
			else
			{
				return null;
			}
		}

		public IEnumerable<State> Integrated
		{
			get { return d_integrated; }
		}

		public int IntegratedCount
		{
			get { return d_integrated.Count; }
		}

		public Cdn.Network Network
		{
			get
			{
				return d_network;
			}
		}

		public Dictionary<Node, EventStateContainer> EventStatesMap
		{
			get { return d_eventStatesMap; }
		}

		public int EventContainersCount
		{
			get
			{
				return d_eventStatesMap.Count;
			}
		}

		public List<EventState> EventStates
		{
			get
			{
				return d_eventStates;
			}
		}
		
		public IEnumerable<EventStateGroup> EventStateGroups
		{
			get
			{
				foreach (var pair in d_eventStateGroups)
				{
					yield return pair.Value;
				}
			}
		}
		
		public int EventStateGroupsCount
		{
			get { return d_eventStateGroups.Count; }
		}

		public IEnumerable<State> EventEquationStates
		{
			get { return d_eventEquationStates; }
		}

		public IEnumerable<EventNodeState> EventNodeStates
		{
			get { return d_eventNodeStates; }
		}

		public int EventNodeStatesCount
		{
			get { return d_eventNodeStates.Count; }
		}

		public IEnumerable<Cdn.Event> Events
		{
			get { return d_events; }
		}

		public int EventsCount
		{
			get { return d_events.Count; }
		}
		
		public Cdn.Expression ExpandExpression(params Cdn.Expression[] expressions)
		{
			return ExpandExpression(null, expressions);
		}
		
		public Cdn.Expression ExpandExpression(Dictionary<Instruction, Instruction> instmap, params Cdn.Expression[] expressions)
		{
			if (expressions.Length == 0)
			{
				var ret = new Cdn.Expression("0");
				ret.Compile(null, null);
				return ret;
			}
			
			List<Cdn.Expression> inlined = new List<Expression>();

			for (int i = 0; i < expressions.Length; ++i)
			{
				inlined.Add(Inline(instmap, expressions[i]));
			}
			
			return Cdn.Expression.Sum(inlined.ToArray());
		}
		
		private Cdn.Expression Inline(Dictionary<Instruction, Instruction> instmap, Cdn.Expression expr)
		{
			List<Cdn.Instruction> instructions = new List<Instruction>();

			foreach (Instruction inst in expr.Instructions)
			{
				InstructionVariable variable = inst as InstructionVariable;
				
				if (variable != null)
				{
					// See if we need to expand it
					Variable v = variable.Variable;

					if (State(v) == null)
					{
						var sub = Inline(instmap, v.Expression);
						
						// Expand the instruction
						foreach (var i in sub.Instructions)
						{
							instructions.Add(i);
						}

						continue;
					}
				}

				var cp = inst.Copy() as Instruction;

				if (instmap != null)
				{
					instmap.Add(inst, cp);
				}

				instructions.Add(cp);
			}
			
			var e = new Cdn.Expression("");
			e.SetInstructionsTake(instructions.ToArray());
			
			return e;
		}
	}
}

