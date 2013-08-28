using System;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class InitialValueTranslator : CLike.InitialValueTranslator
	{
		protected override string Translate(double number)
		{
			return NumberTranslator.Translate(number, null);
		}
	}
}

