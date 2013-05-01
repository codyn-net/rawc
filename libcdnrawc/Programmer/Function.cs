using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class Function
	{
		private string d_name;

		private Tree.Node d_expression;
		private List<Tree.Embedding.Argument> d_arguments;
		private List<Tree.Embedding.Argument> d_orderedArguments;
		private Tree.Embedding d_embedding;
		private List<Cdn.FunctionArgument> d_customArguments;
		private bool d_canBeOverridden;

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

		public bool IsCustom
		{
			get { return d_customArguments != null; }
		}

		public bool CanBeOverridden
		{
			get { return d_canBeOverridden; }
		}

		public IEnumerable<Cdn.FunctionArgument> CustomArguments
		{
			get { return d_customArguments; }
		}

		public Function(string name, Tree.Embedding embedding, IEnumerable<Cdn.FunctionArgument> customArguments, bool canBeOverridden) : this(name, embedding.Expression, embedding.Arguments)
		{
			d_embedding = embedding;
			d_canBeOverridden = canBeOverridden;

			if (customArguments != null)
			{
				d_customArguments = new List<Cdn.FunctionArgument>(customArguments);
			}
		}

		public Function(string name, Tree.Embedding embedding) : this(name, embedding, null, false)
		{
			d_embedding = embedding;
		}

		public string Name
		{
			get { return d_name; }
		}

		public Tree.Node Expression
		{
			get { return d_expression; }
		}

		public List<Tree.Embedding.Argument> OrderedArguments
		{
			get { return d_orderedArguments; }
		}

		public IEnumerable<Tree.Embedding.Argument> Arguments
		{
			get { return d_arguments; }
		}

		public int NumArguments
		{
			get { return d_orderedArguments.Count; }
		}

		public bool Inline
		{
			get { return d_embedding != null ? d_embedding.Inline : false; }
		}

		public bool Pure
		{
			get { return d_embedding != null ? d_embedding.Pure : true; }
		}
	}
}

