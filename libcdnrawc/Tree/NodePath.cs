using System;
using System.Collections.Generic;

namespace Cdn.RawC.Tree
{
	public class NodePath : Stack<uint>
	{
		public NodePath()
		{
		}

		public NodePath(NodePath path) : base(path)
		{
		}

		public string Id
		{
			get
			{
				return String.Join(":", Array.ConvertAll<uint, string>(ToArray(), a => a.ToString()));
			}
		}

		public override string ToString()
		{
			return Id;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			NodePath other = obj as NodePath;

			if (other == null)
			{
				return false;
			}

			return Id.Equals(other.Id);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}

