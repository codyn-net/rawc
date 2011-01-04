using System;
using System.Collections;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class States : IEnumerable<States.State>
	{
		public class State
		{
			public Property Property;
			public LinkAction[] Actions;
			private Cpg.Expression d_expression;

			public State(Property property, LinkAction[] actions)
			{
				Property = property;
				Actions = actions;
			}
			
			private void Expand()
			{
				if (d_expression != null)
				{
					return;
				}
				
				List<Cpg.Expression> exprs = new List<Cpg.Expression>();
				
				foreach (LinkAction action in Actions)
				{
					exprs.Add(action.Equation);
				}
				
				d_expression = RawC.Expression.Expand(exprs.ToArray());
			}
			
			public Cpg.Expression Expression
			{
				get
				{
					Expand();
					return d_expression;
				}
			}
			
			private void Expand()
			{
				if (d_expression != null)
				{
					return;
				}
				
				List<Cpg.Expression> exprs = new List<Cpg.Expression>();
				
				foreach (LinkAction action in Actions)
				{
					exprs.Add(action.Equation);
				}
				
				d_expression = RawC.Expression.Expand(exprs.ToArray());
			}
			
			public Cpg.Expression Expression
			{
				get
				{
					Expand();
					return d_expression;
				}
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
		}
		
		public void Scan()
		{
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
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return d_states.GetEnumerator();
		}
		
		public IEnumerator<State> GetEnumerator()
		{
			return d_states.GetEnumerator();
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

