using System;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class States
	{
		public class State
		{
			public Property Property;
			public LinkAction[] Actions;
			
			public State(Property property, LinkAction[] actions)
			{
				Property = property;
				Actions = actions;
			}
		}

		private IntegratorState d_state;
		private List<State> d_states;
		private List<State> d_integrated;
		private List<State> d_direct;
		private Dictionary<Property, State> d_mapping;

		public States(Cpg.Network network)
		{
			d_state = network.Integrator.State;
			
			d_states = new List<State>();
			d_integrated = new List<State>();
			d_direct = new List<State>();
			d_mapping = new Dictionary<Property, State>();
			
			foreach (Property prop in d_state.IntegratedProperties())
			{
				State s = Add(prop);
				
				d_integrated.Add(s);
			}
			
			foreach (Property prop in d_state.DirectProperties())
			{
				State s = Add(prop);
				
				d_direct.Add(s);
			}
		}
		
		private State Add(Property prop)
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
			
			State s = new State(prop, actions.ToArray());
			
			d_states.Add(s);
			
			d_mapping[prop] = s;
			return s;
		}
		
		public State FromProperty(Property property)
		{
			State ret = null;
			d_mapping.TryGetValue(property, out ret);
			
			return ret;
		}
		
		public State[] Integrated
		{
			get
			{
				return d_integrated.ToArray();
			}
		}
		
		public State[] Direct
		{
			get
			{
				return d_direct.ToArray();
			}
		}
		
		public State[] All
		{
			get
			{
				return d_states.ToArray();
			}
		}
	}
}

