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
				return ((long)number).ToString();
			}
			else
			{
				return number.ToString("0." + new String('0', precision));
			}
		}

		public static string Translate(double number)
		{
			return number.ToString();
		}

		public static string Translate(Cpg.Property property)
		{
			Instruction[] instructions = property.Expression.Instructions;
			
			if (instructions.Length == 1 && instructions[0] is InstructionNumber)
			{
				string val = property.Expression.AsString;
				int pos = val.IndexOf('.');
				
				if (pos == -1)
				{
					return Translate(property.Value, 0);
				}
				else
				{
					return Translate(property.Value, val.Length - pos - 1);
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

