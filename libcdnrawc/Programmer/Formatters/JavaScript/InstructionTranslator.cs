using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.JavaScript
{
	public class InstructionTranslator : CLike.InstructionTranslator
	{
		public InstructionTranslator() : base()
		{
		}
		
		protected override string Translate(InstructionNumber instruction, CLike.Context context)
		{
			return NumberTranslator.Translate(instruction.Value, (Context)context);
		}
		
		protected override string TranslateOperator(InstructionFunction instruction, CLike.Context context)
		{
			switch ((Cdn.MathFunctionType)instruction.Id)
			{
			case MathFunctionType.Modulo:
				return SimpleOperator(context, instruction, " % ");
			default:
				return base.TranslateOperator(instruction, context);
			}
		}

		protected override string Translate(InstructionRand instruction, CLike.Context context)
		{
			var val = base.Translate(instruction, context);

			if (val == null)
			{
				val = "Cdn.Math.rand";
			}

			return val;
		}
	}
}

