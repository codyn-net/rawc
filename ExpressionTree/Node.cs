using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Cpg.RawC.ExpressionTree
{
	public class Node : IEnumerable<SubNode>
	{
		private byte d_code;
		private List<SubNode> d_subnodes;
		private Instruction d_instruction;
		private uint d_count;

		public Node(Instruction instruction)
		{
			int size = 1;
			d_count = 1;
			
			if (instruction != null)
			{
				d_code = RawC.Expression.InstructionCode(instruction);
				d_instruction = instruction;
				
				InstructionFunction ifunc = instruction as InstructionFunction;
				
				if (ifunc != null && ifunc.Arguments > 0)
				{
					size = ifunc.Arguments;
				}
			}

			d_subnodes = new List<SubNode>(size);
			
			for (int i = 0; i < size; ++i)
			{
				d_subnodes.Add(new SubNode());
			}
		}
		
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			
			if (d_instruction != null)
			{
				builder.AppendLine(String.Format("{0} [{1}]", d_instruction.ToString(), d_count));
			}
			
			for (int i = 0; i < d_subnodes.Count; ++i)
			{
				SubNode sub = d_subnodes[i];
				string n = sub.ToString();
				
				if (!String.IsNullOrEmpty(n))
				{
					string[] lst = Array.ConvertAll(n.Split('\n'), a => String.Format("    {0}", a));
					
					lst[0] = String.Format("  {0}) {1}", i + 1, lst[0].Substring(6));
					string s = String.Join("\n", lst);
					
					if (builder.Length != 0)
					{
						builder.AppendLine();
					}

					builder.Append(s);
				}
			}
			
			return builder.ToString();
		}
		
		public void Use()
		{
			++d_count;
		}
		
		public void Unuse()
		{
			--d_count;
		}
		
		public uint Count
		{
			get
			{
				return d_count;
			}
		}
		
		public override bool Equals(object other)
		{
			if (other == null)
			{
				return false;
			}
			
			Node node = other as Node;
			
			if (node == null)
			{
				return false;
			}
			else
			{
				return node.Code == d_code;
			}
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		
		public byte Code
		{
			get
			{
				return d_code;
			}
		}
		
		public List<SubNode> SubNodes
		{
			get
			{
				return d_subnodes;
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return d_subnodes.GetEnumerator();
		}
		
		public IEnumerator<SubNode> GetEnumerator()
		{
			return d_subnodes.GetEnumerator();
		}
		
		public Instruction Instruction
		{
			get
			{
				return d_instruction;
			}
		}
	}
}

