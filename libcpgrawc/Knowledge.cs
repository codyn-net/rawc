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

		private Dictionary<Cpg.Property, State> d_stateMap;

		private List<Cpg.Property> d_properties;
		private List<Cpg.Property> d_inproperties;
		private List<Cpg.Property> d_outproperties;

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
			
			d_stateMap = new Dictionary<Property, State>();

			d_properties = new List<Property>();
			d_inproperties = new List<Property>();
			d_outproperties = new List<Property>();
			
			Scan();
		}
		
		private State ExpandedState(Property prop)
		{
			Link[] links = prop.Object.Links;
			List<LinkAction> actions = new List<LinkAction>();
			
			foreach (Link link in links)
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
			List<State> initialize = new List<State>();
			
			foreach (State st in d_initialize)
			{
				bool found = false;

				for (int i = 0; i < initialize.Count; ++i)
				{
					if (Array.IndexOf(initialize[i].Property.Expression.Dependencies, st.Property) != -1)
					{
						initialize.Insert(i, st);
						found = true;
						break;
					}
				}
				
				if (!found)
				{
					initialize.Add(st);
				}
			}

			d_initialize = initialize;
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

		private void ScanProperties(Cpg.Object obj)
		{
			d_properties.AddRange(obj.Properties);
			
			foreach (Cpg.Property prop in obj.Properties)
			{
				if ((prop.Flags & Cpg.PropertyFlags.In) != 0)
				{
					d_inproperties.Add(prop);
				}
				
				if ((prop.Flags & Cpg.PropertyFlags.Out) != 0)
				{
					d_outproperties.Add(prop);

					if ((prop.Flags & Cpg.PropertyFlags.In) == 0 && !d_stateMap.ContainsKey(prop))
					{
						bool dependsdirect = DependsDirect(prop.Expression);
						bool dependsintegrated = DependsIntegrated(prop.Expression);
						bool dependsin = DependsIn(prop.Expression);
						
						bool beforedirect = dependsin && AnyStateDepends(d_direct, prop);

						if (beforedirect)
						{
							d_precomputeBeforeDirect.Add(new State(prop, RawC.State.Flags.BeforeDirect));
						}

						if (dependsdirect && AnyStateDepends(d_integrated, prop))
						{
							d_precomputeBeforeIntegrated.Add(new State(prop, RawC.State.Flags.BeforeIntegrated));
						}
					
						if (dependsintegrated || (dependsin && !beforedirect))
						{
							d_precomputeAfterIntegrated.Add(new State(prop, RawC.State.Flags.AfterIntegrated));
						}
					}
				}
				
				if (NeedsInitialization(prop, Options.Instance.AlwaysInitializeDynamically))
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
		
		public IEnumerable<Cpg.Property> InProperties
		{
			get
			{
				return d_inproperties;
			}
		}
		
		public IEnumerable<Cpg.Property> OutProperties
		{
			get
			{
				return d_outproperties;
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
			
			if ((property.Flags & (PropertyFlags.In | PropertyFlags.Out)) != PropertyFlags.None)
			{
				return true;
			}
			
			return NeedsInitialization(property, false);
		}
		
		public bool NeedsInitialization(Property property, bool alwaysDynamic)
		{
			if (property.Object is Cpg.Link)
			{
				return false;
			}

			if (alwaysDynamic)
			{
				// Always initialize dynamically if the property is persistent
				return IsPersist(property);
			}
			else
			{
				// Dynamic initialization is needed only if the property is variadic within the same
				// step
				return IsVariadic(property.Expression, true);
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

