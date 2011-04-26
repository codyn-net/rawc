using System;

namespace Cpg.RawC.Tree.Instructions
{
	public class Embedding : Instruction
	{
		private Tree.Embedding d_prototype;

		public Embedding(Tree.Embedding prototype)
		{
			d_prototype = prototype;
		}
		
		public Embedding() : this(null)
		{
		}
		
		public Tree.Embedding Prototype
		{
			get
			{
				return d_prototype;
			}
			set
			{
				d_prototype = value;
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

