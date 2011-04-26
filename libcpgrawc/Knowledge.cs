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
			ScanProperties(d_network);

			// Sort initialize list on dependencies
			d_initialize.Sort(delegate (State a, State b) {
				if (Array.IndexOf(a.Property.Expression.Dependencies, b.Property) != -1)
				{
					return 1;
				}
				else if (Array.IndexOf(b.Property.Expression.Dependencies, a.Property) != -1)
				{
					return -1;
				}
				else
				{
					return 0;
				}
			});
			
			IntegratorState state = d_network.Integrator.State;
			
			foreach (Property prop in state.IntegratedProperties())
			{
				d_integrated.Add(AddState(prop));
			}
			
			foreach (Property prop in state.DirectProperties())
			{
				d_direct.Add(AddState(prop));
			}
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
				}
				
				if (IsVariadic(prop.Expression))
				{
					State state = new State(prop);
					state.Type |= Cpg.RawC.State.Flags.Initialization;
					
					d_initialize.Add(state);
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

		public bool IsVariadic(Cpg.Expression expression)
		{
			// See if the expression is variadic. An expression is variadic if it depends on a variadic operator/function
			// or if it depends on a persistent property
			foreach (Instruction inst in expression.Instructions)
			{
				if (inst is InstructionVariadicFunction)
				{
					return true;
				}
			}
			
			foreach (Property property in expression.Dependencies)
			{
				if (IsVariadic(property))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public bool IsVariadic(Property property)
		{
			return IsPersist(property) || IsVariadic(property.Expression);
		}
		
		public bool IsPersist(Property property)
		{
			// A property is persistent (i.e. needs a persistent storage) if:
			//
			// 1) it is a state (has links that act on it)
			// 2) is either IN or OUT
			// 3) is ONCE and variadic (meaning it needs separate initialization)
			State state = State(property);
			
			if (state != null)
			{
				return true;
			}
			
			if ((property.Flags & (PropertyFlags.In | PropertyFlags.Out)) != PropertyFlags.None)
			{
				return true;
			}
			
			if ((property.Flags & PropertyFlags.Once) != PropertyFlags.None)
			{
				return IsVariadic(property.Expression);
			}
			
			return false;
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
		
		public IEnumerable<State> InitializeStates
		{
			get
			{
				return d_initialize;
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

