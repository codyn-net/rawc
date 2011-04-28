using System;
using System.Collections.Generic;
using System.Collections;

namespace Cpg.RawC.Programmer
{
	public class DataTable : IEnumerable<DataTable.DataItem>
	{
		private string d_name;
		private Dictionary<object, DataItem> d_items;
		private List<DataItem> d_list;
		private bool d_needsInitialization;
		
		public class DataItem
		{
			private DataTable d_table;
			private int d_index;
			private object d_key;
			private string d_alias;
			
			public DataItem(DataTable table, object key, int index)
			{
				d_table = table;
				d_index = index;
				d_key = key;
			}
			
			public DataTable Table
			{
				get
				{
					return d_table;
				}
			}
			
			public int Index
			{
				get
				{
					return d_index;
				}
			}
			
			public object Key
			{
				get
				{
					return d_key;
				}
			}
			
			public string Alias
			{
				get
				{
					return d_alias;
				}
				set
				{
					d_alias = value;
				}
			}
			
			public string AliasOrIndex
			{
				get
				{
					return d_alias != null ? d_alias : d_index.ToString();
				}
			}
		}
		
		public DataTable(string name, bool needsInitialization)
		{
			d_name = name;
			d_items = new Dictionary<object, DataItem>();
			d_list = new List<DataItem>();
			d_needsInitialization = needsInitialization;
		}
		
		public bool NeedsInitialization
		{
			get
			{
				return d_needsInitialization;
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return d_list.GetEnumerator();
		}
		
		public IEnumerator<DataItem> GetEnumerator()
		{
			return d_list.GetEnumerator();
		}
		
		public bool Contains(object key)
		{
			return d_items.ContainsKey(BaseKey(key));
		}
		
		private bool As<T>(object obj, out T val)
		{
			if (obj is T)
			{
				val = (T)obj;
				return true;
			}
			else
			{
				val = default(T);
				return false;
			}
		}
		
		private object BaseKey(object key)
		{
			State state;

			if (As(key, out state))
			{
				return state.Property;
			}
			
			return key;
		}
		
		public int Count
		{
			get
			{
				return d_list.Count;
			}
		}
		
		public bool Add(object key)
		{
			object b = BaseKey(key);
			
			if (d_items.ContainsKey(b))
			{
				return false;
			}
			
			DataItem ret = new DataItem(this, b, d_list.Count);
			
			d_items.Add(b, ret);
			d_list.Add(ret);

			return true;
		}
		
		public DataItem this[int idx]
		{
			get
			{
				return d_list[idx];
			}
		}
		
		public DataItem this[object key]
		{
			get
			{
				object basekey = BaseKey(key);
				
				if (!d_items.ContainsKey(basekey))
				{
					Add(basekey);
					return d_items[basekey];
				}
				else
				{
					return d_items[BaseKey(key)];
				}
			}
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
	}
}

