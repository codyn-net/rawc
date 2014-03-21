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
			Constraint = 1 << 4,
			EventAction = 1 << 5,
			EventNode = 1 << 6,
			EventSet = 1 << 7,
			Promoted = 1 << 8
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

			if (variable != null && variable.HasFlag(VariableFlags.Integrated))
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

		public struct VariableDependency
		{
			public Cdn.Variable Variable;
			public Cdn.InstructionVariable Instruction;
		}

		private static Dictionary<Cdn.Expression, List<VariableDependency>> s_variableDependencies;

		private IEnumerable<VariableDependency> VariableExpressionDependencies(Cdn.Expression expr)
		{
			if (expr == null)
			{
				return new VariableDependency[] {};
			}

			if (s_variableDependencies == null)
			{
				s_variableDependencies = new Dictionary<Cdn.Expression, List<VariableDependency>>();
			}

			List<VariableDependency> lst;

			if (s_variableDependencies.TryGetValue(expr, out lst))
			{
				return lst;
			}

			lst = new List<VariableDependency>();
			s_variableDependencies[expr] = lst;

			foreach (var instr in expr.Instructions)
			{
				InstructionVariable v = instr as InstructionVariable;

				if (v != null)
				{
					lst.Add(new VariableDependency { Variable = v.Variable, Instruction = v });
				}
			}

			foreach (var ex in expr.Dependencies)
			{
				lst.AddRange(VariableExpressionDependencies(ex));
			}

			return lst;
		}

		public IEnumerable<VariableDependency> VariableDependencies
		{
			get
			{
				if (d_expression != null)
				{
					foreach (var v in VariableExpressionDependencies(d_expression))
					{
						yield return v;
					}

					yield break;
				}

				foreach (var v in VariableExpressionDependencies(d_expressionUnexpanded))
				{
					yield return v;
				}

				foreach (var action in d_actions)
				{
					Cdn.Variable subvar;

					if (Knowledge.Instance.EventActionProperties.TryGetValue(action, out subvar))
					{
						yield return new VariableDependency { Variable = subvar, Instruction = null };
					}
					else
					{
						foreach (var v in VariableExpressionDependencies(action.Equation))
						{
							yield return v;
						}
					}
				}
			}
		}

		private void Expand()
		{
			if (d_expression != null)
			{
				return;
			}

			if (d_expressionUnexpanded != null)
			{
				d_expression = Knowledge.Instance.ExpandExpression(d_expressionUnexpanded);
				return;
			}

			List<Cdn.Expression> exprs = new List<Cdn.Expression>();
			Variable v = d_object as Variable;

			if (d_actions.Length != 0)
			{
				foreach (EdgeAction action in d_actions)
				{
					Cdn.Variable subvar;

					if (Knowledge.Instance.EventActionProperties.TryGetValue(action, out subvar))
					{
						Cdn.Expression e = new Cdn.Expression(subvar.Name);
						e.Instructions = new Cdn.Instruction[] {new Cdn.InstructionVariable(subvar)};

						exprs.Add(e);
					}
					else
					{
						exprs.Add(action.Equation);
					}
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

			d_expression = Knowledge.Instance.ExpandExpression(instmap, exprs.ToArray());
			Knowledge.Instance.UpdateInstructionMap(instmap);
		}

		public int[] Slice
		{
			get
			{
				if (d_actions != null && d_actions.Length > 0)
				{
					return d_actions[0].Indices;
				}

				return null;
			}
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
				var parts = flag.Split(new char[] {' ', ','}, StringSplitOptions.RemoveEmptyEntries);

				for (int ip = 0; ip < parts.Length; ++ip)
				{
					if (parts[ip].Length > 2)
					{
						parts[ip] = parts[ip].Substring(0, 3);
					}
				}

				return String.Format("{0}{{{1}}}{{{2}}}", v.FullNameForDisplay, String.Join(",", parts).ToLower(), TypeToString(d_type));
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

		public virtual Cdn.Dimension Dimension
		{
			get
			{
				var v = Object as Cdn.Variable;

				if (v != null)
				{
					return v.Dimension;
				}

				var i = Object as Cdn.Instruction;

				if (i != null)
				{
					var ii = Object as Programmer.Instructions.IInstruction;

					if (ii != null)
					{
						return ii.Dimension;
					}

					var smanip = i.GetStackManipulation();
					return smanip.Push.Dimension;
				}

				return new Cdn.Dimension { Rows = 1, Columns = 1 };
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
			set
			{
				d_instructions = value;
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