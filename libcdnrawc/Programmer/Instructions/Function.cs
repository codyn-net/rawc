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

		public Cdn.Dimension[] Pop
		{
			get
			{
				var smanip = d_function.Expression.Instruction.GetStackManipulation();
				var ret = new Cdn.Dimension[smanip.Pop.Num];

				for (int i = 0; i < smanip.Pop.Num; i++)
				{
					ret[i] = smanip.GetPopn(i).Dimension;
				}

				return ret;
			}
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

