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
		
		private string Translate(Tree.Node node)
		{
			return NotInitialized;
		}
		
		private string Translate(State state)
		{
			return NotInitialized;
		}
		
		private string Translate(Cdn.Variable property)
		{
			if (Knowledge.Instance.NeedsInitialization(property, true))
			{
				return NotInitialized;
			}
			else
			{
				return NumberTranslator.Translate(property);
			}
		}
		
		private string Translate(DelayedState.Key op)
		{
			if (Knowledge.Instance.NeedsInitialization(op.Operator.InitialValue, true))
			{
				return NotInitialized;
			}
			else if (op.Operator.InitialValue != null)
			{
				return NumberTranslator.Translate(op.Operator.InitialValue.Value);
			}
			else
			{
				return "0.0";
			}
		}
		
		private string Translate(Computation.Loop.Index val)
		{
			return val.DataItem.AliasOrIndex;
		}
		
		private string Translate(double number)
		{
			return NumberTranslator.Translate(number);
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

