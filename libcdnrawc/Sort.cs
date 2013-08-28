using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class Sort
	{
		public static void Insertion<T>(IList<T> list) where T : IComparable<T>
		{
		    Insertion(list, delegate (T a, T b) {
		    	return a.CompareTo(b);
		    });
		}

		public static void Insertion<T>(IList<T> list, Comparison<T> comparer)
		{
		    int count = list.Count;

		    for (int j = 1; j < count; j++)
		    {
		        T key = list[j];

		        int i = j - 1;

		        while (i >= 0 && comparer(list[i], key) > 0)
		        {
		            list[i + 1] = list[i];
		            --i;
		        }

		        list[i + 1] = key;
		    }
		}
	}
}

