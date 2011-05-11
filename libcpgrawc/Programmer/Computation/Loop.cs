using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer.Computation
{
	public class Loop : INode
	{
		public class Item
		{
			public DataTable.DataItem Target;
			public Tree.Node Equation;
			
			public Item(DataTable.DataItem target, Tree.Node equation)
			{
				Target = target;
				Equation = equation;
			}
		}
		
		public class Index
		{
			public ulong Value;
			public DataTable.DataItem DataItem;
			
			public Index(ulong val, DataTable.DataItem item)
			{
				Value = val;
				DataItem = item;
			}
		}

		private Function d_function;
		private List<Item> d_items;
		private DataTable d_indextable;
		private Tree.Node d_expression;
		private Tree.Embedding d_embedding;
		private Dictionary<Tree.NodePath, string> d_mapping;
		private Program d_program;
		private bool d_isintegrated;

		public Loop(Program program, DataTable indextable, Tree.Embedding embedding, Function function)
		{
			d_function = function;
			d_embedding = embedding;
			d_items = new List<Item>();
			d_indextable = indextable;
			d_program = program;
			
			// Create expression
			d_expression = (Tree.Node)d_embedding.Expression.Clone();
			d_expression.Instruction = new Instructions.Function(d_function);
			
			// Generate mapping
			d_mapping = new Dictionary<Tree.NodePath, string>();
		}
		
		public bool IsIntegrated
		{
			get
			{
				return d_isintegrated;
			}
			set
			{
				d_isintegrated = value;
			}
		}
		
		public DataTable IndexTable
		{
			get
			{
				return d_indextable;
			}
			set
			{
				d_indextable = value;
			}
		}
		
		public Dictionary<Tree.NodePath, string> Mapping
		{
			get
			{
				return d_mapping;
			}
		}
		
		public List<Item> Items
		{
			get
			{
				return d_items;
			}
		}
		
		public Function Function
		{
			get
			{
				return d_function;
			}
		}
		
		public Tree.Node Expression
		{
			get
			{
				return d_expression;
			}
		}
		
		public void Add(DataTable.DataItem target, Tree.Node equation)
		{
			d_items.Add(new Item(target, equation));
			
			// Add row to index table
			d_indextable.Add(new Index((ulong)target.Index, target));
			
			foreach (Tree.Embedding.Argument arg in d_function.OrderedArguments)
			{
				Tree.Node subnode = equation.FromPath(arg.Path);
				
				DataTable.DataItem it = d_program.StateTable[subnode];
				
				if (d_isintegrated && d_program.IntegrateTable.ContainsKey(it))
				{
					it = d_program.StateTable[d_program.IntegrateTable[it]];
				}

				d_indextable.Add(new Index((ulong)it.Index, it));
			}
		}
		
		private bool RemoveSameIndices(int c1, int c2)
		{
			List<int> indices = new List<int>();

			for (int i = 0; i < d_items.Count; ++i)
			{
				int idx1 = i * d_indextable.Columns + c1;
				int idx2 = i * d_indextable.Columns + c2;
				
				if (((Index)d_indextable[idx1].Key).Value != ((Index)d_indextable[idx2].Key).Value)
				{
					return false;
				}
				
				indices.Add(idx2);
			}
			
			// Remove column c2
			d_indextable.RemoveAll(indices);
			--d_indextable.Columns;
			
			return true;
		}
		
		public void Close()
		{
			// Eliminate redundant columns
			int column = 0;
			
			Dictionary<int, int> indexmap = new Dictionary<int, int>();
			List<int> origindex = new List<int>(d_indextable.Columns);
			
			for (int i = 0; i < d_indextable.Columns; ++i)
			{
				origindex.Add(i);
			}
			
			while (column < d_indextable.Columns)
			{
				int cmp = column + 1;
				indexmap[origindex[column]] = column;

				while (cmp < d_indextable.Columns)
				{
					if (!RemoveSameIndices(column, cmp))
					{
						++cmp;
					}
					else
					{
						indexmap[origindex[cmp]] = column;
						origindex.RemoveAt(cmp);
					}
				}

				++column;
			}
			
			foreach (Tree.Embedding.Argument arg in d_function.Arguments)
			{
				d_mapping[arg.Path] = String.Format("{0}[{1}[i][{2}]]",
				                                    d_program.StateTable.Name,
				                                    d_indextable.Name,
				                                    indexmap[(int)arg.Index] + 1);
			}

		}
	}
}

