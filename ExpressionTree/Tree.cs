using System;
using System.Collections.Generic;

namespace Cpg.RawC.ExpressionTree
{
	public class Tree : Node
	{
		private class Item
		{
			public int Position;
			public Node[] Parents;
			
			public Item(int position, params Node[] parents)
			{
				Position = position;
				Parents = parents;
			}
		}

		public Tree() : base(null)
		{
			SubNodes.Add(new SubNode());
		}

		private void Build(States.State state, uint idx, Stack<Item> stack)
		{
			if (idx >= state.Expression.Instructions.Length)
			{
				stack.Push(new Item(1, this));
				return;
			}
			
			Build(state, idx + 1, stack);
			
			// Process instruction
			Instruction instruction = state.Expression.Instructions[idx];
			InstructionFunction ifunc = instruction as InstructionFunction;
			
			bool isterm = (ifunc == null || ifunc.Arguments == 0);
			
			Node node = new Node(instruction);
			List<Node> np = new List<Node>();
			
			np.Add(this);
			
			Item stacked = stack.Peek();
			
			foreach (Node parent in stacked.Parents)
			{
				bool added = false;
				int position = stacked.Position;
				
				if (Object.ReferenceEquals(parent, this))
				{
					if (isterm)
					{
						continue;
					}

					position = 0;
				}
				
				foreach (Node child in parent.SubNodes[position])
				{
					if (child.Equals(node))
					{
						if (!isterm)
						{
							np.Add(child);
						}
						
						child.Use();

						added = true;
						break;
					}
				}
				
				if (!added)
				{
					Node cloned = new Node(node.Instruction);
					parent.SubNodes[position].Nodes.Add(cloned);
					
					if (!isterm)
					{
						np.Add(cloned);
					}
				}
			}
			
			if (stacked.Position != 0)
			{
				--stacked.Position;
			}
			else
			{
				stack.Pop();
			}

			if (!isterm)
			{
				stack.Push(new Item(ifunc.Arguments - 1, np.ToArray()));
			}
		}

		public void Add(States.State state)
		{
			Build(state, 0, new Stack<Item>());
		}
	}
}

