using System;
using System.Collections.Generic;

namespace Cpg.RawC.Tree
{
	public class NodePath : Stack<uint>
	{
		public NodePath()
		{
		}

		public NodePath(NodePath path) : base(path)
		{
		}
		
		public override string ToString()
		{
			return String.Join(":", Array.ConvertAll<uint, string>(ToArray(), a => a.ToString()));
		}
	}
}

