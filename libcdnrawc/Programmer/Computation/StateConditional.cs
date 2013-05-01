using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class StateConditional : INode, IBlock
	{
		private List<INode> d_body;
		private Knowledge.EventStateGroup d_eventStateGroup;
		private List<INode> d_else;

		public StateConditional(Knowledge.EventStateGroup grp)
		{
			d_eventStateGroup = grp;
			d_body = new List<INode>();
			d_else = new List<INode>();
		}

		public Knowledge.EventStateGroup EventStateGroup
		{
			get { return d_eventStateGroup; }
		}

		public List<INode> Body
		{
			get { return d_body; }
		}

		public List<INode> Else
		{
			get { return d_else; }
		}
	}
}

