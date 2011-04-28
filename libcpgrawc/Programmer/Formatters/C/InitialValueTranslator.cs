using System;

namespace Cpg.RawC.Programmer.Formatters.C
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
		
		private string Translate(Cpg.Property property)
		{
			if (Knowledge.Instance.NeedsInitialization(property, RawC.Options.Instance.AlwaysInitializeDynamically))
			{
				return NotInitialized;
			}
			else
			{
				return NumberTranslator.Translate(property);
			}
		}
		
		private string Translate(double number)
		{
			return NumberTranslator.Translate(number);
		}
		
		public const string NotInitialized = "NINIT";
	}
}

