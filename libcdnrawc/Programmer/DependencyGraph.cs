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

		private class Queue
		{
			private Dictionary<Tree.Embedding, uint> d_embeddingsMap;
			private SortedDictionary<uint, Queue<Node>> d_storage;
			private uint d_nextId;

			public Queue()
			{
				d_embeddingsMap = new Dictionary<Tree.Embedding, uint>();
				d_storage = new SortedDictionary<uint, Queue<Node>>();

				d_nextId = 1;
			}

			public bool Empty
			{
				get { return d_storage.Count == 0; }
			}

			public void Enqueue(Node n)
			{
				uint id;

				if (n.Embedding == null)
				{
					id = 0;
				}
				else if (!d_embeddingsMap.TryGetValue(n.Embedding, out id))
				{
					id = d_nextId;

					d_embeddingsMap[n.Embedding] = id;
					d_nextId++;
				}

				Queue<Node> q;
				if (!d_storage.TryGetValue(id, out q))
				{
					q = new Queue<Node>();
					d_storage[id] = q;
				}

				q.Enqueue(n);
			}

			public Node Dequeue()
			{
				var e = d_storage.GetEnumerator();

				if (!e.MoveNext())
				{
					return null;
				}

				var id = e.Current.Key;
				var q = e.Current.Value;

				var ret = q.Dequeue();

				if (q.Count == 0)
				{
					d_storage.Remove(id);
				}

				return ret;
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

		public void WriteDot(string filename)
		{
			var wr = new System.IO.StreamWriter(filename);
			wr.WriteLine("strict digraph g {");
			wr.WriteLine("\toverlap=scale;");
			wr.WriteLine("\tsplines=true;");

			Queue<Node> q = new Queue<Node>();
			HashSet<Node> processed = new HashSet<Node>();
			q.Enqueue(d_root);

			Dictionary<Tree.Embedding, int> embeddingId = new Dictionary<Tree.Embedding, int>();

			while (q.Count > 0)
			{
				var node = q.Dequeue();
				int eid;

				if (node.Embedding == null)
				{
					eid = 0;
				}
				else if (!embeddingId.TryGetValue(node.Embedding, out eid))
				{
					eid = embeddingId.Count + 1;
					embeddingId[node.Embedding] = eid;
				}

				if (node != d_root)
				{
					wr.Write("\t{0} [label=\"{1} ({2})\"", node.GetHashCode(), node.State.ToString(), eid);

					if ((node.State.Type & State.Flags.Derivative) != 0)
					{
						wr.Write(",shape=box,fillcolor=\"#ffeeff\",style=filled");
					}
					else if ((node.State.Type & State.Flags.Integrated) != 0)
					{
						wr.Write(",shape=diamond,fillcolor=\"#ffffee\",style=filled");
					}

					wr.WriteLine("];");
				}

				foreach (var dep in node.Dependencies)
				{
					if (!processed.Contains(dep))
					{
						processed.Add(dep);
						q.Enqueue(dep);
					}
				}
			}

			foreach (var node in processed)
			{
				foreach (var dep in node.Dependencies)
				{
					wr.WriteLine("\t{0} -> {1};", node.GetHashCode(), dep.GetHashCode());
				}
			}

			wr.WriteLine("}");
			wr.Flush();
			wr.Close();
		}

		public bool DependsOn(State state, object obj)
		{
			Node node;

			if (state.Object == obj)
			{
				// Only constraint states and derivative states can depend on themselves
				return state is ConstraintState || state is DerivativeState;
			}

			if (!d_stateMap.TryGetValue(state, out node))
			{
				return false;
			}

			HashSet<Node> seen = new HashSet<Node>();
			Queue<Node> deps = new Queue<Node>();

			deps.Enqueue(node);

			while (deps.Count > 0)
			{
				var n = deps.Dequeue();

				if (seen.Contains(n))
				{
					continue;
				}

				seen.Add(n);

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
					Add(st, null);
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
			bool checkleaf = false;

			if (node.State != null && states.Contains(node.State))
			{
				Node newnode;

				if (!ret.d_stateMap.TryGetValue(node.State, out newnode))
				{
					newnode = new Node(node.State);

					newnode.Embedding = node.Embedding;
					ret.d_stateMap[node.State] = newnode;
				}

				// Set dependencies
				parent.Dependencies.Add(newnode);
				newnode.DependencyFor.Add(parent);

				// The newnode now becomes the new parent
				parent = newnode;
				checkleaf = true;
			}

			// Don't go deep if node was already seen
			if (seen.Contains(node))
			{
				return;
			}

			seen.Add(node);

			foreach (var dependency in node.Dependencies)
			{
				CollapseNode(ret, dependency, states, parent, seen, leafs);
			}

			if (checkleaf && parent.Dependencies.Count == 0 && parent.State != null)
			{
				leafs.Add(parent);
			}
		}

		private DependencyGraph Collapse(HashSet<State> states, out HashSet<Node> leafs)
		{
			DependencyGraph ret = new DependencyGraph();

			ret.d_states = d_states;
			ret.d_embeddingsMap = d_embeddingsMap;

			// Create a new dependency graph in which only nodes in 'states'
			// appear
			leafs = new HashSet<Node>();

			CollapseNode(ret,
			             d_root,
			             states,
			             ret.d_root,
			             new HashSet<Node>(),
			             leafs);

			return ret;
		}

		public List<DependencyGroup> Sort(HashSet<State> states)
		{
			HashSet<Node> leafs;

			Collapse(states, out leafs);

			var ret = new List<DependencyGroup>();

			var q = new Queue();

			// Add all leaf nodes (nothing depends on them) to the initial set
			foreach (var n in leafs)
			{
				q.Enqueue(n);
			}

			while (!q.Empty)
			{
				var n = q.Dequeue();

				// Append the node to the last group if it has the same
				// embedding
				if (ret.Count != 0 && ret[ret.Count - 1].Embedding == n.Embedding)
				{
					ret[ret.Count - 1].Add(n.State);
				}
				else
				{
					// Otherwise create a new group for it and append the group
					// to the resulting set
					DependencyGroup g = new DependencyGroup(n.Embedding);
					g.Add(n.State);

					ret.Add(g);
				}

				// Iterate over all the nodes (dep) that depend on (n)
				foreach (var dep in n.DependencyFor)
				{
					// Remove the node from its dependencies (it has been
					// processed)
					dep.Dependencies.Remove(n);

					// If this list is now 0, then (dep) does not have any
					// dependencies left and can be added to our queue to be
					// inserted in the result
					if (dep.Dependencies.Count == 0 && dep.State != null)
					{
						q.Enqueue(dep);
					}
				}
			}

			// TODO: check for cyclic dependencies
			return ret;
		}

		public void Add(State state, Dictionary<object, State> mapping)
		{
			Node node = new Node(state);

			d_stateMap[state] = node;
			d_nodeMap[state.DataKey] = node;

			Tree.Embedding embedding;

			if (d_embeddingsMap.TryGetValue(state, out embedding))
			{
				node.Embedding = embedding;
			}

			// Resolve currently unresolved dependencies first
			List<Node> lst;

			if (d_unresolved.TryGetValue(state.DataKey, out lst))
			{
				foreach (var n in lst)
				{
					n.Dependencies.Add(node);
					node.DependencyFor.Add(n);
				}

				d_unresolved.Remove(state.DataKey);
			}
			else
			{
				// Nothing depends on us yet, add to the root for now
				d_root.Dependencies.Add(node);
			}

			// Compute dependencies only when either:
			//
			// 1) State does not represent an integrated state variable
			// 2) State represents an initialization
			// 3) State represents a derivative calculation
			//
			// We do this because some states in the table have a double State
			// associated with it (e.g. one representing initial value and
			// another representing simply the state).
			if ((state.Type & State.Flags.Integrated) == 0 ||
			    (state.Type & State.Flags.Initialization) != 0 ||
			    (state.Type & State.Flags.Derivative) != 0 ||
			    (state.Type & State.Flags.Constraint) != 0)
			{
				var v = state.Object as Cdn.Variable;

				// Additionally check if the variable which the state represents
				// is not an IN or ONCE variable, unless this state is actually
				// representing the intial value computation or derivative
				// computation of that variable
				if (v == null || state is ConstraintState || ((v.Flags & (Cdn.VariableFlags.In | Cdn.VariableFlags.Once)) == 0 || (state.Type & (State.Flags.Initialization | State.Flags.Derivative)) != 0))
				{
					HashSet<Cdn.Expression> seen = new HashSet<Expression>();

					Resolve(node, state.Instructions, seen, mapping);
				}
			}
		}

		private void AddDependency(Node node, object o, State mapped)
		{
			Node res = null;

			if (mapped != null)
			{
				d_stateMap.TryGetValue(mapped, out res);
			}
			else
			{
				d_nodeMap.TryGetValue(o, out res);
			}

			if (res != null)
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

		private void Resolve(Node node, Cdn.Variable variable, HashSet<Cdn.Expression> seen, Dictionary<object, State> mapping)
		{
			State mapped = null;
			DataTable.DataItem item;

			if ((mapping != null && mapping.TryGetValue(variable, out mapped)) ||
			   d_states.TryGetValue(variable, out item))
			{
				seen.Add(variable.Expression);
				AddDependency(node, variable, mapped);
			}
			else
			{
				Resolve(node, variable.Expression, seen, mapping);
			}
		}

		private void Resolve(Node node, IEnumerable<Cdn.Instruction> instructions, HashSet<Cdn.Expression> seen, Dictionary<object, State> mapping)
		{
			foreach (var instruction in instructions)
			{
				Resolve(node, instruction, seen, mapping);
			}
		}

		private void Resolve(Node node, Cdn.Expression expression, HashSet<Cdn.Expression> seen, Dictionary<object, State> mapping)
		{
			if (seen.Contains(expression))
			{
				return;
			}

			Resolve(node, expression.Instructions, seen, mapping);
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

		private void Resolve(Node node, Cdn.Instruction instruction, HashSet<Cdn.Expression> seen, Dictionary<object, State> mapping)
		{
			InstructionVariable variable;
			InstructionRand rand;
			InstructionCustomOperator cusop;

			if (As(instruction, out variable))
			{
				Resolve(node, variable.Variable, seen, mapping);
			}
			else if (As(instruction, out rand))
			{
				AddDependency(node, rand, null);
			}
			else if (As(instruction, out cusop))
			{
				AddDependency(node, cusop, null);
			}
		}
	}
}