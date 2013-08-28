using System;

namespace Cdn.RawC.Tree.Collectors
{
	public interface ICollector
	{
		Result Collect(Node[] forest);
	}
}

