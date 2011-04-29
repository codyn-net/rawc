using System;

namespace Cpg.RawC.Programmer.Instructions
{
	public class Function : Instruction
	{
		private Programmer.Function d_function;

		public Function(Programmer.Function function)
		{
			d_function = function;
		}
		
		public Programmer.Function FunctionCall
		{
			get
			{
				return d_function;
			}
		}
		
		public static new GLib.GType GType
		{
			get
			{
				return Instruction.GType;
			}
		}
	}
}

