using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public struct Sparsity
	{
		public int[] Sparse;

		public Sparsity(int[] sparse)
		{
			Sparse = sparse;
		}
	}

	public class SparseOperator : Instruction, IInstruction
	{
		private Cdn.InstructionFunction d_original;
		private Sparsity d_retsparse;
		private Sparsity[] d_argsparse;

		public SparseOperator(Cdn.InstructionFunction original, Sparsity retsparse, Sparsity[] argsparse)
		{
			d_original = original;
			d_retsparse = retsparse;
			d_argsparse = argsparse;
		}

		public Cdn.InstructionFunction Original
		{
			get { return d_original; }
		}

		public Sparsity[] ArgSparsity
		{
			get { return d_argsparse; }
		}

		public Sparsity RetSparsity
		{
			get { return d_retsparse; }
		}

		public Cdn.Dimension Dimension
		{
			get { return d_original.GetStackManipulation().Push.Dimension; }
		}

		public static new GLib.GType GType
		{
			get { return Instruction.GType; }
		}
	}
}

