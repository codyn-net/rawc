using System;
using System.Collections.Generic;
using System.Text;
using Cdn.RawC.Plugins.Attributes;
using Cdn.RawC.Plugins;

namespace Cdn.RawC.Tree.Collectors
{
	[Plugin(Description="Default Algorithm", Author="Jesse van den Kieboom")]
	public class Default : ICollector
	{
		public Result Collect(Node[] forest)
		{
			Result ret = new Result();
			Dictionary<string, List<Node>> samenodes = new Dictionary<string, List<Node>>();
			List<string> morethanone = new List<string>();

			// The default implementation is very basic, it just compares the whole expression
			for (int i = 0; i < forest.Length; ++i)
			{
				if ((forest[i].DescendantsCount + 1) < Options.Instance.MinimumEmbeddingSize &&
				    !(forest[i].Instruction is InstructionCustomFunction))
				{
					continue;
				}

				string sid = forest[i].Serialize();
				List<Node> lst;

				if (!samenodes.TryGetValue(sid, out lst))
				{
					lst = new List<Node>();
					samenodes[sid] = lst;
				}

				if (lst.Count == 1)
				{
					morethanone.Add(sid);
				}

				lst.Add(forest[i]);
			}

			foreach (string sid in morethanone)
			{
				AddResult(ret, samenodes[sid]);
			}

			return ret;
		}

		private void AddResult(Result ret, List<Node> lst)
		{
			Node proto = (Node)lst[0].Clone();
			List<NodePath> arguments = new List<NodePath>();

			// Find anonymous labels
			foreach (Node node in proto.Descendants)
			{
				if (node.Label[0] == Node.PlaceholderCode)
				{
					arguments.Add(node.Path);
				}
			}

			// Create embedding
			Embedding embedding = ret.Prototype(proto, arguments);

			foreach (Node node in lst)
			{
				embedding.Embed(node);
			}
		}
	}
}