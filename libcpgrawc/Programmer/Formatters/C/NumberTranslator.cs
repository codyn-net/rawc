using System;

namespace Cpg.RawC.Programmer.Formatters.C
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

		public static string Translate(double number, int precision)
		{
			if (precision == 0)
			{
				return Translate(System.Math.Floor(number));
			}
			else
			{
				return number.ToString("0." + new String('0', precision));
			}
		}

		public static string Translate(double number)
		{
			string val = Translate(number, 15);

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

		public static string Translate(Cpg.Property property)
		{
			Instruction[] instructions = property.Expression.Instructions;
			
			if (instructions.Length == 1)
			{
				if (instructions[0] is InstructionConstant)
				{
					return (new InstructionTranslator()).Translate(instructions[0] as InstructionConstant, null);
				}
				else if (instructions[0] is InstructionNumber)
				{
					string val = property.Expression.AsString;
					int pos = val.IndexOf('.');
					
					if (pos == -1)
					{
						return Translate(property.Value);
					}
					else
					{
						return Translate(property.Value, val.Length - pos - 1);
					}
				}
			}

			return Translate(property.Value);
		}
		
		private string DoTranslate(double number)
		{
			return Translate(number);
		}
		
		private string DoTranslate(Cpg.Property property)
		{
			return Translate(property);
		}
	}
}

