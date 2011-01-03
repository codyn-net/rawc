using System;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class Loop
	{
		public class Item
		{
			public States.State State;
			public Expression Expression;
			
			public Item(States.State state, Expression expression)
			{
				State = state;
				Expression = expression;
			}
		}

		private List<Item> d_items;
		private Dictionary<Instruction, int> d_instructionIndexMap;
		
		public Loop(States.State state, Expression expression)
		{
			d_items = new List<Item>();
			d_items.Add(new Item(state, expression));
			
			d_instructionIndexMap = new Dictionary<Instruction, int>();
		}
		
		public Expression Prototype
		{
			get
			{
				return d_items[0].Expression;
			}
		}
		
		public bool Add(States.State state, Expression expr)
		{
			if (Prototype.HashEqual(expr))
			{
				d_items.Add(new Item(state, expr));
				return true;
			}
			else
			{
				return false;
			}
		}
		
		public List<Item> Items
		{
			get
			{
				return d_items;
			}
		}
		
		public List<Expression> Expressions
		{
			get
			{
				List<Expression> ret = new List<Expression>();
				
				foreach (Item item in d_items)
				{
					if (!ret.Contains(item.Expression))
					{
						ret.Add(item.Expression);
					}
				}

				return ret;
			}
		}
		
		public List<States.State> States
		{
			get
			{
				List<States.State> ret = new List<States.State>();
				
				foreach (Item item in d_items)
				{
					if (!ret.Contains(item.State))
					{
						ret.Add(item.State);
					}
				}

				return ret;
			}
		}
		
		public int Count
		{
			get
			{
				return d_items.Count;
			}
		}
		
		public bool ConflictsWith(Loop other)
		{
			List<Expression> expressions = Expressions;
			List<Expression> otherexpr = other.Expressions;

			foreach (Expression expr in expressions)
			{
				if (otherexpr.Contains(expr))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public void Close()
		{
			// Compute the indices of things that need to be derived from the loop index
			// All placeholders in the hash, except for those that are actually equal everywhere...
			Expression proto = Prototype;
			Instruction[] protoinst = proto.WrappedObject.Instructions;

			for (int i = 1; i < d_items.Count; ++i)
			{
				Expression other = d_items[i].Expression;
				Instruction[] otheri = other.WrappedObject.Instructions;
				
				for (int j = 0; j < proto.Hash.Length; ++j)
				{
					if (proto.Hash[j] != Expression.PlaceholderCode || protoinst[j] == null)
					{
						continue;
					}
					
					if (protoinst[j].GetType() != otheri[j].GetType())
					{
						protoinst[j] = null;
					}
					
					if (protoinst[j] is InstructionNumber)
					{
						if (((InstructionNumber)protoinst[j]).Value != ((InstructionNumber)otheri[j]).Value)
						{
							protoinst[j] = null;
						}
					}
					else
					{
						if (((InstructionProperty)protoinst[j]).Property != ((InstructionProperty)otheri[j]).Property)
						{
							protoinst[j] = null;
						}
					}
				}
			}
			
			Instruction[] instr = proto.WrappedObject.Instructions;
			
			for (int i = 0; i < proto.Hash.Length; ++i)
			{
				if (proto.Hash[i] == Expression.PlaceholderCode && protoinst[i] == null)
				{
					d_instructionIndexMap[instr[i]] = i;
				}
			}
		}
		
		public int Cost()
		{
			int ret = 0;

			// Some kind of heuristic to determine the cost of a loop...
			
			// Number of comparisons and increments
			ret += States.Count * 2;
			
			// Size of the body of the loop
			ret += Prototype.Hash.Length;
			
			// Index table size
			ret += States.Count * d_instructionIndexMap.Count;

			return ret;
		}
	}
}

