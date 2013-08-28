using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public class Function : Instruction, IInstruction
	{
		private Programmer.Function d_function;

		public Function(Programmer.Function function)
		{
			d_function = function;
		}

		public Cdn.Dimension Dimension
		{
			get { return d_function.Expression.Dimension; }
		}

		public Programmer.Function FunctionCall
		{
			get { return d_function; }
		}

		public static new GLib.GType GType
		{
			get { return Instruction.GType; }
		}
	}
}

