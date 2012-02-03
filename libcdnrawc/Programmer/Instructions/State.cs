using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public class State : Cdn.Instruction
	{
		private DataTable.DataItem d_item;

		public State(DataTable.DataItem item)
		{
			d_item = item;
		}
		
		public DataTable.DataItem Item
		{
			get
			{
				return d_item;
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

