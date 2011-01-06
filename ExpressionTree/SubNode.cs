using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Cpg.RawC.ExpressionTree
{
	public class SubNode : IEnumerable<Node>
	{
		private SortedList<Node> d_nodes;
		private Node d_parent;
		
		public SubNode() : this(null)
		{
		}

		public SubNode(Node parent)
		{
			d_nodes = new SortedList<Node>();
			d_parent = parent;
		}
		
		public Node Parent
		{
			get
			{
				return d_parent;
			}
			set
			{
				d_parent = value;
			}
		}
		
		public void Add(Node node)
		{
			node.Parent = this;
			d_nodes.Add(node);
		}
		
		public bool Empty
		{
			get
			{
				return d_nodes.Count == 0;
			}
		}
		
		public Node Find(Node node)
		{
			return d_nodes.Find(node);
		}
		
		public IEnumerable<Node> Nodes
		{
			get
			{
				return d_nodes;
			}
		}
		
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			
			foreach (Node node in this)
			{
				string n = node.ToString();
				
				if (!String.IsNullOrEmpty(n))
				{
					string s = String.Join("\n", Array.ConvertAll(n.Split('\n'), a => String.Format("  {0}", a)));
					
					if (builder.Length != 0)
					{
						builder.AppendLine();
					}

					builder.Append(s);
				}
			}
			
			return builder.ToString();
		}
		
		public void Dot(TextWriter writer)
		{
			writer.WriteLine("{0} [shape=point,width=0.1];", (uint)GetHashCode());

			foreach (Node child in this)
			{
				child.Dot(writer);
				writer.WriteLine("{0} -> {1};", (uint)GetHashCode(), (uint)child.GetHashCode());
			}
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return d_nodes.GetEnumerator();
		}
		
		public IEnumerator<Node> GetEnumerator()
		{
			return d_nodes.GetEnumerator();
		}
	}
}

