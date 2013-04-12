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
			if (instruction.Representation.ToLower() == "pi")
			{
				return "Math.PI";
			}
			else if (instruction.Representation.ToLower() == "e")
			{
				return "Math.E";
			}

			return base.Translate(instruction, context);
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

		protected override string FunctionCallName(Programmer.Function function, CLike.Context context)
		{
			return "this." + function.Name;
		}
	}
}

