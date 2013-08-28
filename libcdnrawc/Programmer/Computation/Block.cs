using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class Block : INode, IBlock
	{
		private List<INode> d_body;
		private bool d_needsEvents;

		public Block()
		{
			d_body = new List<INode>();
		}

		public List<INode> Body
		{
			get { return d_body; }
		}

		public bool NeedsEvents
		{
			get
			{
				if (d_needsEvents)
				{
					return d_needsEvents;
				}

				foreach (var node in d_body)
				{
					var block = node as IBlock;

					if (block != null && block.NeedsEvents)
					{
						return true;
					}
				}

				return false;
			}

			set { d_needsEvents = value; }
		}
	}
}

