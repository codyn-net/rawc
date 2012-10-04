using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class APIFunction
	{
		private string d_name;
		private List<Computation.INode> d_source;
		private string d_returnType;
		private string[] d_arguments;
		private bool d_private;

		public APIFunction(string name, string returnType, params string[] arguments)
		{
			d_source = new List<Computation.INode>();
			d_returnType = returnType;
			d_arguments = arguments;
			d_private = false;
			d_name = name;
		}
		
		public bool Private
		{
			get { return d_private; }
			set { d_private = value; }
		}

		public void Add(Computation.INode node)
		{
			d_source.Add(node);
		}

		public void AddRange(IEnumerable<Computation.INode> nodes)
		{
			d_source.AddRange(nodes);
		}

		public string[] Arguments
		{
			get { return d_arguments; }
		}

		public string ReturnType
		{
			get { return d_returnType; }
		}

		public IEnumerable<Computation.INode> Source
		{
			get { return d_source; }
		}

		public int SourceCount
		{
			get { return d_source.Count; }
		}

		public string Name
		{
			get { return d_name; }
		}	

		public bool Contains(Computation.INode node)
		{
			return d_source.Contains(node);
		}
	}
}

