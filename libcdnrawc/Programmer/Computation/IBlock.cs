using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public interface IBlock
	{
		List<INode> Body { get; }
		bool NeedsEvents { get; set; }
	}
}

