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
			d_size = datatable.Count.ToString();

			d_datatable = datatable;
		}

		public ZeroMemory() : this(null, null)
		{
		}

		public ZeroMemory(string name, int size) : this(name, size.ToString())
		{
		}

		public ZeroMemory(string name, string size)
		{
			d_name = name;
			d_size = size;
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

