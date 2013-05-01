using System;

namespace Cdn.RawC.Programmer.Formatters.CLike
{
	public class InitialValueTranslator : DynamicVisitor
	{
		public InitialValueTranslator() : base(typeof(string),
		                                          BindingFlags.Default,
		                                          System.Reflection.BindingFlags.Default |
		                                          System.Reflection.BindingFlags.NonPublic |
		                                          System.Reflection.BindingFlags.Instance |
		                                          System.Reflection.BindingFlags.InvokeMethod,
		                                          a => a.Name == "Translate",
		                                          typeof(object))
		{
		}

		public string Translate(params object[] parameters)
		{
			return Invoke<string>(parameters);
		}

		protected virtual string Translate(object obj)
		{
			return "0";
		}

		protected virtual string Translate(Computation.Loop.Index val)
		{
			return val.DataItem.AliasOrIndex;
		}

		protected virtual string Translate(double number)
		{
			var val = number.ToString("0." + new String('0', 15));

			val = val.TrimEnd('0');

			if (val.EndsWith("."))
			{
				val += "0";
			}

			return val;
		}

		protected virtual string Translate(UInt32 number)
		{
			return number.ToString();
		}
	}
}

