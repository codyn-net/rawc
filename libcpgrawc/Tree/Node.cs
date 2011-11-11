using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cpg.RawC.Tree
{
	public class Node : IEnumerable<Node>, IComparable<Node>, ICloneable
	{
		private State d_state;
		private Instruction d_instruction;
		private uint d_label;
		private List<Node> d_children;
		private SortedList<Node> d_leafs;
		private Node d_parent;
		private bool d_isLeaf;
		private uint d_height;
		private uint d_degree;
		private uint d_childCount;
		private uint d_descendants;
		private bool d_isCommutative;
		private ulong d_treeId;

		public static Node Create(State state, Cpg.Instruction[] instructions)
		{
			Stack<Node > stack = new Stack<Node>();
			
			for (int i = 0; i < instructions.Length; ++i)
			{
				Instruction inst = instructions[i];
				
				Node node = new Node(state, inst);

				int numargs = 0;
				
				InstructionFunction ifunc = inst as InstructionFunction;
				InstructionCustomFunction icfunc = inst as InstructionCustomFunction;
				InstructionCustomOperator icop = inst as InstructionCustomOperator;
				
				if (ifunc != null)
				{
					numargs = ifunc.Arguments;
				}
				else if (icfunc != null)
				{
					numargs = icfunc.Arguments;
				}
				
				for (int j = 0; j < numargs; ++j)
				{
					node.Add(stack.Pop());
				}
				
				if (icop != null && !(icop.Operator is OperatorDelayed))
				{
					foreach (Cpg.Expression ex in icop.Operator.Expressions)
					{
						node.Add(Create(state, ex.Instructions));
					}
				}
				
				node.d_children.Reverse();

				stack.Push(node);
			}
			
			Node ret = stack.Pop();
			
			ret.Sort();
			ret.UpdateTreeId();
			
			return ret;
		}

		public static Node Create(State state)
		{
			return Create(state, state.Instructions);
		}

		public Node(uint label) : this(null, null)
		{
			d_label = label;
		}
		
		public Node() : this(0)
		{
		}
		
		public ulong TreeId
		{
			get
			{
				return d_treeId;
			}
			set
			{
				d_treeId = value;
			}			
		}
		
		public Node(State state, Instruction instruction)
		{
			int size = 0;
			
			d_label = 0;
			
			d_state = state;

			if (instruction != null)
			{
				d_label = Expression.InstructionCode(instruction);
			}

			d_instruction = instruction;

			InstructionFunction ifunc = instruction as InstructionFunction;
				
			if (ifunc != null && ifunc.Arguments > 0)
			{
				size = ifunc.Arguments;
			}
			
			InstructionOperator iop = instruction as InstructionOperator;
			
			if (iop != null)
			{
				d_isCommutative = Cpg.Math.OperatorIsCommutative((Cpg.MathOperatorType)iop.Id);
			}
			else if (ifunc != null)
			{
				d_isCommutative = Cpg.Math.FunctionIsCommutative((Cpg.MathFunctionType)ifunc.Id);
			}
			else
			{
				d_isCommutative = false;
			}			
			
			InstructionCustomFunction icfunc = instruction as InstructionCustomFunction;
			
			if (icfunc != null && icfunc.Arguments > 0)
			{
				size = icfunc.Arguments;
			}
			
			d_isLeaf = size == 0;
			
			d_children = new List<Node>(size);
			d_leafs = new SortedList<Node>();
		}
		
		public void UpdateTreeId()
		{
			ulong treeid = 0;
			PropagateTreeId(ref treeid);
		}
		
		private void PropagateTreeId(ref ulong treeid)
		{
			d_treeId = treeid++;
			
			foreach (Node child in d_children)
			{
				child.PropagateTreeId(ref treeid);
			}
		}
		
		private void CollectDescendants(List<Node> ret)
		{
			foreach (Node child in d_children)
			{
				ret.Add(child);
				child.CollectDescendants(ret);
			}
		}
		
		public Node[] Descendants
		{
			get
			{
				List<Node > ret = new List<Node>();
				
				CollectDescendants(ret);
				return ret.ToArray();
			}
		}
		
		private void Copy(Node other, bool children)
		{
			d_instruction = other.d_instruction;
			d_isLeaf = other.d_isLeaf;
			d_isCommutative = other.d_isCommutative;
			d_childCount = other.d_childCount;
			d_degree = other.d_degree;
			d_height = other.d_height;
			d_state = other.d_state;
			d_descendants = other.d_descendants;
			d_treeId = other.d_treeId;
			
			if (children)
			{
				d_children = new List<Node>(other.d_children);
			}
		}
		
		public object Clone()
		{
			Node node = new Node(d_label);
			node.Copy(this, false);
			
			foreach (Node child in d_children)
			{
				Node newchild = (Node)child.Clone();
				
				node.d_children.Add(newchild);
				newchild.d_parent = node;
				
				foreach (Node leaf in newchild.Leafs)
				{
					d_leafs.Add(leaf);
				}
				
				if (newchild.IsLeaf)
				{
					d_leafs.Add(newchild);
				}
			}
			
			return node;
		}
		
		public NodePath RelPath(Node parent)
		{
			if (d_parent == null || this == parent)
			{
				return new NodePath();
			}
			else
			{
				NodePath path = d_parent.RelPath(parent);
				path.Push((uint)d_parent.Children.IndexOf(this));
				
				return path;
			}
		}

		public NodePath Path
		{
			get
			{
				return RelPath(null);
			}
		}
		
		public Node FromPath(NodePath path)
		{
			Node node = this;
			path = new NodePath(path);

			while (path.Count != 0)
			{
				uint idx = path.Pop();
				
				if (idx >= (uint)node.Children.Count)
				{
					return null;
				}
				
				node = node.Children[(int)idx];
			}
			
			return node;
		}

		public State State
		{
			get	{ return d_state; }
		}
		
		public List<Node> Leafs
		{
			get { return d_leafs; }
		}

		public bool IsLeaf
		{
			get { return d_isLeaf; }
			set { d_isLeaf = value; }
		}
		
		public bool IsCommutative
		{
			get { return d_isCommutative; }
		}
		
		public uint Degree
		{
			get { return d_degree; }
		}
		
		public uint DescendantsCount
		{
			get { return d_descendants; }
		}
		
		public uint ChildCount
		{
			get { return d_childCount; }
			set { d_childCount = value; }
		}
		
		public uint Label
		{
			get { return d_label; }
		}
		
		public List<Node> Children
		{
			get { return d_children; }
		}

		public Instruction Instruction
		{
			get { return d_instruction; }
			set { d_instruction = value; }
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
			get { return d_height; }
			set
			{
				d_height = value;
				
				PropagateHeight();
			}
		}

		public Node Parent
		{
			get { return d_parent; }
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
		
		private void Unleaf(Node child)
		{
			d_leafs.Remove(child);
			
			if (d_parent != null)
			{
				d_parent.Unleaf(child);
			}
		}
		
		public void Add(Node child, bool setParent)
		{
			if (setParent)
			{
				child.Parent = this;
			}
			
			// This node is no longer a leaf because it has a child now
			if (d_parent != null && d_children.Count == 0)
			{
				d_parent.Unleaf(this);
			}

			d_children.Add(child);
			
			++d_degree;
			++d_childCount;
			
			d_descendants += child.DescendantsCount + 1;
			
			foreach (Node leaf in child.Leafs)
			{
				if (d_leafs.Find(leaf) == null)
				{
					d_leafs.Add(leaf);
				}
			}
			
			if (child.IsLeaf)
			{
				d_leafs.Add(child);
			}
		}
		
		public Node Top
		{
			get
			{
				if (d_parent != null)
				{
					return d_parent.Top;
				}
				else
				{
					return this;
				}
			}
		}
		
		public void Replace(Node node)
		{
			Copy(node, false);
		}
		
		public void Replace(NodePath path, Node node)
		{
			Replace(path, node, false);
		}
		
		public void Replace(NodePath path, Node node, bool reconnect)
		{
			Node parent;
			int idx;
			
			if (path.Count == 0)
			{
				parent = Parent;
				idx = Parent.Children.IndexOf(this);
			}
			else
			{
				path = new NodePath(path);

				idx = (int)path.Pop();
				parent = FromPath(path);
			}

			Node orig = parent.Children[idx];
			
			parent.Children[idx] = node;
			node.Parent = parent;
			
			if (reconnect)
			{
				foreach (Node child in orig.Children)
				{
					node.Add(child);
				}
			}
		}
		
		public void Sort()
		{
			// Sort the node
			foreach (Node child in d_children)
			{
				child.Sort();
			}
			
			if (!d_isCommutative)
			{
				return;
			}
			
			Cpg.RawC.Sort.Insertion(d_children);
		}

		public override string ToString()
		{
			return ToString(true);
		}
		
		public string ToString(bool withstate)
		{
			string lbl = "?";

			InstructionFunction ifunc;
			InstructionCustomFunction icfunc;
			InstructionCustomOperator icop;
			InstructionProperty iprop;
			InstructionConstant icons;
			InstructionNumber inum;

			ifunc = d_instruction as InstructionFunction;
			icfunc = d_instruction as InstructionCustomFunction;
			icop = d_instruction as InstructionCustomOperator;
			iprop = d_instruction as InstructionProperty;
			icons = d_instruction as InstructionConstant;
			inum = d_instruction as InstructionNumber;
			
			if (ifunc != null)
			{
				lbl = ifunc.Name;
			}
			else if (icfunc != null)
			{
				lbl = icfunc.Function.Id;
			}
			else if (icop != null)
			{
				lbl = icop.Operator.Name;
			}
			else if (iprop != null)
			{
				lbl = "?" + iprop.Property.FullNameForDisplay;
			}
			else if (icons != null)
			{
				lbl = "?" + icons.Symbol;
			}
			else if (inum != null)
			{
				lbl = "?" + inum.Value.ToString();
			}
			
			string par = "";
			
			Node top = Top;
			
			if (top != null && top.State != null && withstate)
			{
				par = String.Format("{0}, ", top.State.Property.FullNameForDisplay);
			}

			string cs = "";

			if (Children.Count != 0)
			{
				cs = ", (" + String.Join(", ", Array.ConvertAll<Tree.Node, string>(Children.ToArray(), a => a.ToString(false))) + ")";
			}

			return string.Format("[{0}{1}{2}]", par, lbl, cs);
		}

		public int CompareTo(Node other)
		{
			if (other == null)
			{
				return 1;
			}
			
			int ret = d_label.CompareTo(other.Label);
			
			if (ret != 0)
			{
				return ret;
			}
			
			// Equal, compare deeper
			ret = d_children.Count.CompareTo(other.Children.Count);
			
			if (ret != 0)
			{
				return ret;
			}
			
			// Same number of children, compare them left to right
			for (int i = 0; i < d_children.Count; ++i)
			{
				ret = d_children[i].CompareTo(other.Children[i]);
				
				if (ret != 0)
				{
					return ret;
				}
			}
			
			return 0;
		}
		
		public IEnumerable<Node> Collect<T>()
		{
			if (d_instruction is T)
			{
				yield return this;
			}

			foreach (Node child in Descendants)
			{
				if (child.Instruction is T)
				{
					yield return child;
				}
			}
		}
		
		public IEnumerable<Node> Collect(Node other)
		{
			yield return other;

			// Find all nodes in other that correspond to the same nodes in this
			for (int i = 0; i < Children.Count; ++i)
			{
				foreach (Node child in Children[i].Collect(other.Children[i]))
				{
					yield return child;
				}
			}
		}
		
		public string Serialize()
		{
			StringBuilder ret = new StringBuilder();
			ret.Append(Label);
			ret.Append("(");
			
			for (int i = 0; i < d_children.Count; ++i)
			{
				if (i != 0)
				{
					ret.Append(", ");
				}
				
				ret.Append(d_children[i].Serialize());
			}
			
			ret.Append(")");
			
			return ret.ToString();
		}
	}
}

