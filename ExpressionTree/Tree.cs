using System;
using System.Collections.Generic;
using System.IO;

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

		public Tree() : base()
		{
		}

		private void Build(States.State state, int idx, Stack<Item> stack)
		{
			// Process instruction
			Instruction instruction = state.Instructions[idx];
			InstructionFunction ifunc = instruction as InstructionFunction;
			
			// It's a terminal if it's not a function, or it has 0 arguments
			Node node = new Node(instruction);
			List<Node> np = new List<Node>();
			
			np.Add(this);
			
			Item stacked = stack.Peek();
			
			foreach (Node parent in stacked.Parents)
			{
				int position = stacked.Position;
				
				// Check if this is the root parent, bit special case in terms of position
				if (Object.ReferenceEquals(parent, this))
				{
					// Don't add terminals to the root
					if (node.IsTerminal)
					{
						continue;
					}

					position = 0;
				}
				
				// Find corresponding node in the subnode
				Node other = parent.SubNodes[position].Find(node);
				
				if (other == null)
				{
					other = new Node(node);
					
					// Add the new node to the correct subnode on the parent
					parent.SubNodes[position].Add(other);
				}
				
				other.Use(state);

				if (!node.IsTerminal)
				{
					// Add new parent if it's not a terminal
					np.Add(other);
				}
			}
			
			if (stacked.Position != 0)
			{
				// Decrease argument position
				--stacked.Position;
			}
			else
			{
				// Operator is done, pop the argument
				stack.Pop();
			}

			if (!node.IsTerminal)
			{
				// Push a new position on the stack for the function
				stack.Push(new Item(ifunc.Arguments - 1, np.ToArray()));
			}
		}

		public void Add(States.State state)
		{
			Stack<Item> stack = new Stack<Item>();
			stack.Push(new Item(0, this));

			for (int i = state.Instructions.Length - 1; i >= 0; --i)
			{
				Build(state, i, stack);
			}
		}
		
		public override void Dot(TextWriter writer)
		{
			writer.WriteLine("digraph G {");

			// Generate all the nodes and relationships
			base.Dot(writer);

			writer.WriteLine("}");
		}
	}
}

