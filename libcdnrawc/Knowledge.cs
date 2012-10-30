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

			d_instructionMapping = new Dictionary<Instruction, Instruction>();

			Scan();
		}

		private IEnumerable<Edge> EdgesForVariable(Variable prop)
		{
			HashSet<Cdn.Edge> unique = new HashSet<Edge>();

			foreach (var action in prop.Actions)
			{
				if (unique.Add(action.Edge))
				{
					yield return action.Edge;
				}
			}
		}

		private delegate State StateCreator(Variable v, EdgeAction[] actions);

		private State ExpandedState(Variable prop)
		{
			return ExpandedState(prop, (v, actions) => {
				return new State(v, actions);
			});
		}

		private State ExpandedState(Variable prop, StateCreator creator)
		{
			List<EdgeAction> actions = new List<EdgeAction>();

			foreach (Edge link in EdgesForVariable(prop))
			{
				foreach (EdgeAction action in link.Actions)
				{
					if (action.TargetVariable == prop)
					{
						actions.Add(action);
					}
				}
			}
			
			return creator(prop, actions.ToArray());
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

		private void ExtractStates()
		{
			HashSet<object> unique = new HashSet<object>();

			// Add integrated state variables
			var integrated = Knowledge.Instance.FlaggedVariables(VariableFlags.Integrated);

			foreach (var v in integrated)
			{
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

			// Add in variables
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.In))
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
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Out))
			{
				if (!unique.Contains(v))
				{
					State s = ExpandedState(v);
					AddState(unique, s);

					AddAux(s, auxset);
				}
			}

			// Add once variables
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Once))
			{
				if (!unique.Contains(v))
				{
					var s = ExpandedState(v);
					AddState(unique, s);
				}
			}
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

		private void Scan()
		{
			// We also scan the integrator because the 't' and 'dt' properties are defined there
			ScanVariables(d_network.Integrator);
			ScanVariables(d_network);

			PromoteConstraints();

			ExtractStates();
			ExtractDelayedStates();

			ExtractInitialize();
			ExtractRand();
		}

		private void PromoteConstraint(Cdn.Variable variable)
		{
			var c = variable.Constraint;

			if (c == null)
			{
				return;
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

			if (!variable.Integrated)
			{
				foreach (var action in variable.Actions)
				{
					// Redirect edge to the unconstraint variable for direct edges
					action.Target = nv.Name;
				}
			}
		}

		private void PromoteConstraints()
		{
			foreach (var variable in d_variables)
			{
				PromoteConstraint(variable);
			}
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

				if (state == Time || state == TimeStep)
				{
					continue;
				}

				if (state.Object != null)
				{
					var v = state.Object as Variable;
					Instruction[] instrs;

					if (v == null || state.Actions.Length == 0)
					{
						instrs = state.Instructions;
					}
					else
					{
						instrs = v.Expression.Instructions;
					}

					AddInitialize(new State(state.Object, instrs, state.Type | RawC.State.Flags.Initialization));
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

		private void ScanVariables(Cdn.Object obj)
		{
			d_variables.AddRange(obj.Variables);
			
			foreach (Cdn.Variable prop in obj.Variables)
			{
				AddFlaggedVariable(prop);
			}
			
			Cdn.Node grp = obj as Cdn.Node;
			
			if (grp == null)
			{
				return;
			}
			
			foreach (Cdn.Object child in grp.Children)
			{
				ScanVariables(child);
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
	}
}

