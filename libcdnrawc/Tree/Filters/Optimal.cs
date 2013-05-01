using System;
using System.Collections.Generic;

namespace Cdn.RawC.Tree.Filters
{
	[Plugins.Attributes.Plugin(Description="Optimal Filter", Author="Jesse van den Kieboom")]
	public class Optimal : IFilter
	{
		public Optimal()
		{
		}

		public Tree.Embedding[] Filter(IEnumerable<Tree.Embedding> prototypes)
		{
			throw new NotImplementedException("The optimal filter is not yet implemented");
		}
	}
}

