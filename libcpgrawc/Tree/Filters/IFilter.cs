using System;
using System.Collections.Generic;

namespace Cpg.RawC.Tree.Filters
{
	public interface IFilter
	{
		Tree.Embedding[] Filter(IEnumerable<Tree.Embedding> prototypes);
	}
}

