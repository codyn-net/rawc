using System;

namespace Cdn.RawC.Programmer.Formatters.JavaScript
{
	public class NumberTranslator : DynamicVisitor
	{
		public NumberTranslator() : base(typeof(string),
		                                 BindingFlags.Default,
		                                 System.Reflection.BindingFlags.Default |
		                                 System.Reflection.BindingFlags.NonPublic |
		                                 System.Reflection.BindingFlags.Instance |
		                                 System.Reflection.BindingFlags.InvokeMethod,
		                                 a => a.Name == "Translate",
		                                 typeof(object))
		{
		}

		public static string Translate(double number, int precision, Context context)
		{
			if (Double.IsNaN(number))
			{
				return "Number.NaN";
			}
			else if (Double.IsInfinity(number))
			{
				return "Number.POSITIVE_INFINITY";
			}
			else if (precision == 0)
			{
				return Translate(System.Math.Floor(number), context);
			}
			else
			{
				return number.ToString("0." + new String('0', precision));
			}
		}

		public static string Translate(double number, Context context)
		{
			if (Double.IsNaN(number))
			{
				return "Number.NaN";
			}
			else if (Double.IsPositiveInfinity(number))
			{
				return "Number.POSITIVE_INFINITY";
			}
			else if (Double.IsNegativeInfinity(number))
			{
				return "Number.NEGATIVE_INFINITY";
			}
			else if (number == Double.MaxValue)
			{
				return "Number.MAX_VALUE";
			}
			else if (number == Double.MinValue)
			{
				return "-Number.MAX_VALUE";
			}
			else if (number == Double.Epsilon)
			{
				return "Number.MIN_VALUE";
			}
			else if (number == -Double.Epsilon)
			{
				return "-Number.MIN_VALUE";
			}
			else if (number == System.Math.PI)
			{
				return "Math.PI";
			}
			else if (number == -System.Math.PI)
			{
				return "-Math.PI";
			}
			else if (number == System.Math.E)
			{
				return "Math.E";
			}
			else if (number == -System.Math.E)
			{
				return "-Math.E";
			}

			string val = Translate(number, 15, context);

			if (val.IndexOf('.') == -1)
			{
				return val + ".0";
			}
			else
			{
				val = val.TrimEnd('0');

				if (val.EndsWith("."))
				{
					val += "0";
				}

				return val;
			}
		}

		public static string Translate(Cdn.Variable property, Context context)
		{
			Instruction[] instructions = property.Expression.Instructions;
			
			if (instructions.Length == 1 && instructions[0] is InstructionNumber)
			{
				string val = ((InstructionNumber)instructions[0]).Representation;

				if (val.ToLower() == "pi")
				{
					return "Math.PI";
				}
				else if (val.ToLower() == "e")
				{
					return "Math.E";
				}

				int pos = val.IndexOf('.');
				
				if (pos == -1)
				{
					return Translate(property.Value, context);
				}
				else
				{
					return Translate(property.Value, val.Length - pos - 1, context);
				}
			}

			return Translate(property.Value, context);
		}
	}
}

