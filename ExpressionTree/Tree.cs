using System;
using System.Collections.Generic;
using System.IO;

namespace Cpg.RawC.ExpressionTree
{
	public class Tree : Node
	{
		private States.State d_state;
		private List<Node> d_leafs;
		
		public Tree(States.State state) : this(state, null)
		{
		}
		
		public Tree(States.State state, uint label): base(label)
		{
			d_state = state;
			d_leafs = new List<Node>();
		}
		
		public Tree(uint label) : this(null, label)
		{
		}
		
		public Tree(States.State state, Instruction instruction) : base(instruction)
		{
			d_state = state;
			d_leafs = new List<Node>();
		}
		
		public States.State State
		{
			get
			{
				return d_state;
			}
		}
		
		public List<Node> Leafs
		{
			get
			{
				return d_leafs;
			}
		}

		public static Tree Create(States.State state)
		{
			Stack<Node> stack = new Stack<Node>();
			Tree ret = null;
			List<Node> leafs = new List<Node>();
			
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
					leafs.Add(node);
				}

				stack.Push(node);				
			}
			
			
			leafs.Reverse();
			ret.Leafs.AddRange(leafs);

			return ret;
		}		
	}
}

