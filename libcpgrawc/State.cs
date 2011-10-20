using System;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class State
	{
		[Flags()]
		public enum Flags
		{
			None = 0,
			Integrated = 1 << 0,
			Direct = 1 << 1,
			Initialization = 1 << 2,
			BeforeDirect = 1 << 3,
			BeforeIntegrated = 1 << 4,
			AfterIntegrated = 1 << 5,
			Update = 1 << 6,
			Delayed = 1 << 7
		}

		public Property Property;
		public LinkAction[] Actions;
		private Cpg.Expression d_expression;
		private Instruction[] d_instructions;
		private Cpg.Expression d_initialValue;
		private Flags d_type;
		
		public State(Flags type)
		{
			d_type = type;
		}
		
		public State(Property property, params LinkAction[] actions) : this(property, Flags.None, actions)
		{
		}
		
		public State(Property property, Flags type, params LinkAction[] actions)
		{
			Property = property;
			Actions = actions;
			
			if (property != null && property.Integrated)
			{
				d_type = Flags.Integrated;
			}
			else if (property != null)
			{
				d_type = Flags.Direct;
			}
			
			d_type |= type;
		}
		
		public State(Cpg.Expression expression) : this(expression, null, Flags.None)
		{
		}
		
		public State(Cpg.Expression expression, Flags type) : this(expression, null, type)
		{
		}
		
		public State(Cpg.Expression expression, Cpg.Expression initialValue, Flags type)
		{
			d_expression = expression;
			d_initialValue = initialValue;
			d_type = type;
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
			
			if (Actions.Length != 0)
			{
				List<Cpg.Instruction> instructions = new List<Cpg.Instruction>(d_expression.Instructions);
				
				if ((d_type & (Flags.Integrated | Flags.Direct)) != 0)
				{
					if ((d_type & Flags.Integrated) != 0)
					{
						// Multiply by timestep
						instructions.Add(new InstructionProperty(Knowledge.Instance.Network.Integrator.Property("dt"), Cpg.InstructionPropertyBinding.None));
						instructions.Add(new InstructionOperator((int)Cpg.MathOperatorType.Multiply, "*", 2));

						// Add to original state variable as well
						instructions.Add(new InstructionProperty(Property, Cpg.InstructionPropertyBinding.None));
						instructions.Add(new InstructionOperator((int)Cpg.MathOperatorType.Plus, "+", 2));
					}
				}
				
				d_expression.Instructions = instructions.ToArray();
			}
		}
		
		public Cpg.Expression Expression
		{
			get
			{
				if ((d_type & Flags.Initialization) != 0 && d_initialValue != null)
				{
					return InitialValue;
				}
				else
				{
					Expand();
					return d_expression;
				}
			}
		}
		
		public Cpg.Expression InitialValue
		{
			get
			{
				return d_initialValue != null ? d_initialValue : Expression;
			}
			set
			{
				d_initialValue = value;
			}
		}
		
		public Instruction[] Instructions
		{
			get
			{
				if (d_instructions == null)
				{
					Cpg.Expression expression = Expression;

					if (expression != null)
					{
						d_instructions = expression.Instructions;
					}
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