using System;

namespace Cdn.RawC.Programmer.Instructions
{
	public class Variable : Instruction
	{
		private string d_name;

		public Variable(string name)
		{
			d_name = name;
		}
		
		public string Name
		{
			get
			{
				return d_name;
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

