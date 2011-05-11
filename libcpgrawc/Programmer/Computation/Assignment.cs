using System;

namespace Cpg.RawC.Programmer.Computation
{
	public class Assignment : INode
	{
		private State d_state;
		private DataTable.DataItem d_item;
		private Tree.Node d_equation;

		public Assignment(State state, DataTable.DataItem item, Tree.Node equation)
		{
			d_state = state;
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
		
		public State State
		{
			get
			{
				return d_state;
			}
		}
	}
}

