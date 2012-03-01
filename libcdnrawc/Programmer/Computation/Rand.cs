using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class Rand : INode
	{
		private List<State> d_states;

		public Rand(IEnumerable<State> states)
		{
			d_states = new List<State>(states);
		}

		public bool Empty
		{
			get { return d_states.Count == 0; }
		}

		public IEnumerable<KeyValuePair<int, int>> Ranges(DataTable table)
		{
			if (Empty)
			{
				yield break;
			}

			int[] indices = Array.ConvertAll<State, int>(d_states.ToArray(), (a) => table[a].Index);

			Array.Sort(indices);

			int start = indices[0];
			int end = indices[0];

			for (int i = 1; i < indices.Length; ++i)
			{
				if (indices[i] != end + 1)
				{
					yield return new KeyValuePair<int, int>(start, end);

					start = indices[i];
					end = indices[i];
				}
				else
				{
					++end;
				}
			}

			yield return new KeyValuePair<int, int>(start, end);
		}
	}
}

