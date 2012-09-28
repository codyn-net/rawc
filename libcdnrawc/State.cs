using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class State : Programmer.DataTable.IKey
	{
		[Flags()]
		public enum Flags
		{
			None = 0,
			Integrated = 1 << 0,
			Initialization = 1 << 1,
			Update = 1 << 2
		}

		private object d_object;
		private EdgeAction[] d_actions;
		private Cdn.Expression d_expression;
		private Cdn.Expression d_expressionUnexpanded;
		private Instruction[] d_instructions;
		private Flags d_type;
		
		public State(Flags type)
		{
			d_type = type;
		}

		public State(object obj, params EdgeAction[] actions) : this(obj, (Cdn.Expression)null, Flags.None, actions)
		{
		}

		public State(object obj, Cdn.Instruction[] instructions, Flags type, params EdgeAction[] actions) : this(obj, (Cdn.Expression)null, type, actions)
		{
			d_instructions = instructions;
		}

		public State(object obj, Cdn.Expression expr, Flags type, params EdgeAction[] actions)
		{
			d_object = obj;
			d_actions = actions;

			if (d_actions == null)
			{
				d_actions = new EdgeAction[0];
			}

			Variable variable = obj as Variable;
			
			if (variable != null && variable.Integrated)
			{
				d_type = Flags.Integrated;
			}

			if (expr != null)
			{
				d_expressionUnexpanded = expr;
			}

			d_type |= type;
		}

		public virtual object DataKey
		{
			get { return d_object; }
		}

		private void Expand()
		{
			if (d_expression != null)
			{
				return;
			}

			if (d_expressionUnexpanded != null)
			{
				d_expression = Tree.Expression.Expand(d_expressionUnexpanded);
				return;
			}
			
			List<Cdn.Expression> exprs = new List<Cdn.Expression>();
			Variable v = d_object as Variable;
			
			if (d_actions.Length != 0)
			{
				foreach (EdgeAction action in d_actions)
				{
					exprs.Add(action.Equation);
				}
			}
			else
			{
				if (v != null)
				{
					exprs.Add(v.Expression);
				}
				else
				{
					Expression e = d_object as Expression;

					if (e != null)
					{
						exprs.Add(e);
					}
				}
			}

			Dictionary<Instruction, Instruction> instmap = new Dictionary<Instruction, Instruction>();

			d_expression = RawC.Tree.Expression.Expand(instmap, exprs.ToArray());

			if (d_actions.Length != 0 && v != null)
			{
				List<Cdn.Instruction> instructions = new List<Cdn.Instruction>(d_expression.Instructions);
				
				if ((d_type & Flags.Integrated) != 0)
				{
					// Multiply by timestep
					instructions.Add(new InstructionVariable(Knowledge.Instance.Network.Integrator.Variable("dt"), Cdn.InstructionVariableBinding.None));
					instructions.Add(new InstructionFunction((int)Cdn.MathFunctionType.Multiply, "*", 2));

					// Add to original state variable as well
					instructions.Add(new InstructionVariable(v, Cdn.InstructionVariableBinding.None));
					instructions.Add(new InstructionFunction((int)Cdn.MathFunctionType.Plus, "+", 2));

					d_expression.SetInstructionsTake(instructions.ToArray());
				}
			}

			Knowledge.Instance.UpdateInstructionMap(instmap);
		}
		
		public Cdn.Expression Expression
		{
			get
			{
				Expand();
				return d_expression;
			}
		}

		public EdgeAction[] Actions
		{
			get { return d_actions; }
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

		public object Object
		{
			get { return d_object; }
		}
	}
}