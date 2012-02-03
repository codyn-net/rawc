using System;
using System.Collections.Generic;

namespace Cdn.RawC.Tree.Filters
{
	public interface IFilter
	{
		Tree.Embedding[] Filter(IEnumerable<Tree.Embedding> prototypes);
	}
}

