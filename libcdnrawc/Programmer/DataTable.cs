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
				Update = 1 << 9,
				Counter = 1 << 10,
				Size = 1 << 11,
				Initialization = 1 << 12,
				Temporary = 1 << 13
			}

			private DataTable d_table;
			private int d_index;
			private object d_key;
			private string d_alias;
			private Flags d_type;
			
			public DataItem(DataTable table, object key, int index) : this(table, key, index, Flags.None)
			{
			}

			public DataItem(DataTable table, object key, int index, Flags type)
			{
				d_table = table;
				d_index = index;
				d_key = key;
				d_type = type;
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
				get
				{
					return d_type;
				}
				set
				{
					d_type = value;
				}
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
				set
				{
					d_index = value;
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
			d_locked = false;
		}
		
		public void Lock()
		{
			d_locked = true;
		}
		
		public bool Locked
		{
			get
			{
				return d_locked;
			}
		}
		
		public ulong MaxSize
		{
			get
			{
				return d_maxSize;
			}
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
			DelayedState dstate;
			Tree.Node node;

			if (As(key, out state) && state.Variable != null)
			{
				return state.Variable;
			}
			else if (As(key, out dstate))
			{
				return new DelayedState.Key(dstate.Operator, dstate.Delay);
			}
			else if (As(key, out node))
			{
				InstructionVariable prop = node.Instruction as InstructionVariable;
				
				if (prop != null)
				{
					return prop.Variable;
				}
				
				Instructions.State st = node.Instruction as Instructions.State;
				
				if (st != null)
				{
					return st.Item;
				}
				
				InstructionCustomOperator op = node.Instruction as InstructionCustomOperator;
				
				if (op != null && op.Operator is OperatorDelayed)
				{
					OperatorDelayed opdel = (OperatorDelayed)op.Operator;
					double delay = 0;

					Knowledge.Instance.Delays.TryGetValue(opdel, out delay);
					return new DelayedState.Key(opdel, delay);
				}
				
				InstructionNumber opnum = node.Instruction as InstructionNumber;
				
				if (opnum != null)
				{
					return opnum.Value;
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
			
			DelayedState dstate;
			
			if (As(key, out dstate))
			{
				/* Also add memory in the table for all the delayed values */
				for (uint i = 1; i < dstate.Count; ++i)
				{
					d_list.Add(new DataItem(this, b, d_list.Count, DataItem.Flags.Delayed));
				}
			}

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
				
				if (!d_items.ContainsKey(basekey) && !d_locked)
				{
					Add(basekey);
					return d_items[basekey];
				}
				else
				{
					return d_items[basekey];
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
			
			for (int i = 0; i < d_list.Count; ++i)
			{
				d_list[i].Index = i;
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

