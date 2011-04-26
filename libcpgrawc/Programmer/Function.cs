using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer
{
	public class Function
	{
		private string d_name;

		private Tree.Node d_expression;
		private List<Tree.Embedding.Argument> d_arguments;
		private int d_numArguments;

		public Function(string name, Tree.Node expression, IEnumerable<Tree.Embedding.Argument> arguments)
		{
			d_expression = expression;
			d_arguments = new List<Tree.Embedding.Argument>(arguments);
			d_name = name;
			
			d_numArguments = 0;
			
			foreach (Tree.Embedding.Argument arg in d_arguments)
			{
				if (arg.Index >= d_numArguments)
				{
					d_numArguments = ((int)arg.Index) + 1;
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
				return d_numArguments;
			}
		}
	}
}

