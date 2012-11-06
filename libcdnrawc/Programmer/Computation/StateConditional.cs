using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class StateConditional : INode, IBlock
	{
		private List<INode> d_body;
		private Knowledge.EventStateGroup d_eventStateGroup;

		public StateConditional(Knowledge.EventStateGroup grp)
		{
			d_eventStateGroup = grp;
			d_body = new List<INode>();
		}
		
		public Knowledge.EventStateGroup EventStateGroup
		{
			get { return d_eventStateGroup; }
		}
		
		public List<INode> Body
		{
			get { return d_body; }
		}
	}
}

