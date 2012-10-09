using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class DependencyGraph
	{
		private class Node
		{
			public State State;
			public HashSet<Node> Dependencies;
			public HashSet<Node> DependencyFor;
			public Tree.Embedding Embedding;
			
			public Node(State state)
			{
				State = state;
				Dependencies = new HashSet<Node>();
				DependencyFor = new HashSet<Node>();
			}
		}
		
		private Node d_root;
		private DataTable d_states;
		private Dictionary<object, List<Node>> d_unresolved;
		private Dictionary<object, Node> d_nodeMap;
		private Dictionary<State, Node> d_stateMap;
		private Dictionary<State, Tree.Embedding> d_embeddingsMap;

		private DependencyGraph()
		{
			d_root = new Node(null);

			d_unresolved = new Dictionary<object, List<Node>>();
			d_nodeMap = new Dictionary<object, Node>();
			d_stateMap = new Dictionary<State, Node>();
			d_embeddingsMap = new Dictionary<State, Tree.Embedding>();
		}

		public bool DependsOn(State state, object obj)
		{
			Node node = d_stateMap[state];
			Queue<Node> deps = new Queue<Node>();

			deps.Enqueue(node);

			while (deps.Count > 0)
			{
				var n = deps.Dequeue();

				if (n.State.Object == obj || n.State.DataKey == obj)
				{
					return true;
				}

				foreach (var dep in n.Dependencies)
				{
					deps.Enqueue(dep);
				}
			}

			return false;
		}

		public DependencyGraph(DataTable states, IEnumerable<Tree.Embedding> embeddings) : this()
		{
			d_states = states;

			foreach (var embedding in embeddings)
			{
				foreach (var instance in embedding.Instances)
				{
					if (instance.State != null)
					{
						d_embeddingsMap[instance.State] = embedding;
					}
				}
			}

			foreach (DataTable.DataItem item in d_states)
			{
				State st = item.Object as State;

				if (st != null)
				{
					Add(st);
				}
			}
		}

		private void CollapseNode(DependencyGraph ret,
		                          Node node,
		                          HashSet<State> states,
		                          Node parent,
		                          HashSet<Node> seen,
		                          HashSet<Node> leafs)
		{
			if (seen.Contains(node))
			{
				return;
			}

			seen.Add(node);
			bool checkleaf = false;

			if (node.State != null && states.Contains(node.State))
			{
				Node newnode;

				if (!ret.d_stateMap.TryGetValue(node.State, out newnode))
				{
					newnode = new Node(node.State);
					ret.d_stateMap[node.State] = newnode;
				}

				// Set dependencies
				parent.Dependencies.Add(newnode);
				newnode.DependencyFor.Add(parent);

				// The newnode now becomes the new parent
				parent = newnode;

				checkleaf = true;
			}

			foreach (var dependency in node.Dependencies)
			{
				CollapseNode(ret, dependency, states, parent, seen, leafs);
			}

			if (checkleaf && parent.Dependencies.Count == 0)
			{
				leafs.Add(parent);
			}
		}

		private HashSet<Node> Collapse(HashSet<State> states)
		{
			DependencyGraph ret = new DependencyGraph();

			ret.d_states = d_states;
			ret.d_embeddingsMap = d_embeddingsMap;

			// Create a new dependency graph in which only nodes in 'states'
			// appear
			HashSet<Node> leafs = new HashSet<Node>();

			CollapseNode(ret,
			             d_root,
			             states,
			             d_root,
			             new HashSet<Node>(),
			             leafs);

			return leafs;
		}

		public class Group : IEnumerable<Group>
		{
			private Tree.Embedding d_embedding;
			private List<State> d_states;
			private Group d_previous;
			private Group d_next;
			private uint d_id;

			public Group(Tree.Embedding embedding)
			{
				d_embedding = embedding;
				d_states = new List<State>();
				d_id = 0;
			}

			public IEnumerator<Group> GetEnumerator()
			{
				for (var next = this; next != null; next = next.d_next)
				{
					yield return next;
				}
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			protected void UpdateIdBackwards()
			{
				d_id = d_next.d_id - 1;

				if (d_previous != null)
				{
					d_previous.UpdateIdBackwards();
				}
			}

			protected void UpdateIdForwards()
			{
				d_id = d_previous.d_id + 1;

				if (d_next != null)
				{
					d_next.UpdateIdBackwards();
				}
			}

			public Tree.Embedding Embedding
			{
				get { return d_embedding; }
			}

			public IEnumerable<State> States
			{
				get { return d_states; }
			}

			public int StatesCount
			{
				get { return d_states.Count; }
			}

			public Group Previous
			{
				get
				{
					return d_previous;
				}
				set
				{
					if (d_previous != null)
					{
						value.d_previous = d_previous;

						d_previous.d_next = value;
						d_previous = value;
					}
					else
					{
						d_previous = value;
					}

					d_previous.d_next = this;
					d_previous.UpdateIdForwards();
				}
			}

			public Group Next
			{
				get
				{
					return d_next;
				}
				set
				{
					if (d_next != null)
					{
						value.d_next = d_next;

						d_next.d_previous = value;
						d_next = value;
					}
					else
					{
						d_next = value;
					}

					d_next.d_previous = this;
					d_next.UpdateIdForwards();
				}
			}

			public uint Id
			{
				get { return d_id; }
			}

			public void Add(State state)
			{
				d_states.Add(state);
			}
		}

		public Group Sort(HashSet<State> states)
		{
			HashSet<Node> leafs;

			leafs = Collapse(states);

			Group ret = new Group(null);
			Dictionary<Node, Group> seen = new Dictionary<Node, Group>();

			// Go from all the leafs upwards
			foreach (Node leaf in leafs)
			{
				ComputeNodeInGroups(leaf, ret, seen);
			}

			return ret;
		}

		private void ComputeNodeInGroups(Node node,
		                                 Group grp,
		                                 Dictionary<Node, Group> seen)
		{
			if (seen.ContainsKey(node))
			{
				return;
			}

			Group root = grp;

			// Find right-most dependency
			foreach (Node dep in node.Dependencies)
			{
				Group depgrp;

				if (!seen.TryGetValue(dep, out depgrp))
				{
					continue;
				}

				if (depgrp.Id > root.Id)
				{
					root = depgrp;
				}
			}

			// Add in root, or on the right side of root (i.e. compute after root)
			while (root.Embedding != node.Embedding && root.StatesCount > 0)
			{
				var next = root.Next;

				if (next == null)
				{
					root.Next = new Group(node.Embedding);
					root = root.Next;
					break;
				}
			}

			root.Add(node.State);
			seen[node] = root;

			// Recursively go up, i.e. nodes that depend on 'node'.
			foreach (Node dep in node.DependencyFor)
			{
				ComputeNodeInGroups(dep, root, seen);
			}
		}
		
		public void Add(State state)
		{
			Node node = new Node(state);
			d_stateMap[state] = node;

			Tree.Embedding embedding;

			if (d_embeddingsMap.TryGetValue(state, out embedding))
			{
				node.Embedding = embedding;
			}
			
			// Resolve currently unresolved dependencies first
			List<Node> lst;
			
			if (d_unresolved.TryGetValue(state.Object, out lst))
			{
				foreach (var n in lst)
				{
					n.Dependencies.Add(node);
					node.DependencyFor.Add(n);
				}

				d_unresolved.Remove(state.Object);
			}
			else
			{
				// Nothing depends on us yet, add to the root for now
				d_root.Dependencies.Add(node);
			}
			
			var variable = state.Object as Cdn.Variable;
			HashSet<Cdn.Expression> seen = new HashSet<Expression>();
			
			if (variable != null)
			{
				Resolve(node, variable.Expression, seen);
			}
			
			var instruction = state.Object as Cdn.Instruction;
			
			if (instruction != null)
			{
				Resolve(node, instruction, seen);
			}
		}

		private void AddDependency(Node node, object o)
		{
			Node res;

			if (d_nodeMap.TryGetValue(o, out res))
			{
				// Res might be in root, remove it
				d_root.Dependencies.Remove(res);

				node.Dependencies.Add(res);
			}
			else
			{
				List<Node> lst;
				
				if (!d_unresolved.TryGetValue(o, out lst))
				{
					lst = new List<Node>();
					d_unresolved[o] = lst;
				}
				
				lst.Add(node);
			}
		}

		private void Resolve(Node node, Cdn.Variable variable, HashSet<Cdn.Expression> seen)
		{
			DataTable.DataItem item;

			if (d_states.TryGetValue(variable, out item))
			{
				seen.Add(variable.Expression);
				AddDependency(node, variable);
			}
			else
			{
				Resolve(node, variable.Expression, seen);
			}
		}

		private void Resolve(Node node, Cdn.Expression expression, HashSet<Cdn.Expression> seen)
		{
			if (seen.Contains(expression))
			{
				return;
			}

			foreach (var instruction in expression.Instructions)
			{
				Resolve(node, instruction, seen);
			}

			foreach (var e in expression.Dependencies)
			{
				Resolve(node, e, seen);
			}
		}

		private bool As<T>(object o, out T item)
		{
			if (o is T)
			{
				item = (T)o;
				return true;
			}
			else
			{
				item = default(T);
				return false;
			}
		}

		private void Resolve(Node node, Cdn.Instruction instruction, HashSet<Cdn.Expression> seen)
		{
			InstructionVariable variable;
			InstructionRand rand;
			InstructionCustomOperator cusop;

			if (As(instruction, out variable))
			{
				Resolve(node, variable.Variable, seen);
			}
			else if (As(instruction, out rand))
			{
				AddDependency(node, rand);
			}
			else if (As(instruction, out cusop))
			{
				AddDependency(node, cusop);
			}
		}
	}
}