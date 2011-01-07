using System;
using System.Collections.Generic;
using System.Text;

namespace Cpg.RawC.ExpressionTree
{
	public class Graph
	{		
		private List<Node> d_nodes;
		private Dictionary<Node, Node> d_mapping;
		private Dictionary<Node, List<Node>> d_reverseMapping;
		private bool d_labeled;

		public Graph(bool labeled, params Tree[] forest)
		{
			d_nodes = new List<Node>();
			d_mapping = new Dictionary<ExpressionTree.Node, Node>();
			d_reverseMapping = new Dictionary<Node, List<Node>>();
			d_labeled = labeled;
			
			Dictionary<uint, Node> lmap = new Dictionary<uint, Node>();
			Queue<ExpressionTree.Node> queue = new Queue<ExpressionTree.Node>();
			
			// Create initial mapping for leaves
			if (!d_labeled)
			{
				Node leaf = Add(0, 0);
				leaf.IsLeaf = true;

				lmap[0] = leaf;
			}
			
			foreach (Tree tree in forest)
			{
				foreach (ExpressionTree.Node leaf in tree.Leaves)
				{
					if (d_labeled && !lmap.ContainsKey(leaf.Label))
					{
						Node n = Add(leaf.Label, 0);
						lmap[leaf.Label] = n;
					}
					
					queue.Enqueue(leaf);
				}
			}

			while (queue.Count != 0)
			{
				ExpressionTree.Node node = queue.Dequeue();
				
				if (node.IsLeaf)
				{
					Map(node, lmap[d_labeled ? node.Label : 0]);
				}
				else
				{
					bool found = false;
					List<Node> children = new List<Node>();
					
					foreach (ExpressionTree.Node child in node.Children)
					{
						children.Add(d_mapping[child]);
					}
					
					for (int i = d_nodes.Count - 1; i >= 0; --i)
					{
						Node g = d_nodes[i];
						
						if (node.Height != g.Height)
						{
							break;
						}
						else if (node.Degree != g.Degree || (d_labeled && node.Label != g.Label))
						{
							continue;
						}
						
						bool match = true;
						
						for (int j = 0; j < children.Count; ++j)
						{
							if (!Object.ReferenceEquals(g.Children[j], children[j]))
							{
								match = false;
								break;
							}
						}
						
						if (match)
						{
							Map(node, g);
							found = true;
							break;
						}
					}
					
					if (!found)
					{
						Node w = Add(node);
					
						foreach (ExpressionTree.Node child in node)
						{
							// Explicitly directly on children instead of the Add method because
							// we want to avoid side-effects (like parenting, and height recalculation)
							w.Add(d_mapping[child], false);
						}
					}
				}

				if (node.Parent != null)
				{
					if (node.Parent.ChildCount > 0)
					{
						--node.Parent.ChildCount;
					}

					if (node.Parent.ChildCount == 0)
					{
						queue.Enqueue(node.Parent);
					}
				}
			}
		}
		
		private void Map(Node original, Node graph)
		{
			d_mapping[original] = graph;
			List<Node> reverse;
			
			if (!d_reverseMapping.TryGetValue(graph, out reverse))
			{
				reverse = new List<Node>();
				d_reverseMapping[graph] = reverse;
			}
			
			reverse.Add(original);
		}
		
		public bool Labeled
		{
			get
			{
				return d_labeled;
			}
		}
		
		public Dictionary<Node, Node> Mapping
		{
			get
			{
				return d_mapping;
			}
		}
		
		public Dictionary<Node, List<Node>> ReverseMapping
		{
			get
			{
				return d_reverseMapping;
			}
		}
		
		public Node Add(uint label, uint height)
		{
			Node n = new Node(label);
			n.Height = height;

			d_nodes.Add(n);
			
			return n;
		}
		
		public Node Add(Node node)
		{
			Node ret = Add(node.Label, node.Height);
			
			Map(node, ret);
			
			return ret;
		}
		
		public List<Node> Nodes
		{
			get
			{
				return d_nodes;
			}
		}
		
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			foreach (ExpressionTree.Node node in d_nodes)
			{
				if (d_reverseMapping.ContainsKey(node))
				{
					string[] parts = Array.ConvertAll<ExpressionTree.Node, string>(d_reverseMapping[node].ToArray(), a => a.ToString());
					builder.AppendFormat("{0} -> {1}\n", node.Height, string.Join(", ", parts));
				}
			}
			
			return builder.ToString();
		}
	}
}

