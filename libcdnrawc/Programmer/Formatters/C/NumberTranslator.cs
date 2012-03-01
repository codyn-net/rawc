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
		                                 new Type[] {typeof(object)})
		{
		}

		private static string SpecifierFromContext(Context context)
		{
			if (context != null && context.Options.ValueType == "float")
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
				return number.ToString("0." + new String('0', precision));
			}
		}

		public static string Translate(double number, Context context)
		{
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

			string val = Translate(number, 15, context);

			if (val.IndexOf('.') == -1)
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
				string val = property.Expression.AsString;
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

