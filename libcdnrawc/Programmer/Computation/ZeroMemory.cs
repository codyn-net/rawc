using System;

namespace Cdn.RawC.Programmer.Computation
{
	public class ZeroMemory : INode
	{
		private string d_name;
		private string d_size;
		private DataTable d_datatable;

		public ZeroMemory(DataTable datatable)
		{
			d_name = datatable.Name;
			d_size = datatable.Size.ToString();

			d_datatable = datatable;
		}

		public ZeroMemory()
		{
		}

		public string Name
		{
			get { return d_name; }
		}

		public string Size
		{
			get { return d_size; }
		}

		public DataTable DataTable
		{
			get { return d_datatable; }
		}
	}
}

