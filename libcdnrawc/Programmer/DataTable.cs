using System;
using System.Collections.Generic;
using System.Collections;

namespace Cdn.RawC.Programmer
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
		private ulong d_maxSize;
		private bool d_locked;
		private int d_size;

		public interface IKey
		{
			object DataKey { get; }
		}
		
		public class DataItem
		{
			[Flags()]
			public enum Flags
			{
				None = 0,
				State = 1 << 0,
				Index = 1 << 1,
				Constant = 1 << 2,
				Delayed = 1 << 3,
				Integrated = 1 << 4,
				Direct = 1 << 5,
				In = 1 << 6,
				Out = 1 << 7,
				Once = 1 << 8,
				Derivative = 1 << 9,
				Counter = 1 << 10,
				Size = 1 << 11,
				Initialization = 1 << 12,
				Temporary = 1 << 13,
				RandSeed = 1 << 14
			}

			private DataTable d_table;
			private int d_index;
			private int d_dataindex;
			private object d_key;
			private object d_object;
			private string d_alias;
			private Flags d_type;
			private Cdn.Dimension d_dimension;
			private int[] d_slice;
			
			public DataItem(DataTable table, object key, object obj, int index, int dataindex) : this(table, key, obj, index, dataindex, Flags.None)
			{
			}

			public DataItem(DataTable table, object key, object obj, int index, int dataindex, Flags type)
			{
				d_table = table;
				d_index = index;
				d_dataindex = dataindex;
				d_key = key;
				d_object = obj;
				d_type = type;
				
				var st = obj as State;
				
				if (st != null)
				{
					d_dimension = st.Dimension;
				}
				else
				{
					d_dimension = new Cdn.Dimension { Rows = 1, Columns = 1 };
				}
			}
			
			public int[] Slice
			{
				get { return d_slice; }
				set { d_slice = value; }
			}

			public int DataIndex
			{
				get { return d_dataindex; }
				set { d_dataindex = value; }
			}

			public bool HasType(Flags type)
			{
				return (d_type & type) != 0;
			}
			
			public string Description
			{
				get
				{
					if (d_key is Cdn.Variable)
					{
						Cdn.Variable prop = (Cdn.Variable)d_key;
						return prop.FullNameForDisplay;
					}
					else if (d_key is DelayedState.Key)
					{
						DelayedState.Key key = (DelayedState.Key)d_key;
						
						if (HasType(Flags.State))
						{
							return key.Operator.Expression.AsString;
						}
						else
						{
							return "";
						}
					}
					else if (d_key is Computation.Loop.Index)
					{
						Computation.Loop.Index idx = (Computation.Loop.Index)d_key;
						
						return idx.Value.ToString();
					}
					else
					{
						return d_key.ToString();
					}
				}
			}
			
			public Flags Type
			{
				get { return d_type; }
				set { d_type = value; }
			}
			
			public DataTable Table
			{
				get { return d_table; }
			}
			
			public int Index
			{
				get { return d_index; }
				set { d_index = value; }
			}
			
			public Cdn.Dimension Dimension
			{
				get { return d_dimension; }
				set { d_dimension = value; }
			}
			
			public object Key
			{
				get { return d_key; }
			}
			
			public string Alias
			{
				get { return d_alias; }
				set { d_alias = value; }
			}

			public object Object
			{
				get { return d_object; }
			}
			
			public string AliasOrIndex
			{
				get
				{
					return d_alias != null ? d_alias : d_dataindex.ToString();
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
			d_locked = false;
		}
		
		public void Lock()
		{
			d_locked = true;
		}
		
		public bool Locked
		{
			get { return d_locked; }
		}
		
		public ulong MaxSize
		{
			get { return d_maxSize;	}
			set
			{
				if (value > d_maxSize)
				{
					d_maxSize = value;
				}
			}
		}
		
		public bool IsConstant
		{
			get { return d_isconstant; }
			set { d_isconstant = value; }
		}
		
		public bool IntegerType
		{
			get { return d_integertype; }
			set { d_integertype = value; }
		}
		
		public int Columns
		{
			get { return d_columns; }
			set { d_columns = value; }
		}
		
		public bool NeedsInitialization
		{
			get { return d_needsInitialization; }
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
			IKey keyed;

			if (As(key, out keyed))
			{
				object r = keyed.DataKey;

				if (r != null)
				{
					return r;
				}
			}

			return key;
		}
		
		public int Count
		{
			get { return d_list.Count; }
		}

		public int Size
		{
			get { return d_size; }
		}

		public DataItem AddAlias(object key, object other)
		{
			var item = this[other];
			d_items.Add(BaseKey(key), item);
			
			return item;
		}

		public DataItem Add(object key)
		{
			object b = BaseKey(key);
			
			if (d_items.ContainsKey(b))
			{
				return d_items[b];
			}
			
			var st = key as State;
						
			DataItem ret = new DataItem(this, b, key, d_list.Count, d_size);

			if (st != null)
			{
				ret.Slice = st.Slice;
			}

			d_items.Add(b, ret);
			d_list.Add(ret);
			
			d_size += ret.Dimension.Size();

			return ret;
		}
		
		public DataItem this[int idx]
		{
			get
			{
				return d_list[idx];
			}
		}

		public bool TryGetValue(object key, out DataItem item)
		{
			object basekey = BaseKey(key);
			return d_items.TryGetValue(basekey, out item);
		}
		
		public DataItem this[object key]
		{
			get
			{
				DataItem ret;
				object basekey = BaseKey(key);
				
				if (!d_items.TryGetValue(basekey, out ret))
				{
					Console.Error.WriteLine("Failed to find state item: {0}, {1}", key, basekey);
					throw new KeyNotFoundException();
				}
				else
				{
					return ret;
				}
			}
		}
		
		public void RemoveAll(IEnumerable<int> indices)
		{
			int num = 0;
			int cur = d_list.Count;
			
			List<int > ids = new List<int>(indices);
			ids.Sort();

			foreach (int idx in ids)
			{
				d_items.Remove(d_list[idx - num].Key);
				d_list.RemoveAt(idx - num);
				++num;
			}
			
			// How many rows
			if (d_columns > 0)
			{
				d_columns -= num / (cur / d_columns);
			}
			
			int size = 0;
			
			for (int i = 0; i < d_list.Count; ++i)
			{
				var item = d_list[i];
				
				item.Index = i;
				item.DataIndex = size;
				
				size += item.Dimension.Size();
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

