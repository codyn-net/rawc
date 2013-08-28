using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public class Variable : Instruction, IInstruction
	{
		private string d_name;
		private bool d_member;

		public Variable(string name, bool member)
		{
			d_name = name;
			d_member = member;
		}

		public Variable(string name) : this(name, false)
		{
		}

		public Cdn.Dimension Dimension
		{
			get { return new Cdn.Dimension { Rows = 1, Columns = 1}; }
		}

		public string Name
		{
			get { return d_name; }
		}

		public bool Member
		{
			get { return d_member; }
		}

		public static new GLib.GType GType
		{
			get { return Instruction.GType; }
		}
	}
}

