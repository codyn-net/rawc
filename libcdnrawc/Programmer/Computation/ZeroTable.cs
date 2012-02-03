using System;

namespace Cdn.RawC.Programmer.Computation
{
	public class ZeroTable : INode
	{
		private DataTable d_datatable;

		public ZeroTable(DataTable datatable)
		{
			d_datatable = datatable;
		}
		
		public DataTable DataTable
		{
			get
			{
				return d_datatable;
			}
		}
	}
}

