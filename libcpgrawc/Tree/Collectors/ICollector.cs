using System;

namespace Cpg.RawC.Tree.Collectors
{
	public interface ICollector
	{
		Result Collect(Node[] forest);
	}
}

