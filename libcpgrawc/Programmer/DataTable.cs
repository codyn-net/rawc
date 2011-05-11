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
		private int d_columns;
		private bool d_isconstant;
		private bool d_integertype;
		
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
		
		public DataTable(string name, bool needsInitialization) : this(name, needsInitialization, -1)
		{
		}
		
		public DataTable(string name, bool needsInitialization, int columns)
		{
			d_name = name;
			d_items = new Dictionary<object, DataItem>();
			d_list = new List<DataItem>();
			d_needsInitialization = needsInitialization;
			d_columns = columns;
			d_isconstant = false;
			d_integertype = false;
		}
		
		public bool IsConstant
		{
			get
			{
				return d_isconstant;
			}
			set
			{
				d_isconstant = value;
			}
		}
		
		public bool IntegerType
		{
			get
			{
				return d_integertype;
			}
			set
			{
				d_integertype = value;
			}
		}
		
		public int Columns
		{
			get
			{
				return d_columns;
			}
			set
			{
				d_columns = value;
			}
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
			Tree.Node node;

			if (As(key, out state) && state.Property != null)
			{
				return state.Property;
			}
			else if (As(key, out node))
			{
				InstructionProperty prop = node.Instruction as InstructionProperty;
				
				if (prop != null)
				{
					return prop.Property;
				}
				
				Instructions.State st = node.Instruction as Instructions.State;
				
				if (st != null)
				{
					return st.Item;
				}
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
		
		public DataItem Add(object key)
		{
			object b = BaseKey(key);
			
			if (d_items.ContainsKey(b))
			{
				return d_items[b];
			}
			
			DataItem ret = new DataItem(this, b, d_list.Count);
			
			d_items.Add(b, ret);
			d_list.Add(ret);

			return ret;
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
		
		public void RemoveAll(IEnumerable<int> indices)
		{
			int num = 0;

			foreach (int idx in indices)
			{
				d_items.Remove(d_list[idx - num].Key);
				d_list.RemoveAt(idx - num);
				++num;
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

