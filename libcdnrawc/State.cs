using System;
using System.Collections.Generic;

namespace Cdn.RawC
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

		public Variable Variable;
		public EdgeAction[] Actions;
		private Cdn.Expression d_expression;
		private Instruction[] d_instructions;
		private Cdn.Expression d_initialValue;
		private Flags d_type;
		
		public State(Flags type)
		{
			d_type = type;
		}
		
		public State(Variable property, params EdgeAction[] actions) : this(property, Flags.None, actions)
		{
		}
		
		public State(Variable property, Flags type, params EdgeAction[] actions)
		{
			Variable = property;
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
		
		public State(Cdn.Expression expression) : this(expression, null, Flags.None)
		{
		}
		
		public State(Cdn.Expression expression, Flags type) : this(expression, null, type)
		{
		}
		
		public State(Cdn.Expression expression, Cdn.Expression initialValue, Flags type)
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
			
			List<Cdn.Expression> exprs = new List<Cdn.Expression>();
			
			if (Actions.Length != 0)
			{
				foreach (EdgeAction action in Actions)
				{
					exprs.Add(action.Equation);
				}
			}
			else
			{
				exprs.Add(Variable.Expression);
			}
			
			d_expression = RawC.Tree.Expression.Expand(exprs.ToArray());
			
			if (Actions.Length != 0)
			{
				List<Cdn.Instruction> instructions = new List<Cdn.Instruction>(d_expression.Instructions);
				
				if ((d_type & Flags.Integrated) != 0)
				{
					// Multiply by timestep
					instructions.Add(new InstructionVariable(Knowledge.Instance.Network.Integrator.Variable("dt"), Cdn.InstructionVariableBinding.None));
					instructions.Add(new InstructionFunction((int)Cdn.MathFunctionType.Multiply, "*", 2));

					// Add to original state variable as well
					instructions.Add(new InstructionVariable(Variable, Cdn.InstructionVariableBinding.None));
					instructions.Add(new InstructionFunction((int)Cdn.MathFunctionType.Plus, "+", 2));
				}

				d_expression.Instructions = instructions.ToArray();
			}
		}
		
		public Cdn.Expression Expression
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
		
		public Cdn.Expression InitialValue
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
					Cdn.Expression expression = Expression;

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