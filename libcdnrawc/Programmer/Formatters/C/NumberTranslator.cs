using System;

namespace Cdn.RawC.Programmer.Formatters.C
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

		private static string SpecifierFromContext(Context context)
		{
			if (context != null && ((Options)context.Options).ValueType == "float")
			{
				return "f";
			}
			else
			{
				return "";
			}
		}

		public static string Translate(double number, int precision, Context context)
		{
			if (Double.IsNaN(number))
			{
				return "NAN";
			}
			else if (Double.IsInfinity(number))
			{
				return "INFINITY";
			}
			else if (precision == 0)
			{
				return Translate(System.Math.Floor(number), context);
			}
			else
			{
				return number.ToString("R");
			}
		}

		public static string Translate(double number, Context context)
		{
			string vt;

			if (context != null)
			{
				vt = ((Options)context.Options).ValueType;
			}
			else
			{
				vt = "double";
			}

			if (Double.IsNaN(number))
			{
				return "NAN";
			}
			else if (Double.IsPositiveInfinity(number))
			{
				return "INFINITY";
			}
			else if (Double.IsNegativeInfinity(number))
			{
				return "-INFINITY";
			}
			else if (number == Double.MaxValue)
			{
				if (vt == "float")
				{
					return "FLT_MAX";
				}
				else
				{
					return "DBL_MAX";
				}
			}
			else if (number == Double.MinValue)
			{
				if (vt == "float")
				{
					return "-FLT_MAX";
				}
				else
				{
					return "-DBL_MAX";
				}
			}
			else if (number == Double.Epsilon)
			{
				if (vt == "float")
				{
					return "FLT_MIN";
				}
				else
				{
					return "DBL_MIN";
				}
			}
			else if (number == -Double.Epsilon)
			{
				if (vt == "float")
				{
					return "-FLT_MIN";
				}
				else
				{
					return "-DBL_MIN";
				}
			}
			else if (number == System.Math.PI)
			{
				return "M_PI";
			}
			else if (number == -System.Math.PI)
			{
				return "-M_PI";
			}

			string val = Translate(number, 20, context);

			if (val.IndexOf('.') == -1 && val.IndexOf('E') == -1 && val.IndexOf('e') == -1)
			{
				return val + ".0" + SpecifierFromContext(context);
			}
			else
			{
				val = val.TrimEnd('0');

				if (val.EndsWith("."))
				{
					val += "0";
				}

				return val + SpecifierFromContext(context);
			}
		}

		public static string Translate(Cdn.Variable property, Context context)
		{
			Instruction[] instructions = property.Expression.Instructions;

			if (instructions.Length == 1 && instructions[0] is InstructionNumber)
			{
				string val = ((InstructionNumber)instructions[0]).Representation;

				if (val == "pi")
				{
					return "M_PI";
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

		private string DoTranslate(double number, Context context)
		{
			return Translate(number, context);
		}

		private string DoTranslate(Cdn.Variable property, Context context)
		{
			return Translate(property, context);
		}
	}
}
