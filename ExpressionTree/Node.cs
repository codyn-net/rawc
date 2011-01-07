using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cpg.RawC.ExpressionTree
{
	public class Node : IEnumerable<Node>, IComparable<Node>
	{
		private uint d_label;
		private Instruction d_instruction;

		private List<Node> d_children;
		private Node d_parent;

		private bool d_isLeaf;
		private uint d_height;
		
		private uint d_degree;
		private uint d_childCount;
		private uint d_descendants;

		public int CompareTo(Node other)
		{
			if (other == null)
			{
				return 1;
			}
			
			return d_label.CompareTo(other.Label);
		}
		
		public Node(uint label)
		{
			d_label = label;
			d_isLeaf = false;
			d_children = new List<Node>();
		}

		public Node(Instruction instruction)
		{
			int size = 0;

			d_label = RawC.Expression.InstructionCode(instruction);
			d_instruction = instruction;
				
			InstructionFunction ifunc = instruction as InstructionFunction;
				
			if (ifunc != null && ifunc.Arguments > 0)
			{
				size = ifunc.Arguments;
			}
			
			InstructionCustomFunction icfunc = instruction as InstructionCustomFunction;
			
			if (icfunc != null && icfunc.Arguments > 0)
			{
				size = icfunc.Arguments;
			}
				
			d_isLeaf = size == 0;
			d_children = new List<Node>(size);
		}
		
		public bool IsLeaf
		{
			get
			{
				return d_isLeaf;
			}
			set
			{
				d_isLeaf = value;
			}
		}
		
		public uint Degree
		{
			get
			{
				return d_degree;
			}
		}
		
		public uint Descendants
		{
			get
			{
				return d_descendants;
			}
		}
		
		public uint ChildCount
		{
			get
			{
				return d_childCount;
			}
			set
			{
				d_childCount = value;
			}
		}
		
		public uint Label
		{
			get
			{
				return d_label;
			}
		}
		
		public List<Node> Children
		{
			get
			{
				return d_children;
			}
		}

		public Instruction Instruction
		{
			get
			{
				return d_instruction;
			}
		}
		
		private void PropagateHeight()
		{
			if (d_parent == null)
			{
				return;
			}

			if (Height + 1 > d_parent.Height)
			{
				d_parent.Height = Height + 1;
			}
		}

		public uint Height
		{
			get
			{
				return d_height;
			}
			set
			{
				d_height = value;
				
				PropagateHeight();
			}
		}

		public Node Parent
		{
			get
			{
				return d_parent;
			}
			private set
			{
				d_parent = value;
				
				PropagateHeight();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return d_children.GetEnumerator();
		}
		
		public IEnumerator<Node> GetEnumerator()
		{
			return d_children.GetEnumerator();
		}
		
		public void Add(Node child)
		{
			Add(child, true);
		}

		public void Add(Node child, bool setParent)
		{
			if (setParent)
			{
				child.Parent = this;
			}

			d_children.Add(child);
			
			++d_degree;
			++d_childCount;
			
			d_descendants += child.Descendants + 1;
		}
		
		public Tree Top
		{
			get
			{
				if (d_parent != null)
				{
					return d_parent.Top;
				}
				else
				{
					return this as Tree;
				}
			}
		}
		
		public override string ToString ()
		{
			string lbl = "?";

			InstructionFunction ifunc;
			InstructionCustomFunction icfunc;
			
			ifunc = d_instruction as InstructionFunction;
			icfunc = d_instruction as InstructionCustomFunction;
			
			if (ifunc != null)
			{
				lbl = ifunc.Name;
			}
			else if (icfunc != null)
			{
				lbl = icfunc.Function.Id;
			}
			
			Tree top = Top;
			string par = "";
			
			if (top != null)
			{
				par = String.Format("{0}.{1}", top.State.Property.Object.FullId, top.State.Property.Name);
			}

			return string.Format("[{0}, {1}, ({2})]", par, lbl, Descendants);
		}
	}
}

