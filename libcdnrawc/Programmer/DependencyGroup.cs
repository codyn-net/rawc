using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class DependencyGroup : List<State>
	{
		private Tree.Embedding d_embedding;

		public DependencyGroup(Tree.Embedding embedding)
		{
			d_embedding = embedding;
		}
		
		public Tree.Embedding Embedding
		{
			get { return d_embedding; }
			set { d_embedding = value; }
		}
	}
}

