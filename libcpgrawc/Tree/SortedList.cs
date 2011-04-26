using System;
using System.Collections.Generic;

namespace Cpg.RawC.Tree
{
	public class SortedList<T> : List<T> where T : IComparable<T>
	{
		public SortedList()
		{
		}
		
		public T Find(T item)
		{
			int i = BinarySearch(item);
			
			if (i >= 0)
			{
				return this[i];
			}
			else
			{
				return default(T);
			}
		}
		
		public new void Add(T item)
		{
			int i = BinarySearch(item);
			
			if (i >= 0)
			{
				base.Insert(i, item);
			}
			else
			{
				base.Insert(~i, item);
			}
		}
		
		public new void Remove(T item)
		{
			int i = BinarySearch(item);
			
			if (i >= 0)
			{
				RemoveAt(i);
			}
		}
	}
}

