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
			Derivative = 1 << 2,
			Constant = 1 << 3,
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
			Knowledge.Instance.UpdateInstructionMap(instmap);
		}

		private string TypeToString(Flags type)
		{
			if ((type & Flags.Initialization) != 0 && (type & Flags.Integrated) != 0)
			{
				return "i<";
			}
			else if ((type & Flags.Initialization) != 0)
			{
				return "i";
			}
			else if ((type & Flags.Integrated) != 0)
			{
				return "<";
			}
			else if ((type & Flags.Constant) != 0)
			{
				return "c";
			}

			return "";
		}

		public override string ToString()
		{
			var v = d_object as Cdn.Variable;

			if (v != null)
			{
				var flag = v.Flags.ToString();

				if (flag.Length > 2)
				{
					flag = flag.Substring(0, 3);
				}

				return String.Format("{0}{{{1}}}{{{2}}}", v.FullNameForDisplay, flag.ToLower(), TypeToString(d_type));
			}

			var i = d_object as Cdn.Instruction;

			if (i != null)
			{
				return String.Format("{0}{{{1}}}", i.ToString(), TypeToString(d_type));
			}

			return base.ToString();
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