using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class InstructionTranslator : CLike.InstructionTranslator
	{
		protected override string Translate(InstructionRand instruction, CLike.Context context)
		{
			string val = base.Translate(instruction, context);

			if (val == null)
			{
				val = "CDN_MATH_RAND()";
			}

			return val;
		}

		protected override string Translate(InstructionNumber instruction, CLike.Context context)
		{
			return NumberTranslator.Translate(instruction.Value, (Context)context);
		}
	}
}

