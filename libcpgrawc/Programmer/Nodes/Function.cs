using System;

namespace Cpg.RawC.Programmer.Nodes
{
	public class Function : Tree.Node
	{
		private Tree.Embedding.Instance d_embedding;
		private Programmer.Function d_function;

		public Function(Tree.Embedding.Instance embedding, Programmer.Function function)
		{
			d_embedding = embedding;
			d_function = function;
		}
		
		public Tree.Embedding.Instance Embedding
		{
			get
			{
				return d_embedding;
			}
		}
		
		public Programmer.Function FunctionCall
		{
			get
			{
				return d_function;
			}
		}
	}
}

