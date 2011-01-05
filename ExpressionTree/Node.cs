using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cpg.RawC.ExpressionTree
{
	public class Node : IEnumerable<SubNode>
	{
		private byte d_code;
		private List<SubNode> d_subnodes;
		private Instruction d_instruction;
		private bool d_isTerminal;
		private List<States.State> d_states;

		public Node(Node other) : this(other.Instruction)
		{
		}
		
		public Node() : this((Instruction)null)
		{
		}

		public Node(Instruction instruction)
		{
			int size = 1;
			
			d_states = new List<States.State>();
			
			if (instruction != null)
			{
				d_code = RawC.Expression.InstructionCode(instruction);
				d_instruction = instruction;
				
				InstructionFunction ifunc = instruction as InstructionFunction;
				
				if (ifunc != null && ifunc.Arguments > 0)
				{
					size = ifunc.Arguments;
				}
				
				d_isTerminal = (ifunc == null || ifunc.Arguments == 0);
			}

			d_subnodes = new List<SubNode>(size);
			
			for (int i = 0; i < size; ++i)
			{
				d_subnodes.Add(new SubNode());
			}
		}
		
		public List<States.State> States
		{
			get
			{
				return d_states;
			}
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			
			if (d_instruction != null)
			{
				builder.AppendLine(String.Format("{0} [{1}]", d_instruction.ToString(), Count));
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
		
		public void Use(States.State state)
		{
			d_states.Add(state);
		}
		
		public void Unuse(States.State state)
		{
			d_states.Remove(state);
		}
		
		public int Count
		{
			get
			{
				return d_states.Count;
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
		
		public bool IsTerminal
		{
			get
			{
				return d_isTerminal;
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
		
		private string DotInstruction()
		{
			if (d_instruction == null)
			{
				return "#";
			}

			InstructionFunction ifunc = d_instruction as InstructionFunction;
			
			if (ifunc != null)
			{
				return ifunc.Name;
			}
			
			return d_instruction.ToString();
		}
		
		public virtual void Dot(TextWriter writer)
		{
			if (IsTerminal)
			{
				writer.WriteLine("{0} [shape=circle,fontsize=9,label=\"{1}\"];", (uint)GetHashCode(), Count);
			}
			else
			{
				string[] names = Array.ConvertAll<States.State, string>(d_states.ToArray(), a => String.Format("{0}.{1}", a.Property.Object.FullId, a.Property.Name));

				writer.WriteLine("{0} [shape=record,label=\"{1}|{{{2}|{3}}}\"];", (uint)GetHashCode(), DotInstruction(), Count, String.Join(", ", names));
			}
			
			foreach (SubNode subnode in d_subnodes)
			{
				if (!subnode.Empty)
				{
					subnode.Dot(writer);
					writer.WriteLine("{0} -> {1};", (uint)GetHashCode(), (uint)subnode.GetHashCode());
				}
			}
		}
	}
}

