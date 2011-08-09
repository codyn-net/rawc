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
			
			public static implicit operator ulong(Index idx)
			{
				return idx.Value;
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
			d_indextable.MaxSize = (ulong)target.Index;
			
			foreach (Tree.Embedding.Argument arg in d_function.OrderedArguments)
			{
				Tree.Node subnode = equation.FromPath(arg.Path);
				
				DataTable.DataItem it = d_program.StateTable[subnode];
				
				if (d_isintegrated && d_program.IntegrateTable.ContainsKey(it))
				{
					it = d_program.StateTable[d_program.IntegrateTable[it]];
				}
				
				d_indextable.Add(new Index((ulong)it.Index, it)).Type = (it.Type | DataTable.DataItem.Flags.Index);
				d_indextable.MaxSize = (ulong)it.Index;
			}
		}
		
		private bool ColumnIsDuplicate(int c1, out int c2, out int[] indices)
		{
			indices = new int[d_items.Count];
			
			for (c2 = c1 + 1; c2 < d_indextable.Columns; ++c2)
			{
				bool ret = true;

				for (int i = 0; i < d_items.Count; ++i)
				{
					int idx1 = i * d_indextable.Columns + c1;
					int idx2 = i * d_indextable.Columns + c2;
				
					if (((Index)d_indextable[idx1].Key).Value != ((Index)d_indextable[idx2].Key).Value)
					{
						ret = false;
						break;
					}
					
					indices[i] = idx1;
				}
				
				if (ret)
				{
					return true;
				}
			}
			
			return false;
		}
		
		private int FromMap(Dictionary<int, int> mapping, int idx)
		{
			int ret;

			while (true)
			{
				ret = mapping[idx];
				
				if (ret <= idx)
				{
					return ret;
				}
				else
				{
					idx = ret;
				}
			}
		}
		
		public void Close()
		{
			// Eliminate redundant columns
			Dictionary<int, int> indexmap = new Dictionary<int, int>();
			List<int> removed = new List<int>();
			
			for (int i = 0; i < d_indextable.Columns; ++i)
			{
				indexmap[i] = i;
			}
			
			for (int i = 1; i < d_indextable.Columns; ++i)
			{
				int col;
				int[] indices;
				
				if (ColumnIsDuplicate(i, out col, out indices))
				{
					indexmap[i] = col;
					removed.AddRange(indices);
					
					for (int j = i + 1; j < d_indextable.Columns; ++j)
					{
						--indexmap[j];
					}
				}
			}
			
			d_indextable.RemoveAll(removed);
			
			for (int i = 0; i < d_indextable.Count; ++i)
			{
				if (d_indextable[i].HasType(DataTable.DataItem.Flags.Delayed))
				{
					d_indextable.IsConstant = false;
					break;
				}
			}
			
			foreach (Tree.Embedding.Argument arg in d_function.Arguments)
			{
				d_mapping[arg.Path] = String.Format("{0}[{1}[i][{2}]]",
				                                    d_program.StateTable.Name,
				                                    d_indextable.Name,
				                                    FromMap(indexmap, (int)arg.Index + 1));
			}
		}
	}
}

