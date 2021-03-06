using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class APIFunction : Computation.IBlock
	{
		private string d_name;
		private List<Computation.INode> d_source;
		private string d_returnType;
		private string[] d_arguments;
		private bool d_private;
		private bool d_needsEvents;

		public APIFunction(string name, string returnType, params string[] arguments)
		{
			d_source = new List<Computation.INode>();
			d_returnType = returnType;
			d_arguments = arguments;
			d_name = name;
		}

		public bool Private
		{
			get { return d_private; }
			set { d_private = value; }
		}

		public bool NeedsEvents
		{
			get
			{
				if (d_needsEvents)
				{
					return d_needsEvents;
				}

				foreach (var node in d_source)
				{
					var block = node as Computation.IBlock;

					if (block != null && block.NeedsEvents)
					{
						return true;
					}
				}

				return false;
			}

			set { d_needsEvents = value; }
		}

		public List<Computation.INode> Body
		{
			get { return d_source; }
		}

		public string[] Arguments
		{
			get { return d_arguments; }
		}

		public string ReturnType
		{
			get { return d_returnType; }
		}

		public string Name
		{
			get { return d_name; }
		}
	}
}

