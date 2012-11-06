using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class Block : INode, IBlock
	{
		private List<INode> d_body;

		public Block()
		{
			d_body = new List<INode>();
		}

		public List<INode> Body
		{
			get { return d_body; }
		}
	}
}

