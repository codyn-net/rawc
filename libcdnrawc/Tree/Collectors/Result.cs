using System;
using System.Collections.Generic;

namespace Cdn.RawC.Tree.Collectors
{
	public class Result
	{
		private Dictionary<State, List<Node>> d_embeddings;
		private List<Embedding> d_prototypes;

		public Result()
		{
			d_prototypes = new List<Embedding>();
			d_embeddings = new Dictionary<State, List<Node>>();
		}

		public void Add(Embedding embedding)
		{
			d_prototypes.Add(embedding);
			embedding.InstanceAdded += PrototypeInstanceAdded;
		}

		public Embedding Prototype(Node node, IEnumerable<NodePath> arguments)
		{
			Embedding ret = new Embedding((Node)node.Clone(), arguments);
			Add(ret);

			return ret;
		}

		private void PrototypeInstanceAdded(object source, Embedding.InstanceArgs args)
		{
			List<Node > items;

			if (!d_embeddings.TryGetValue(args.Instance.State, out items))
			{
				items = new List<Node>();
				d_embeddings[args.Instance.State] = items;
			}

			items.Add(args.Instance);
		}

		public IEnumerable<Embedding> Prototypes
		{
			get
			{
				return d_prototypes;
			}
		}

		public IEnumerable<Node> Embeddings(State state)
		{
			List<Node > instances;

			if (d_embeddings.TryGetValue(state, out instances))
			{
				return instances;
			}
			else
			{
				return null;
			}
		}
	}
}

