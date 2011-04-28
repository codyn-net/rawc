using System;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class State
	{
		public enum Flags
		{
			None,
			Integrated,
			Direct,
			Initialization,
			BeforeDirect,
			BeforeIntegrated,
			AfterIntegrated
		}

		public Property Property;
		public LinkAction[] Actions;
		private Cpg.Expression d_expression;
		private Instruction[] d_instructions;
		private Flags d_type;
		
		public State(Property property, params LinkAction[] actions) : this(property, Flags.None, actions)
		{
		}
		
		public State(Property property, Flags type, params LinkAction[] actions)
		{
			Property = property;
			Actions = actions;
			
			if (property.Integrated)
			{
				d_type = Flags.Integrated;
			}
			else
			{
				d_type = Flags.Direct;
			}
			
			d_type |= type;
		}
		
		private void Expand()
		{
			if (d_expression != null)
			{
				return;
			}
			
			List<Cpg.Expression> exprs = new List<Cpg.Expression>();
			
			if (Actions.Length != 0)
			{
				foreach (LinkAction action in Actions)
				{
					exprs.Add(action.Equation);
				}
			}
			else
			{
				exprs.Add(Property.Expression);
			}
			
			d_expression = RawC.Tree.Expression.Expand(exprs.ToArray());
		}
		
		public Cpg.Expression Expression
		{
			get
			{
				Expand();
				return d_expression;
			}
		}
		
		public Instruction[] Instructions
		{
			get
			{
				if (d_instructions == null)
				{
					d_instructions = Expression.Instructions;
				}
				
				return d_instructions;
			}
		}
		
		public Flags Type
		{
			get
			{
				return d_type;
			}
			set
			{
				d_type = value;
			}
		}
	}
}