using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Cpg.RawC.ExpressionTree
{
	public class SubNode : IEnumerable<Node>
	{
		private List<Node> d_nodes;

		public SubNode()
		{
			d_nodes = new List<Node>();
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

