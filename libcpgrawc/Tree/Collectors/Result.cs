using System;
using System.Collections.Generic;

namespace Cpg.RawC.Tree.Collectors
{
	public class Result
	{
		private Dictionary<State, List<Embedding.Instance>> d_embeddings;
		private List<Embedding> d_prototypes;

		public Result()
		{
			d_prototypes = new List<Embedding>();
			d_embeddings = new Dictionary<State, List<Embedding.Instance>>();
		}
		
		public void Add(Embedding embedding)
		{
			d_prototypes.Add(embedding);
			embedding.InstanceAdded += PrototypeInstanceAdded;
		}
		
		public Embedding Prototype(Node node, IEnumerable<NodePath> arguments)
		{
			Embedding ret = new Embedding(node, arguments);
			Add(ret);
			
			return ret;
		}
		
		private void PrototypeInstanceAdded(object source, Embedding.InstanceArgs args)
		{
			List<Embedding.Instance> items;

			if (!d_embeddings.TryGetValue(args.Instance.State, out items))
			{
				items = new List<Embedding.Instance>();
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
		
		public IEnumerable<Embedding.Instance> Embeddings(State state)
		{
			List<Embedding.Instance> instances;
			
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

