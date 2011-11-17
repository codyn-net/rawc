using System;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class Knowledge
	{
		private static Knowledge s_instance;
		private Cpg.Network d_network;
		private List<State> d_integrated;
		private List<State> d_direct;
		private List<State> d_initialize;
		private List<State> d_precomputeBeforeDirect;
		private List<State> d_precomputeBeforeIntegrated;
		private List<State> d_precomputeAfterIntegrated;
		private List<State> d_delayed;
		private Dictionary<Cpg.Property, State> d_stateMap;
		private Dictionary<Cpg.PropertyFlags, List<Cpg.Property>> d_flaggedproperties;
		private List<Cpg.Property> d_properties;

		public static Knowledge Initialize(Cpg.Network network)
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
			
			d_stateMap = new Dictionary<Property, State>();

			d_properties = new List<Property>();
			d_flaggedproperties = new Dictionary<PropertyFlags, List<Property>>();

			Scan();
		}

		private IEnumerable<Link> LinksForProxies(Cpg.Object obj)
		{
			if (obj == null || obj.Parent == null)
			{
				yield break;
			}

			foreach (Link link in obj.Parent.Links)
			{
				yield return link;
			}

			foreach (Link link in LinksForProxies(obj.Parent))
			{
				yield return link;
			}
		}

		private IEnumerable<Link> LinksForProperty(Property prop)
		{
			foreach (Link link in prop.Object.Links)
			{
				yield return link;
			}

			foreach (Link link in LinksForProxies(prop.Object))
			{
				yield return link;
			}
		}
		
		private State ExpandedState(Property prop)
		{
			List<LinkAction > actions = new List<LinkAction>();

			foreach (Link link in LinksForProperty(prop))
			{
				foreach (LinkAction action in link.Actions)
				{
					if (action.TargetProperty == prop)
					{
						actions.Add(action);
					}
				}
			}
			
			return new State(prop, actions.ToArray());
		}
		
		private State AddState(Cpg.Property prop)
		{
			State state = ExpandedState(prop);
			d_stateMap[prop] = state;
			
			return state;
		}

		private void AllDependencies(Cpg.Expression expr, Dictionary<Cpg.Property, bool> ret)
		{
			foreach (Instruction instr in expr.Instructions)
			{
				InstructionProperty p = instr as InstructionProperty;

				if (p != null && !ret.ContainsKey(p.Property))
				{
					ret[p.Property] = true;
					AllDependencies(p.Property.Expression, ret);
				}
			}
		}

		private List<State> SortOnDependencies(List<State> lst)
		{
			List<State > ret = new List<State>();
			List<Dictionary<Cpg.Property, bool >> deps = new List<Dictionary<Cpg.Property, bool>>();
			
			foreach (State st in lst)
			{
				bool found = false;

				Dictionary<Cpg.Property, bool > pdeps = new Dictionary<Cpg.Property, bool>();
				AllDependencies(st.Property.Expression, pdeps);

				for (int i = 0; i < ret.Count; ++i)
				{
					if (deps[i].ContainsKey(st.Property))
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
			
			foreach (Property prop in state.IntegratedProperties())
			{
				d_integrated.Add(AddState(prop));
			}
			
			foreach (Property prop in state.DirectProperties())
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
				
				foreach (Cpg.Instruction instruction in st.Instructions)
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
						d_initialize.Add(new DelayedState(opdel, Cpg.RawC.State.Flags.Initialization));
					}
				}
			}
		}
		
		private bool DependsOn(Cpg.Expression expression, Cpg.RawC.State.Flags flags)
		{
			// Check if the expression depends on any other property that has direct actors
			foreach (Cpg.Property dependency in expression.Dependencies)
			{
				State state;

				if (d_stateMap.TryGetValue(dependency, out state))
				{
					if ((state.Type & flags) != 0)
					{
						return true;
					}
				}
				
				if (DependsOn(dependency.Expression, flags))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public bool DependsDirect(Cpg.Expression expression)
		{
			return DependsOn(expression, Cpg.RawC.State.Flags.Direct);
		}
		
		public bool DependsIntegrated(Cpg.Expression expression)
		{
			return DependsOn(expression, Cpg.RawC.State.Flags.Integrated);
		}

		public bool DependsTime(Cpg.Expression expression)
		{
			Cpg.Property tprop = d_network.Integrator.Property("t");
			Cpg.Property dtprop = d_network.Integrator.Property("dt");

			return Array.IndexOf(expression.Dependencies, tprop) != -1 ||
				   Array.IndexOf(expression.Dependencies, dtprop) != -1;
		}
		
		public bool DependsIn(Cpg.Expression expression)
		{
			foreach (Cpg.Property dependency in expression.Dependencies)
			{
				if ((dependency.Flags & Cpg.PropertyFlags.In) != 0)
				{
					return true;
				}
				
				if (DependsIn(dependency.Expression))
				{
					return true;
				}
			}
			
			return false;
		}
		
		private bool AnyStateDepends(IEnumerable<State> states, Cpg.Property property)
		{
			foreach (State state in states)
			{
				if (Array.IndexOf(state.Expression.Dependencies, property) != -1)
				{
					return true;
				}
			}
			
			return false;
		}

		private void AddFlaggedProperty(Cpg.Property property)
		{
			foreach (Cpg.PropertyFlags flags in Enum.GetValues(typeof(Cpg.PropertyFlags)))
			{
				if ((property.Flags & flags) != 0)
				{
					List<Cpg.Property> lst;

					if (!d_flaggedproperties.TryGetValue(flags, out lst))
					{
						lst = new List<Property>();
						d_flaggedproperties[flags] = lst;
					}

					lst.Add(property);
				}
			}
		}

		private void ScanProperties(Cpg.Object obj)
		{
			d_properties.AddRange(obj.Properties);
			
			foreach (Cpg.Property prop in obj.Properties)
			{
				AddFlaggedProperty(prop);
				bool needsinit = NeedsInitialization(prop, true);
				bool isvar = IsVariadic(prop.Expression, true);
				bool isin = (prop.Flags & Cpg.PropertyFlags.In) != 0;
				bool isout = (prop.Flags & Cpg.PropertyFlags.Out) != 0;
				bool isonce = (prop.Flags & Cpg.PropertyFlags.Once) != 0;

				if ((isout || (needsinit && isvar)) && !isin && !isonce &&
					!d_stateMap.ContainsKey(prop))
				{
					bool dependsdirect = DependsDirect(prop.Expression);
					bool dependsintegrated = DependsIntegrated(prop.Expression);
					bool dependsin = DependsIn(prop.Expression);
					bool dependstime = DependsTime(prop.Expression);

					bool beforedirect = dependsin && AnyStateDepends(d_direct, prop);

					if (beforedirect)
					{
						d_precomputeBeforeDirect.Add(new State(prop, RawC.State.Flags.BeforeDirect));
					}

					if (dependsdirect && AnyStateDepends(d_integrated, prop))
					{
						d_precomputeBeforeIntegrated.Add(new State(prop, RawC.State.Flags.BeforeIntegrated));
					}
				
					if ((dependsin && !beforedirect) || dependstime || isvar || dependsintegrated)
					{
						d_precomputeAfterIntegrated.Add(new State(prop, RawC.State.Flags.AfterIntegrated));
					}
				}

				if (needsinit)
				{
					d_initialize.Add(new State(prop, RawC.State.Flags.Initialization));
				}
			}
			
			Cpg.Group grp = obj as Cpg.Group;
			
			if (grp == null)
			{
				return;
			}
			
			foreach (Cpg.Object child in grp.Children)
			{
				ScanProperties(child);
			}
		}

		public Knowledge(Cpg.Network network)
		{
			d_network = network;
		}
		
		public IEnumerable<Cpg.Property> Properties
		{
			get
			{
				return d_properties;
			}
		}

		public IEnumerable<Cpg.Property> FlaggedProperties(Cpg.PropertyFlags flags)
		{
			List<Cpg.Property> lst;

			if (d_flaggedproperties.TryGetValue(flags, out lst))
			{
				foreach (Cpg.Property prop in lst)
				{
					yield return prop;
				}
			}
		}

		public State State(Cpg.Property property)
		{
			State state = null;
			d_stateMap.TryGetValue(property, out state);
			
			return state;
		}
		
		public bool IsVariadic(Cpg.Property property)
		{
			// A property is variadic if it is acted upon, or if it is an IN
			State state = State(property);
			
			if (state != null)
			{
				return true;
			}
			
			return (property.Flags & Cpg.PropertyFlags.In) != 0;
		}

		public bool IsVariadic(Cpg.Expression expression, bool samestep)
		{
			// See if the expression is variadic. An expression is variadic if it depends on a variadic operator/function
			foreach (Instruction inst in expression.Instructions)
			{
				if (inst is InstructionVariadicFunction)
				{
					return true;
				}
				
				InstructionCustomOperator icop = inst as InstructionCustomOperator;
				
				if (icop != null)
				{
					foreach (Cpg.Expression ex in icop.Operator.Expressions)
					{
						if (IsVariadic(ex, samestep))
						{
							return true;
						}
					}
				}
			}
			
			// Check if any of its dependencies are then variadic maybe
			foreach (Property property in expression.Dependencies)
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
		
		public bool IsPersist(Property property)
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
			
			if ((property.Flags & (PropertyFlags.In | PropertyFlags.Out | PropertyFlags.Once)) != PropertyFlags.None)
			{
				return true;
			}
			
			return NeedsInitialization(property, false);
		}

		public bool NeedsInitialization(Cpg.Expression expression, bool alwaysDynamic)
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
		
		public bool NeedsInitialization(Property property, bool alwaysDynamic)
		{
			if (property == null)
			{
				return false;
			}

			if (property.Object is Cpg.Link)
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
		
		public Cpg.Network Network
		{
			get
			{
				return d_network;
			}
		}
	}
}

