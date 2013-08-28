using System;

namespace Cdn.RawC.Programmer.Formatters.JavaScript
{
	public class InitialValueTranslator : CLike.InitialValueTranslator
	{
		protected override string Translate(double number)
		{
			return NumberTranslator.Translate(number, null);
		}
	}
}

