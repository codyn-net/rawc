using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public class SparseOperator : Instruction, IInstruction
	{
		private Cdn.InstructionFunction d_original;
		private SparsityInfo d_retsparse;
		private SparsityInfo[] d_argsparse;

		public SparseOperator(Cdn.InstructionFunction original, SparsityInfo retsparse, SparsityInfo[] argsparse)
		{
			d_original = original;
			d_retsparse = retsparse;
			d_argsparse = argsparse;
		}

		public Cdn.InstructionFunction Original
		{
			get { return d_original; }
		}

		public SparsityInfo[] ArgSparsity
		{
			get { return d_argsparse; }
		}

		public SparsityInfo RetSparsity
		{
			get { return d_retsparse; }
		}

		public Cdn.Dimension Dimension
		{
			get { return d_original.GetStackManipulation().Push.Dimension; }
		}

		public Cdn.Dimension[] Pop
		{
			get
			{
				var smanip = d_original.GetStackManipulation();
				var ret = new Cdn.Dimension[smanip.Pop.Num];

				for (int i = 0; i < smanip.Pop.Num; i++)
				{
					ret[i] = smanip.GetPopn(i).Dimension;
				}

				return ret;
			}
		}

		public override string ToString()
		{
			return string.Format("[SPO: Original={0}, RetSparsity=[{1}]]", Original, String.Join(", ", Array.ConvertAll(RetSparsity.Sparsity, (a) => a.ToString())));
		}

		public static new GLib.GType GType
		{
			get { return Instruction.GType; }
		}
	}
}

