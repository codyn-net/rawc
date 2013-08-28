using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class DependencyGroup : List<State>
	{
		private Tree.Embedding d_embedding;
		private Knowledge.EventStateGroup d_eventStateGroup;

		public DependencyGroup(Tree.Embedding embedding, Knowledge.EventStateGroup eventStateGroup)
		{
			d_embedding = embedding;
			d_eventStateGroup = eventStateGroup;
		}

		public Tree.Embedding Embedding
		{
			get { return d_embedding; }
			set { d_embedding = value; }
		}

		public Knowledge.EventStateGroup EventStateGroup
		{
			get { return d_eventStateGroup; }
			set { d_eventStateGroup = value; }
		}
	}
}

