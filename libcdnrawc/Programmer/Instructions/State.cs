using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public class State : Cdn.Instruction, IInstruction
	{
		private DataTable.DataItem d_item;

		public State(DataTable.DataItem item)
		{
			d_item = item;
		}

		public Cdn.Dimension Dimension
		{
			get { return d_item.Dimension; }
		}

		public DataTable.DataItem Item
		{
			get { return d_item; }
		}

		public static new GLib.GType GType
		{
			get { return Instruction.GType; }
		}
	}
}

