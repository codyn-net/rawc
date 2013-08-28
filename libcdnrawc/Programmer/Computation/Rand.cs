using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class Rand : INode
	{
		public struct IndexRange
		{
			public int Start;
			public int End;
			public int ZeroOffset;
		}

		private List<State> d_states;

		public Rand(IEnumerable<State> states)
		{
			d_states = new List<State>(states);
		}

		public bool Empty
		{
			get { return d_states.Count == 0; }
		}

		public IEnumerable<IndexRange> Ranges(DataTable table)
		{
			if (Empty)
			{
				yield break;
			}

			int[] indices = Array.ConvertAll<State, int>(d_states.ToArray(), (a) => table[a].DataIndex);

			Array.Sort(indices);

			int start = indices[0];
			int end = indices[0];
			int offset = start;

			for (int i = 1; i < indices.Length; ++i)
			{
				if (indices[i] != end + 1)
				{
					yield return new IndexRange {Start = start, End = end, ZeroOffset = offset};

					offset += end - start;

					start = indices[i];
					end = indices[i];
				}
				else
				{
					++end;
				}
			}

			yield return new IndexRange { Start = start, End = end, ZeroOffset = offset};
		}
	}
}

