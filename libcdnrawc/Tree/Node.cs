using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cdn.RawC.Tree
{
	public class Node : IEnumerable<Node>, IComparable<Node>, ICloneable, Programmer.DataTable.IKey
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
		
		public static Node Create(State state, Cdn.Expression expression)
		{
			return Create(state, expression.Instructions);
		}

		public static Node Create(State state, Cdn.Instruction[] instructions)
		{
			Stack<Node > stack = new Stack<Node>();
			
			for (int i = 0; i < instructions.Length; ++i)
			{
				Instruction inst = instructions[i];
				
				Node node = new Node(state, inst);

				int numargs = 0;
				
				InstructionCustomOperator icop = inst as InstructionCustomOperator;
				numargs = (int)inst.GetStackManipulation().Pop.Num;

				for (int j = 0; j < numargs; ++j)
				{
					node.Add(stack.Pop());
				}
				
				if (icop != null && !(icop.Operator is OperatorDelayed))
				{
					// TODO Support for operators...
					/*foreach (Cdn.Expression ex in icop.Operator.Expressions)
					{
						node.Add(Create(state, ex.Instructions));
					}*/
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
			uint size = 0;
			
			d_label = 0;
			
			d_state = state;

			d_instruction = instruction;

			if (instruction != null)
			{
				d_label = InstructionCode(instruction);

				if (instruction.GetStackManipulation() != null)
				{
					size = instruction.GetStackManipulation().Pop.Num;
				}

				d_isCommutative = instruction.IsCommutative;
			}

			d_isLeaf = size == 0;
			
			d_children = new List<Node>((int)size);
			d_leafs = new SortedList<Node>();
		}

		public object DataKey
		{
			get
			{
				if (Instruction is InstructionRand)
				{
					return Instruction;
				}

				InstructionVariable prop = Instruction as InstructionVariable;
				
				if (prop != null)
				{
					return prop.Variable;
				}

				Programmer.Instructions.State st = Instruction as Programmer.Instructions.State;

				if (st != null)
				{
					return st.Item.Key;
				}
				
				InstructionCustomOperator op = Instruction as InstructionCustomOperator;
				
				if (op != null && op.Operator is OperatorDelayed)
				{
					OperatorDelayed opdel = (OperatorDelayed)op.Operator;
					double delay = 0;

					Knowledge.Instance.LookupDelay(op, out delay);
					return new DelayedState.Key(opdel, delay);
				}

				InstructionNumber opnum = Instruction as InstructionNumber;
				
				if (opnum != null)
				{
					return opnum.Value;
				}

				if (d_state != null)
				{
					return d_state.DataKey;
				}

				return null;
			}
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
			
			Cdn.RawC.Sort.Insertion(d_children);
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
			InstructionVariable iprop;
			InstructionNumber inum;

			ifunc = d_instruction as InstructionFunction;
			icfunc = d_instruction as InstructionCustomFunction;
			icop = d_instruction as InstructionCustomOperator;
			iprop = d_instruction as InstructionVariable;
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
				lbl = "?" + iprop.Variable.FullNameForDisplay;
			}
			else if (inum != null)
			{
				lbl = "?" + inum.Representation;
			}
			else if (d_instruction is InstructionRand)
			{
				lbl = "rand";
			}
			
			string par = "";
			
			Node top = Top;
			
			if (top != null && top.State != null && withstate)
			{
				Variable v = top.State.Object as Variable;

				if (v != null)
				{
					par = String.Format("{0}, ", v.FullNameForDisplay);
				}
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
			
			var smanip = d_instruction.GetStackManipulation();
			var dim = smanip.Push.Dimension;
			
			if (!dim.IsOne)
			{
				ret.AppendFormat("[{0},{1}]", dim.Rows, dim.Columns);
			}
			
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

		private static Dictionary<string, uint> s_hashMapping;
		private static uint s_nextMap;
		
		static Node()
		{
			s_hashMapping = new Dictionary<string, uint>();
			s_nextMap = (uint)MathFunctionType.Num + (uint)MathFunctionType.Num + 1;
		}
		
		private static uint HashMap(string id)
		{
			uint ret;

			if (!s_hashMapping.TryGetValue(id, out ret))
			{
				ret = s_nextMap++;
				s_hashMapping[id] = ret;
			}
			
			return ret;
		}
		
		private static bool InstructionIs<T>(Instruction inst, out T t)
		{
			if (inst is T)
			{
				t = (T)(object)inst;
				return true;
			}
			else
			{
				t = default(T);
			}
			
			return false;
		}

		public static IEnumerable<uint> InstructionCodes(Instruction inst)
		{
			return InstructionCodes(inst, false);
		}

		public static uint InstructionCode(Instruction inst)
		{
			return InstructionCode(inst, false);
		}

		public static uint InstructionCode(Instruction inst, bool strict)
		{
			foreach (uint i in InstructionCodes(inst, strict))
			{
				return i;
			}

			return 0;
		}
		
		public static IEnumerable<uint> InstructionCodes(Instruction inst, bool strict)
		{
			InstructionFunction ifunc;
			InstructionCustomOperator icusop;
			InstructionCustomFunction icusf;
			InstructionVariable ivar;
			InstructionNumber inum;
			InstructionRand irand;

			if (InstructionIs(inst, out icusf))
			{
				// Generate byte code for this function by name
				yield return HashMap("f_" + icusf.Function.FullId);
			}
			else if (InstructionIs(inst, out icusop))
			{
				if (icusop.Operator is OperatorDelayed && !strict)
				{
					// These are actually part of the state table, so we use
					// a placeholder code here
					yield return PlaceholderCode;
				}
				else
				{
					bool ns = strict || icusop.Operator is OperatorDelayed;

					yield return HashMap("co_" + icusop.Operator.Name);

					Cdn.Function f = icusop.Operator.PrimaryFunction;

					if (f != null && f.Expression != null)
					{
						foreach (Instruction i in f.Expression.Instructions)
						{
							foreach (uint id in InstructionCodes(i, ns))
							{
								yield return id;
							}
						}
					}
					else
					{
						foreach (Cdn.Expression[] exprs in icusop.Operator.AllExpressions())
						{
							foreach (Cdn.Expression e in exprs)
							{
								foreach (Instruction i in e.Instructions)
								{
									foreach (uint id in InstructionCodes(i, ns))
									{
										yield return id;
									}
								}
							}
						}

						foreach (Cdn.Expression[] exprs in icusop.Operator.AllIndices())
						{
							foreach (Cdn.Expression e in exprs)
							{
								foreach (Instruction i in e.Instructions)
								{
									foreach (uint id in InstructionCodes(i, ns))
									{
										yield return id;
									}
								}
							}
						}
					}
				}
			}
			else if (InstructionIs(inst, out ifunc))
			{
				// Functions just store the id
				yield return (uint)ifunc.Id + 1;
			}
			else if (strict)
			{
				if (InstructionIs(inst, out ivar))
				{
					yield return HashMap(String.Format("var_{0}", ivar.Variable.FullName));
				}
				else if (InstructionIs(inst, out inum))
				{
					yield return HashMap(String.Format("num_{0}", inum.Value));
				}
				else if (InstructionIs(inst, out irand))
				{
					yield return HashMap(String.Format("rand_{0}", irand.Handle));
				}
				else
				{
					throw new NotImplementedException(String.Format("Unhandled strict instruction code: {0}", inst.GetType()));
				}
			}
			else
			{
				// Placeholder for numbers, properties and rands
				yield return PlaceholderCode;
			}
		}
		
		public const uint PlaceholderCode = 0;

	}
}

