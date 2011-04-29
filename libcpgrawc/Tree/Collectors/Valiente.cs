using System;
using System.Collections.Generic;
using System.Text;
using Cpg.RawC.Plugins.Attributes;
using Cpg.RawC.Plugins;

namespace Cpg.RawC.Tree.Collectors
{
	[Plugin(Description="Valiente Algorithm", Author="Jesse van den Kieboom")]
	public class Valiente : ICollector, IOptions
	{
		private class CustomOptions : CommandLine.OptionGroup
		{
			[Setting("labeled", true, Description="Whether to use labels")]
			public bool Labeled;
		}

		private List<Node> d_nodes;
		private Dictionary<Node, Node> d_mapping;
		private Dictionary<Node, List<Node>> d_reverseMapping;
		private CustomOptions d_options;
		
		public Valiente()
		{
			d_options = new CustomOptions();

			d_nodes = new List<Node>();
			d_mapping = new Dictionary<Node, Node>();
			d_reverseMapping = new Dictionary<Node, List<Node>>();
		}
		
		public CommandLine.OptionGroup Options
		{
			get
			{
				return d_options;
			}
		}
		
		public Result Collect(Node[] forest)
		{
			Calculate(forest);
			
			// d_nodes is a list of all root nodes constituting the embeddings
			// d_reverseMapping is a map from the roots to all the original nodes
			// which are contained in that root
			Result result = new Result();
			
			foreach (Node root in d_nodes)
			{
				List<Node> mapping = d_reverseMapping[root];
				
				// Replace the subexpression that was mapped on this root with an
				// embedding
				Node prototype = (Node)root.Clone();
				List<NodePath> arguments = new List<NodePath>();
				
				// Calculate the placeholder nodes
				foreach (Node node in prototype.Descendants)
				{
					if (Expression.InstructionCode(node.Instruction) == Expression.PlaceholderCode)
					{
						arguments.Add(node.Path);
					}
				}
				
				Embedding proto = result.Prototype(prototype, arguments);
				
				// Now we generate all the full expressions for this embedding
				foreach (Node inst in mapping)
				{
					// Replace inst in top hiearchy with embedding node
					proto.Embed(((Node)inst.Top.Clone()).FromPath(inst.Path));
				}
			}
			
			return result;
		}
		
		public void Calculate(Node[] forest)
		{
			Dictionary<uint, Node> lmap = new Dictionary<uint, Node>();
			Queue<Node> queue = new Queue<Node>();
			
			// Create initial mapping for leaves
			if (!d_options.Labeled)
			{
				Node leaf = Add(0, 0);
				leaf.IsLeaf = true;

				lmap[0] = leaf;
			}
			
			foreach (Node tree in forest)
			{
				foreach (Node leaf in tree.Leafs)
				{
					if (d_options.Labeled && !lmap.ContainsKey(leaf.Label))
					{
						Node n = Add(leaf.Label, 0);
						lmap[leaf.Label] = n;
					}
					
					queue.Enqueue(leaf);
				}
			}

			while (queue.Count != 0)
			{
				Node node = queue.Dequeue();
				
				if (node.IsLeaf)
				{
					Map(node, lmap[d_options.Labeled ? node.Label : 0]);
				}
				else
				{
					bool found = false;
					List<Node> children = new List<Node>();
					
					foreach (Node child in node.Children)
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
						else if (node.Degree != g.Degree || (d_options.Labeled && node.Label != g.Label))
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
					
						foreach (Node child in node)
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
				return d_options.Labeled;
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

			foreach (Node node in d_nodes)
			{
				if (d_reverseMapping.ContainsKey(node))
				{
					string[] parts = Array.ConvertAll<Node, string>(d_reverseMapping[node].ToArray(), a => a.ToString());
					builder.AppendFormat("{0} -> {1}\n", node.Height, string.Join(", ", parts));
				}
			}
			
			return builder.ToString();
		}
	}
}

