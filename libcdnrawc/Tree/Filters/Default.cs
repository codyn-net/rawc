using System;
using System.Collections.Generic;

namespace Cdn.RawC.Tree.Filters
{
	[Plugins.Attributes.Plugin(Description="Greedy Filter", Author="Jesse van den Kieboom")]
	public class Default : IFilter
	{
		public Default()
		{
		}

		public Tree.Embedding[] Filter(IEnumerable<Tree.Embedding> prototypes)
		{
			// Filter out any prototypes that have conflicting instances.
			// Sort prototypes based on a score heuristic
			List<Tree.Embedding> list = new List<Tree.Embedding>(prototypes);
			list.Sort(CompareEmbeddings);

			List<Tree.Embedding> ret = new List<Tree.Embedding>();

			foreach (Tree.Embedding embedding in list)
			{
				bool isconflict = false;

				foreach (Tree.Embedding comp in ret)
				{
					if (comp.Conflicts(embedding))
					{
						isconflict = true;
						break;
					}
				}

				if (!isconflict)
				{
					ret.Add(embedding);
				}
			}

			return ret.ToArray();
		}

		private int Score(Tree.Embedding embedding)
		{
			return -1 * embedding.InstancesCount * embedding.Expression.Descendants.Length;
		}

		private int CompareEmbeddings(Tree.Embedding a, Tree.Embedding b)
		{
			return Score(a).CompareTo(Score(b));
		}
	}
}

