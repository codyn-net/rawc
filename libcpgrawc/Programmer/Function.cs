using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer
{
	public class Function
	{
		private string d_name;

		private Tree.Node d_expression;
		private List<Tree.Embedding.Argument> d_arguments;
		private List<Tree.Embedding.Argument> d_orderedArguments;

		public Function(string name, Tree.Node expression, IEnumerable<Tree.Embedding.Argument> arguments)
		{
			d_expression = expression;
			d_arguments = new List<Tree.Embedding.Argument>(arguments);
			d_name = name;
			
			d_orderedArguments = new List<Tree.Embedding.Argument>();
			
			foreach (Tree.Embedding.Argument arg in d_arguments)
			{
				while (arg.Index >= d_orderedArguments.Count)
				{
					d_orderedArguments.Add(null);
				}
				
				if (d_orderedArguments[(int)arg.Index] == null)
				{
					d_orderedArguments[(int)arg.Index] = arg;
				}
			}
		}
		
		public Function(string name, Tree.Embedding embedding) : this(name, embedding.Expression, embedding.Arguments)
		{
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
		}
		
		public Tree.Node Expression
		{
			get
			{
				return d_expression;
			}
		}
		
		public IEnumerable<Tree.Embedding.Argument> OrderedArguments
		{
			get
			{
				return d_orderedArguments;
			}
		}
		
		public IEnumerable<Tree.Embedding.Argument> Arguments
		{
			get
			{
				return d_arguments;
			}
		}
		
		public int NumArguments
		{
			get
			{
				return d_orderedArguments.Count;
			}
		}
	}
}

