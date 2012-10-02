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
		private List<State> d_randStates;
		private List<State> d_delayedStates;
		private Dictionary<Instruction, Instruction> d_instructionMapping;
		private Dictionary<Instruction, double> d_delays;
		private Dictionary<object, State> d_stateMap;
		private Dictionary<object, State> d_initializeMap;
		private Dictionary<Cdn.VariableFlags, List<Cdn.Variable>> d_flaggedVariables;
		private List<Cdn.Variable> d_variables;
		private Dictionary<Cdn.Expression, HashSet<Cdn.Variable>> d_dependencyCache;
		private Dictionary<Cdn.Expression, HashSet<Cdn.Expression>> d_expressionDependencyCache;
		private HashSet<object> d_randStateSet;

		public static Knowledge Initialize(Cdn.Network network)
		{
			s_instance = new Knowledge(network);
			s_instance.Init();

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
			d_flaggedVariables = new Dictionary<VariableFlags, List<Variable>>();
			d_dependencyCache = new Dictionary<Expression, HashSet<Variable>>();
			d_expressionDependencyCache = new Dictionary<Cdn.Expression, HashSet<Cdn.Expression>>();
			d_states = new List<State>();
			d_auxStates = new List<State>();
			d_initialize = new List<State>();
			d_randStates = new List<State>();
			d_delayedStates = new List<State>();
			d_instructionMapping = new Dictionary<Instruction, Instruction>();

			Scan();
		}

		private IEnumerable<Edge> EdgesForProxies(Cdn.Object obj)
		{
			if (obj == null || obj.Parent == null)
			{
				yield break;
			}

			foreach (Edge link in obj.Parent.Edges)
			{
				yield return link;
			}

			foreach (Edge link in EdgesForProxies(obj.Parent))
			{
				yield return link;
			}
		}

		private IEnumerable<Edge> EdgesForVariableAll(Variable prop)
		{
			Node node = prop.Object as Node;

			if (node == null)
			{
				yield break;
			}

			foreach (Edge link in node.Edges)
			{
				yield return link;
			}

			foreach (Edge link in EdgesForProxies(prop.Object))
			{
				yield return link;
			}
		}

		private IEnumerable<Edge> EdgesForVariable(Variable prop)
		{
			var s = new HashSet<Edge>();

			foreach (Edge item in EdgesForVariableAll(prop))
			{
				if (s.Add(item))
				{
					yield return item;
				}
			}
		}

		private State ExpandedState(Variable prop)
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
			
			return new State(prop, actions.ToArray());
		}

		public void UpdateInstructionMap(Dictionary<Instruction, Instruction> mapping)
		{
			foreach (KeyValuePair<Instruction, Instruction> pair in mapping)
			{
				d_instructionMapping[pair.Key] = pair.Value;
			}
		}

		private bool SortDependsOn(List<State> lst, State s)
		{
			foreach (State l in lst)
			{
				if (DependsOn(l, s.Object))
				{
					return true;
				}
			}

			return false;
		}

		public List<List<State>> SortOnDependencies(IEnumerable<State> lst)
		{
			List<List<State>> ret = new List<List<State>>();

			foreach (State st in lst)
			{
				bool found = false;

				foreach (List<State> got in ret)
				{
					if (!SortDependsOn(got, st))
					{
						got.Add(st);
						found = true;
						break;
					}
				}

				if (!found)
				{
					List<State> got = new List<State>();
					got.Add(st);

					ret.Add(got);
				}

			}

			ret.Reverse();
			return ret;
		}

		private void AddInitialize(State state)
		{
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

		private void ExtractStates()
		{
			HashSet<object> unique = new HashSet<object>();

			// Add integrated state variables
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Integrated))
			{
				AddState(unique, ExpandedState(v));
			}

			// Add in variables
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.In))
			{
				AddState(unique, ExpandedState(v));
			}

			// Add out variables
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Out))
			{
				State s = ExpandedState(v);

				AddState(unique, s);
				d_auxStates.Add(s);
			}

			// Add once variables
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Once))
			{
				AddState(unique, ExpandedState(v));
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

		private void Scan()
		{
			// We also scan the integrator because the 't' and 'dt' properties are defined there
			ScanVariables(d_network.Integrator);
			ScanVariables(d_network);

			ExtractStates();
			ExtractDelayedStates();

			ExtractInitialize();
			ExtractRand();
		}

		private void ExtractInitialize()
		{
			foreach (var state in d_states)
			{
				if ((d_randStateSet != null && d_randStateSet.Contains(state.Object)) || state is DelayedState)
				{
					continue;
				}

				if (state.Object != null)
				{
					var v = state.Object as Variable;

					if (v == null || v.Object != d_network.Integrator || v.Name != "t")
					{
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
				;

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

		public bool DependsOn(State state, Cdn.Instruction i)
		{
			if (state == null || i == null)
			{
				return false;
			}

			DelayedState ds = state as DelayedState;
			Instruction[] instructions;

			if (ds != null && (ds.Type & RawC.State.Flags.Initialization) != 0)
			{
				if (ds.Operator.InitialValue != null)
				{
					instructions = ds.Operator.InitialValue.Instructions;
				}
				else
				{
					instructions = new Instruction[] {};
				}
			}
			else
			{
				instructions = state.Instructions;
			}


			foreach (Cdn.Instruction instr in instructions)
			{
				if (instr == i)
				{
					return true;
				}

				InstructionVariable v = instr as InstructionVariable;
				State s = Knowledge.Instance.State(v);

				if (s != null)
				{
					if (DependsOn(s, i))
					{
						return true;
					}
				}

				InstructionRand r = instr as InstructionRand;
				s = Knowledge.Instance.State(r);

				if (s != null)
				{
					if (DependsOn(s, i))
					{
						return true;
					}
				}
			}

			return false;
		}

		public bool DependsOn(State state, object o)
		{
			if (state == null || o == null)
			{
				return false;
			}

			Variable v = o as Variable;

			if (v != null)
			{
				return DependsOn(state, v);
			}

			Instruction instr = o as Instruction;

			if (instr != null)
			{
				return DependsOn(state, instr);
			}

			return false;
		}
		
		public bool DependsOn(State state, Cdn.Variable other)
		{
			if (state == null || other == null)
			{
				return false;
			}

			// Check if the expression depends on any other property that has direct actors
			HashSet<Variable> deps = RecursiveDependencies(state.Expression);

			return deps.Contains(other);
		}
		
		private void RecursiveDependencies(Cdn.Expression expression, HashSet<Cdn.Variable> un)
		{
			foreach (Cdn.Variable v in expression.VariableDependencies)
			{
				if (un.Add(v))
				{
					RecursiveDependencies(v.Expression, un);
				}
			}
		}

		private HashSet<Cdn.Variable> RecursiveDependencies(Cdn.Expression expression)
		{
			HashSet<Cdn.Variable> ret;

			if (d_dependencyCache.TryGetValue(expression, out ret))
			{
				return ret;
			}

			ret = new HashSet<Cdn.Variable>();
			RecursiveDependencies(expression, ret);

			d_dependencyCache[expression] = ret;
			return ret;
		}

		private void RecursiveExpressionDependencies(Cdn.Expression expression, HashSet<Cdn.Expression> un)
		{
			foreach (Cdn.Expression e in expression.Dependencies)
			{
				if (un.Add(e))
				{
					RecursiveExpressionDependencies(e, un);
				}
			}
		}

		private HashSet<Cdn.Expression> RecursiveExpressionDependencies(Cdn.Expression expression)
		{
			HashSet<Cdn.Expression> ret;

			if (d_expressionDependencyCache.TryGetValue(expression, out ret))
			{
				return ret;
			}

			ret = new HashSet<Cdn.Expression>();
			ret.Add(expression);
			RecursiveExpressionDependencies(expression, ret);

			d_expressionDependencyCache[expression] = ret;
			return ret;
		}

		public bool DependsDelay(Cdn.Expression expression)
		{
			HashSet<Cdn.Expression> deps = RecursiveExpressionDependencies(expression);

			foreach (Cdn.Expression e in deps)
			{
				foreach (Cdn.Instruction i in e.Instructions)
				{
					InstructionCustomOperator op = i as InstructionCustomOperator;

					if (op != null && op.Operator is OperatorDelayed)
					{
						return true;
					}
				}
			}

			return false;
		}
		
		public bool DependsIn(Cdn.Expression expression)
		{
			foreach (Cdn.Variable dependency in RecursiveDependencies(expression))
			{
				if ((dependency.Flags & Cdn.VariableFlags.In) != 0)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private bool AnyStateDepends(IEnumerable<State> states, Cdn.Variable property)
		{
			foreach (State state in states)
			{
				if (RecursiveDependencies(state.Expression).Contains(property))
				{
					return true;
				}
			}
			
			return false;
		}

		private bool AnyVariableDepends(IEnumerable<Cdn.Variable> variables, Cdn.Variable property)
		{
			foreach (Cdn.Variable v in variables)
			{
				if (RecursiveDependencies(v.Expression).Contains(property))
				{
					return true;
				}
			}

			return false;
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

		public Cdn.Network Network
		{
			get
			{
				return d_network;
			}
		}
	}
}

