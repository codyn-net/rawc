using System;

namespace Cpg.RawC.Programmer.Instructions
{
	public class Function : Instruction
	{
		private Tree.Node d_embedding;
		private Programmer.Function d_function;

		public Function(Tree.Node embedding, Programmer.Function function)
		{
			d_embedding = embedding;
			d_function = function;
		}
		
		public Tree.Node Embedding
		{
			get
			{
				return d_embedding;
			}
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

