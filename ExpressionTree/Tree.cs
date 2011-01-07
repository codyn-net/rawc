using System;
using System.Collections.Generic;
using System.IO;

namespace Cpg.RawC.ExpressionTree
{
	public class Tree : Node
	{
		private States.State d_state;
		private List<Node> d_leaves;
		
		public Tree(States.State state) : this(state, null)
		{
		}
		
		public Tree(States.State state, uint label): base(label)
		{
			d_state = state;
			d_leaves = new List<Node>();
		}
		
		public Tree(uint label) : this(null, label)
		{
		}
		
		public Tree(States.State state, Instruction instruction) : base(instruction)
		{
			d_state = state;
			d_leaves = new List<Node>();
		}
		
		public States.State State
		{
			get
			{
				return d_state;
			}
		}
		
		public List<Node> Leaves
		{
			get
			{
				return d_leaves;
			}
		}

		public static Tree Create(States.State state)
		{
			Stack<Node> stack = new Stack<Node>();
			Tree ret = null;
			List<Node> leaves = new List<Node>();
			
			for (int i = 0; i < state.Instructions.Length; ++i)
			{
				Node node;
				Instruction inst = state.Instructions[i];
				
				if (i == state.Instructions.Length - 1)
				{
					ret = new Tree(state, inst);
					node = ret;
				}
				else
				{				
					node = new Node(inst);
				}

				InstructionFunction ifunc = inst as InstructionFunction;
				
				if (ifunc != null)
				{
					for (int j = 0; j < ifunc.Arguments; ++j)
					{
						node.Add(stack.Pop());
					}				
				}

				if (node.IsLeaf)
				{
					leaves.Add(node);
				}

				stack.Push(node);				
			}
			
			leaves.Reverse();
			ret.Leaves.AddRange(leaves);

			return ret;
		}		
	}
}

