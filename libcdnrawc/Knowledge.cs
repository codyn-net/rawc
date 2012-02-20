using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class Knowledge
	{
		private static Knowledge s_instance;
		private Cdn.Network d_network;
		private List<State> d_integrated;
		private List<State> d_direct;
		private List<State> d_initialize;
		private List<State> d_precomputeBeforeDirect;
		private List<State> d_precomputeBeforeIntegrated;
		private List<State> d_precomputeAfterIntegrated;
		private List<State> d_delayed;
		private Dictionary<Cdn.Variable, State> d_stateMap;
		private Dictionary<Cdn.VariableFlags, List<Cdn.Variable>> d_flaggedproperties;
		private List<Cdn.Variable> d_properties;
		private Dictionary<Cdn.Expression, HashSet<Cdn.Variable>> d_dependencyCache;

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
			d_integrated = new List<State>();
			d_direct = new List<State>();
			d_initialize = new List<State>();
			d_precomputeBeforeDirect = new List<State>();
			d_precomputeBeforeIntegrated = new List<State>();
			d_precomputeAfterIntegrated = new List<State>();
			d_delayed = new List<State>();
			
			d_stateMap = new Dictionary<Variable, State>();

			d_properties = new List<Variable>();
			d_flaggedproperties = new Dictionary<VariableFlags, List<Variable>>();
			d_dependencyCache = new Dictionary<Expression, HashSet<Variable>>();

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
			foreach (Edge link in ((Node)prop.Object).Edges)
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
			List<EdgeAction > actions = new List<EdgeAction>();

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
		
		private State AddState(Cdn.Variable prop)
		{
			State state = ExpandedState(prop);
			d_stateMap[prop] = state;
			
			return state;
		}

		private List<State> SortOnDependencies(List<State> lst)
		{
			List<State > ret = new List<State>();
			List<HashSet<Cdn.Variable>> deps = new List<HashSet<Cdn.Variable>>();
			
			foreach (State st in lst)
			{
				bool found = false;

				HashSet<Cdn.Variable> pdeps = new HashSet<Cdn.Variable>();
				RecursiveDependencies(st.Variable.Expression, pdeps);

				for (int i = 0; i < ret.Count; ++i)
				{
					if (deps[i].Contains(st.Variable))
					{
						ret.Insert(i, st);
						deps.Insert(i, pdeps);
						found = true;
						break;
					}
				}
				
				if (!found)
				{
					ret.Add(st);
					deps.Add(pdeps);
				}
			}

			return ret;
		}
		
		private void Scan()
		{
			IntegratorState state = d_network.Integrator.State;
			
			foreach (Variable prop in state.IntegratedProperties())
			{
				d_integrated.Add(AddState(prop));
			}
			
			foreach (Variable prop in state.DirectProperties())
			{
				d_direct.Add(AddState(prop));
			}

			// We also scan the integrator because the 't' and 'dt' properties are defined there
			ScanProperties(d_network.Integrator);
			ScanProperties(d_network);
			
			// Sort initialize list on dependencies
			d_initialize = SortOnDependencies(d_initialize);
			d_precomputeAfterIntegrated = SortOnDependencies(d_precomputeAfterIntegrated);
			d_precomputeBeforeDirect = SortOnDependencies(d_precomputeBeforeDirect);
			d_precomputeBeforeIntegrated = SortOnDependencies(d_precomputeBeforeIntegrated);

			ExtractDelayedStates();
		}
		
		private void ExtractDelayedStates()
		{
			Dictionary<DelayedState.Key, bool > same = new Dictionary<DelayedState.Key, bool>();

			foreach (State st in States)
			{
				if (st.Instructions == null)
				{
					continue;
				}
				
				foreach (Cdn.Instruction instruction in st.Instructions)
				{
					InstructionCustomOperator op = instruction as InstructionCustomOperator;
					
					if (op == null || !(op.Operator is OperatorDelayed))
					{
						continue;
					}
					
					if (Options.Instance.FixedTimeStep <= 0)
					{
						throw new Exception("The network uses the `delayed' operator but no fixed time step was specified (--fixed-time-step)...");
					}
					
					OperatorDelayed opdel = (OperatorDelayed)op.Operator;
					DelayedState.Key key = new DelayedState.Key(opdel);
					
					if (same.ContainsKey(key))
					{
						continue;
					}

					double size = (opdel.Delay / Options.Instance.FixedTimeStep);
					
					if (size % 1 > 0.0000001)
					{
						Console.Error.WriteLine("Warning: the delayed time ({0}) is not a multiple of the time step ({1})...",
						                        opdel.Delay,
						                        Options.Instance.FixedTimeStep);
					}
					
					DelayedState s = new DelayedState(opdel);					
					d_delayed.Add(s);
					same.Add(key, true);
					
					if (NeedsInitialization(opdel.InitialValue, true))
					{
						d_initialize.Add(new DelayedState(opdel, Cdn.RawC.State.Flags.Initialization));
					}
				}
			}
		}
		
		private bool DependsOn(Cdn.Expression expression, Cdn.RawC.State.Flags flags)
		{
			// Check if the expression depends on any other property that has direct actors
			foreach (Cdn.Variable dependency in RecursiveDependencies(expression))
			{
				State state;

				if (d_stateMap.TryGetValue(dependency, out state))
				{
					if ((state.Type & flags) != 0)
					{
						return true;
					}
				}
			}
			
			return false;
		}
		
		public bool DependsDirect(Cdn.Expression expression)
		{
			return DependsOn(expression, Cdn.RawC.State.Flags.Direct);
		}
		
		public bool DependsIntegrated(Cdn.Expression expression)
		{
			return DependsOn(expression, Cdn.RawC.State.Flags.Integrated);
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

		public bool DependsTime(Cdn.Expression expression)
		{
			Cdn.Variable tprop = d_network.Integrator.Variable("t");
			Cdn.Variable dtprop = d_network.Integrator.Variable("dt");

			HashSet<Cdn.Variable> deps = RecursiveDependencies(expression);

			return deps.Contains(tprop) || deps.Contains(dtprop);
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

		private void AddFlaggedVariable(Cdn.Variable property)
		{
			foreach (Cdn.VariableFlags flags in Enum.GetValues(typeof(Cdn.VariableFlags)))
			{
				if ((property.Flags & flags) != 0)
				{
					List<Cdn.Variable> lst;

					if (!d_flaggedproperties.TryGetValue(flags, out lst))
					{
						lst = new List<Variable>();
						d_flaggedproperties[flags] = lst;
					}

					lst.Add(property);
				}
			}
		}

		private void ScanProperties(Cdn.Object obj)
		{
			d_properties.AddRange(obj.Variables);
			
			foreach (Cdn.Variable prop in obj.Variables)
			{
				AddFlaggedVariable(prop);

				bool needsinit = NeedsInitialization(prop, true);
				bool isvar = IsVariadic(prop.Expression, true);
				bool isin = (prop.Flags & Cdn.VariableFlags.In) != 0;
				bool isout = (prop.Flags & Cdn.VariableFlags.Out) != 0;
				bool isonce = (prop.Flags & Cdn.VariableFlags.Once) != 0;

				if ((isout || (needsinit && isvar)) && !isin && !isonce &&
					!d_stateMap.ContainsKey(prop))
				{
					bool dependsdirect = DependsDirect(prop.Expression);
					bool dependsintegrated = DependsIntegrated(prop.Expression);
					bool dependsin = DependsIn(prop.Expression);
					bool dependstime = DependsTime(prop.Expression);
					bool directdepends = AnyStateDepends(d_direct, prop);
					bool integrateddepends = AnyStateDepends(d_integrated, prop);

					bool beforedirect = dependsin && directdepends;

					if (beforedirect)
					{
						d_precomputeBeforeDirect.Add(new State(prop, RawC.State.Flags.BeforeDirect));
					}

					if (dependsdirect && integrateddepends)
					{
						d_precomputeBeforeIntegrated.Add(new State(prop, RawC.State.Flags.BeforeIntegrated));
					}
				
					if ((isout || integrateddepends || directdepends) && ((dependsin && !beforedirect) || dependstime || isvar || dependsintegrated))
					{
						d_precomputeAfterIntegrated.Add(new State(prop, RawC.State.Flags.AfterIntegrated));
					}
				}

				if (needsinit)
				{
					d_initialize.Add(new State(prop, RawC.State.Flags.Initialization));
				}
			}
			
			Cdn.Node grp = obj as Cdn.Node;
			
			if (grp == null)
			{
				return;
			}
			
			foreach (Cdn.Object child in grp.Children)
			{
				ScanProperties(child);
			}
		}

		public Knowledge(Cdn.Network network)
		{
			d_network = network;
		}
		
		public IEnumerable<Cdn.Variable> Properties
		{
			get
			{
				return d_properties;
			}
		}

		public IEnumerable<Cdn.Variable> FlaggedProperties(Cdn.VariableFlags flags)
		{
			List<Cdn.Variable> lst;

			if (d_flaggedproperties.TryGetValue(flags, out lst))
			{
				foreach (Cdn.Variable prop in lst)
				{
					yield return prop;
				}
			}
		}

		public State State(Cdn.Variable property)
		{
			State state = null;
			d_stateMap.TryGetValue(property, out state);
			
			return state;
		}
		
		public bool IsVariadic(Cdn.Variable property)
		{
			// A property is variadic if it is acted upon, or if it is an IN
			State state = State(property);
			
			if (state != null)
			{
				return true;
			}
			
			return (property.Flags & Cdn.VariableFlags.In) != 0;
		}

		public bool IsVariadic(Cdn.Expression expression, bool samestep)
		{
			// See if the expression is variadic. An expression is variadic if it depends on a variadic operator/function
			foreach (Instruction inst in expression.Instructions)
			{
				if (inst is InstructionRand)
				{
					return true;
				}
				
				InstructionCustomOperator icop = inst as InstructionCustomOperator;
				
				if (icop != null)
				{
					// TODO: operators
					/*foreach (Cdn.Expression ex in icop.Operator.Expressions)
					{
						if (IsVariadic(ex, samestep))
						{
							return true;
						}
					}*/
				}
			}
			
			// Check if any of its dependencies are then variadic maybe
			foreach (Variable property in expression.VariableDependencies)
			{
				if (IsVariadic(property.Expression, samestep))
				{
					return true;
				}
				
				if (!samestep && IsVariadic(property))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public bool IsPersist(Variable property)
		{
			// A property is persistent (i.e. needs a persistent storage) if:
			//
			// 1) it is a state (has links that act on it)
			// 2) is either IN or OUT
			// 3) needs separate initialization
			State state = State(property);
			
			if (state != null)
			{
				return true;
			}
			
			if ((property.Flags & (VariableFlags.In | VariableFlags.Out | VariableFlags.Once)) != VariableFlags.None)
			{
				return true;
			}
			
			return NeedsInitialization(property, false);
		}

		public bool NeedsInitialization(Cdn.Expression expression, bool alwaysDynamic)
		{
			if (expression == null)
			{
				return false;
			}

			if (alwaysDynamic)
			{
				return true;
			}
			else
			{
				return IsVariadic(expression, true);
			}
		}
		
		public bool NeedsInitialization(Variable property, bool alwaysDynamic)
		{
			if (property == null)
			{
				return false;
			}

			// Always initialize dynamically if the property is persistent
			if (alwaysDynamic)
			{
				return IsPersist(property);
			}
			else
			{
				// Dynamic initialization is needed only if the property is variadic within the same
				// step
				return NeedsInitialization(property.Expression, alwaysDynamic);
			}
		}
		
		public IEnumerable<State> States
		{
			get
			{
				foreach (State state in d_integrated)
				{
					yield return state;
				}

				foreach (State state in d_direct)
				{
					yield return state;
				}
				
				foreach (State state in d_initialize)
				{
					yield return state;
				}
				
				foreach (State state in d_delayed)
				{
					yield return state;
				}
				
				foreach (State state in d_precomputeBeforeDirect)
				{
					yield return state;
				}
				
				foreach (State state in d_precomputeBeforeIntegrated)
				{
					yield return state;
				}
				
				foreach (State state in d_precomputeAfterIntegrated)
				{
					yield return state;
				}
			}
		}

		public IEnumerable<State> IntegratedStates
		{
			get
			{
				return d_integrated;
			}
		}
		
		public IEnumerable<State> DirectStates
		{
			get
			{
				return d_direct;
			}
		}
		
		public int DirectStatesCount
		{
			get
			{
				return d_direct.Count;
			}
		}
		
		public int IntegratedStatesCount
		{
			get
			{
				return d_integrated.Count;
			}
		}
		
		public IEnumerable<State> InitializeStates
		{
			get
			{
				return d_initialize;
			}
		}
		
		public IEnumerable<State> DelayedStates
		{
			get
			{
				return d_delayed;
			}
		}
		
		public int DelayedStatesCount
		{
			get
			{
				return d_delayed.Count;
			}
		}
		
		public int InitializeStatesCount
		{
			get
			{
				return d_initialize.Count;
			}
		}
		
		public IEnumerable<State> PrecomputeBeforeDirectStates
		{
			get
			{
				return d_precomputeBeforeDirect;
			}
		}
		
		public int PrecomputeBeforeDirectStatesCount
		{
			get
			{
				return d_precomputeBeforeDirect.Count;
			}
		}
		
		public IEnumerable<State> PrecomputeBeforeIntegratedStates
		{
			get
			{
				return d_precomputeBeforeIntegrated;
			}
		}
		
		public int PrecomputeBeforeIntegratedStatesCount
		{
			get
			{
				return d_precomputeBeforeIntegrated.Count;
			}
		}
		
		public IEnumerable<State> PrecomputeAfterIntegratedStates
		{
			get
			{
				return d_precomputeAfterIntegrated;
			}
		}
		
		public int PrecomputeAfterIntegratedStatesCount
		{
			get
			{
				return d_precomputeAfterIntegrated.Count;
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

