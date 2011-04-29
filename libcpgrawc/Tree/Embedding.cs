using System;
using System.Collections.Generic;

namespace Cpg.RawC.Tree
{
	public class Embedding
	{
		public class Argument
		{
			private uint d_index;
			private NodePath d_path;
			
			public Argument(NodePath path, uint index)
			{
				d_index = index;
				d_path = path;
			}
			
			public uint Index
			{
				get
				{
					return d_index;
				}
				set
				{
					d_index = value;
				}
			}
			
			public NodePath Path
			{
				get
				{
					return d_path;
				}
			}
		}

		private List<Node> d_instances;
		private List<Argument> d_arguments;
		private List<NodePath> d_potentialArguments;
		private Node d_expression;
		private uint d_argumentIdx;
		
		public struct InstanceArgs
		{
			public Node Instance;
			
			public InstanceArgs(Node instance)
			{
				Instance = instance;
			}
		}

		public delegate void InstanceHandler(object source, InstanceArgs instance);
		public event InstanceHandler InstanceAdded = delegate {};
		public event InstanceHandler InstanceRemoved = delegate {};
		
		public class Instance : Instruction
		{
			private Embedding d_prototype;
			private List<ulong> d_embeddedIds;
			private Instruction d_originalInstruction;
	
			public Instance(Tree.Embedding prototype, IEnumerable<ulong> embeddedIds, Instruction originalInstruction)
			{
				d_prototype = prototype;
				d_embeddedIds = new List<ulong>(embeddedIds);
				d_originalInstruction = originalInstruction;
			}
			
			public Instance() : this(null, new List<ulong>(), null)
			{
			}
			
			public List<ulong> EmbeddedIds
			{
				get
				{
					return d_embeddedIds;
				}
			}
			
			public Embedding Prototype
			{
				get
				{
					return d_prototype;
				}
				set
				{
					d_prototype = value;
				}
			}
			
			public Instruction OriginalInstruction
			{
				get
				{
					return d_originalInstruction;
				}
			}

			public bool Conflicts(Embedding.Instance other)
			{
				// Compare overlap in embedded ids
				int i = 0;
				int j = 0;
				
				while (i < d_embeddedIds.Count && j < other.d_embeddedIds.Count)
				{
					if (d_embeddedIds[i] == other.d_embeddedIds[j])
					{
						return true;
					}
					else if (d_embeddedIds[i] < other.d_embeddedIds[j])
					{
						++i;
					}
					else
					{
						++j;
					}
				}
				
				return false;
			}
			
			public static new GLib.GType GType
			{
				get
				{
					return Instruction.GType;
				}
			}
		}
		
		public Embedding(Node node, IEnumerable<Argument> arguments) : this(node, new NodePath[] {})
		{
			d_arguments = new List<Argument>(arguments);
			
			Sort.Insertion(d_arguments, (a, b) => a.Index.CompareTo(b.Index));
		}
		
		public Embedding(Node node, IEnumerable<NodePath> arguments)
		{
			d_potentialArguments = new List<NodePath>(arguments);

			d_arguments = new List<Argument>();
			d_instances = new List<Node>();
			d_expression = (Node)node.Clone();
			
			d_argumentIdx = 0;
		}
		
		public bool Conflicts(Embedding other)
		{
			foreach (Node node in d_instances)
			{
				foreach (Node othernode in other.Instances)
				{
					if (node.State != othernode.State)
					{
						continue;
					}
					
					Instance a = (Instance)node.Instruction;
					Instance b = (Instance)othernode.Instruction;

					if (a.Conflicts(b))
					{
						return true;
					}
				}
			}
			
			return false;
		}
		
		public Node Expression
		{
			get
			{
				return d_expression;
			}
		}
		
		public void Embed(Node embed)
		{
			List<ulong> embeddedIds = new List<ulong>();
			
			embed.Instruction = new Instance(this, embeddedIds, embed.Instruction);

			Add(embed);
		}
		
		public void Revert()
		{
			while (d_instances.Count > 0)
			{
				Remove(d_instances[0]);
			}
		}
		
		public void Remove(Node instance)
		{
			instance.Instruction = ((Instance)instance.Instruction).OriginalInstruction;
			d_instances.Remove(instance);

			InstanceRemoved(this, new InstanceArgs(instance));
		}
		
		private bool SameArguments(Node a, Node b)
		{
			// Check if the nodes in a and b, at 'path' are the same thing
			if (!a.Instruction.Equal(b.Instruction))
			{
				return false;
			}
			
			// We need an additional check for properties because Instruction.Equal
			// defines property instructions to be equal only based on checking the
			// name of the property and the binding (because this means equality in terms
			// of expression). However, we need to know _exact_ equality
			InstructionProperty aprop = a.Instruction as InstructionProperty;
			InstructionProperty bprop = b.Instruction as InstructionProperty;
			
			if (aprop != null && bprop != null)
			{
				return aprop.Property == bprop.Property;
			}
			
			if (a.Children.Count != b.Children.Count)
			{
				return false;
			}
			
			// Compare children
			for (int i = 0; i < a.Children.Count; ++i)
			{
				if (!SameArguments(a.Children[i], b.Children[i]))
				{
					return false;
				}
			}
			
			return true;
		}
		
		private bool SameArguments(Node a, Node b, NodePath path)
		{
			return SameArguments(a.FromPath(path), b.FromPath(path));
		}
		
		private bool ArgumentMatch(Argument argument, NodePath path)
		{
			NodePath orig = argument.Path;
			
			foreach (Node instance in d_instances)
			{
				if (!SameArguments(instance.FromPath(orig), instance.FromPath(path)))
				{
					return false;
				}
			}
			
			return true;
		}
		
		private bool MergeArgument(NodePath path)
		{
			// See if this can be represented by a previous argument already
			foreach (Argument argument in d_arguments)
			{
				if (ArgumentMatch(argument, path))
				{
					// Use same index
					d_arguments.Add(new Argument(path, argument.Index));
					return true;
				}
			}
			
			uint idx = d_argumentIdx++;

			// Unmerge previous arguments
			foreach (Argument argument in d_arguments)
			{
				if (argument.Path == path)
				{
					argument.Index = idx;
				}
			}

			d_arguments.Add(new Argument(path, idx));
			return false;
		}
		
		private void VerifyArguments(Node added)
		{
			d_potentialArguments.RemoveAll(delegate (NodePath path) {
				bool needarg = d_instances.Count > 1 && !SameArguments(d_instances[0], added, path);
				
				if (needarg)
				{
					needarg = !MergeArgument(path);
				}
				
				return needarg;
			});
		}
		
		public void Add(Node instance)
		{
			if (!(instance.Instruction is Instance))
			{
				throw new Exception("Cannot add embedding instance which is not a real embedding");
			}

			d_instances.Add(instance);

			VerifyArguments(instance);

			InstanceAdded(this, new InstanceArgs(instance));
		}

		public IEnumerable<Node> Instances
		{
			get
			{
				return d_instances;
			}
		}
		
		public IEnumerable<Argument> Arguments
		{
			get
			{
				return d_arguments;
			}
		}
		
		public int InstancesCount
		{
			get
			{
				return d_instances.Count;
			}
		}
	}
}

