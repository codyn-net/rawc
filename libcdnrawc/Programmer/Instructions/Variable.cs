using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public class Variable : Instruction, IInstruction
	{
		private string d_name;

		public Variable(string name)
		{
			d_name = name;
		}

		public Cdn.Dimension Dimension
		{
			get { return new Cdn.Dimension { Rows = 1, Columns = 1}; }
		}

		public string Name
		{
			get { return d_name; }
		}

		public static new GLib.GType GType
		{
			get { return Instruction.GType; }
		}
	}
}

