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
		private bool d_needsEventStates;

		public APIFunction(string name, string returnType, params string[] arguments)
		{
			d_source = new List<Computation.INode>();
			d_returnType = returnType;
			d_arguments = arguments;
			d_private = false;
			d_needsEventStates = false;
			d_name = name;
		}
		
		public bool Private
		{
			get { return d_private; }
			set { d_private = value; }
		}

		public bool NeedsEventStates
		{
			get { return d_needsEventStates; }
			set { d_needsEventStates = value; }
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

