using System;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class InitialValueTranslator : DynamicVisitor
	{
		public InitialValueTranslator() : base(typeof(string),
		                                          BindingFlags.Default,
		                                          System.Reflection.BindingFlags.Default |
		                                          System.Reflection.BindingFlags.NonPublic |
		                                          System.Reflection.BindingFlags.Instance |
		                                          System.Reflection.BindingFlags.InvokeMethod,
		                                          new Type[] {typeof(object)})
		{
		}
		
		public string Translate(params object[] parameters)
		{
			return Invoke<string>(parameters);
		}

		private string Translate(object obj)
		{
			return NotInitialized;
		}

		private string Translate(Computation.Loop.Index val)
		{
			return val.DataItem.AliasOrIndex;
		}
		
		private string Translate(double number)
		{
			return NumberTranslator.Translate(number, null);
		}
		
		private string Translate(DelayedState.Size size)
		{
			return "0";
		}
		
		private string Translate(UInt32 number)
		{
			return number.ToString();
		}
		
		public const string NotInitialized = "NINIT";
	}
}

