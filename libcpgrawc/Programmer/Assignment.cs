using System;

namespace Cpg.RawC.Programmer
{
	public class Assignment : IComputationNode
	{
		private DataTable.DataItem d_item;
		private Tree.Node d_equation;

		public Assignment(DataTable.DataItem item, Tree.Node equation)
		{
			d_item = item;
			d_equation = equation;
		}
		
		public DataTable.DataItem Item
		{
			get
			{
				return d_item;
			}
		}
		
		public Tree.Node Equation
		{
			get
			{
				return d_equation;
			}
		}
	}
}

