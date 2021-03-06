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
			private Knowledge.EventStateGroup d_eventStateGroup;
			private bool d_eventStateGroupComputed;
			public uint Label;

			public Node(State state)
			{
				State = state;
				Dependencies = new HashSet<Node>();
				DependencyFor = new HashSet<Node>();
			}

			public Knowledge.EventStateGroup EventStateGroup
			{
				get
				{
					if (!d_eventStateGroupComputed)
					{
						var e = State as EventActionState;

						if (e == null)
						{
							d_eventStateGroup = null;
						}
						else
						{
							d_eventStateGroup = Knowledge.Instance.EventStateToGroup[e];
						}

						d_eventStateGroupComputed = true;
					}

					return d_eventStateGroup;
				}

				set
				{
					d_eventStateGroup = value;
					d_eventStateGroupComputed = true;
				}
			}
		}

		private class Queue
		{
			private Dictionary<Knowledge.EventStateGroup, uint> d_eventStateMap;
			private Dictionary<Tree.Embedding, uint> d_embeddingsMap;
			private SortedDictionary<uint, SortedDictionary<uint, Queue<Node>>> d_storage;
			private uint d_nextId;
			private uint d_nextEvId;

			public Queue()
			{
				d_embeddingsMap = new Dictionary<Tree.Embedding, uint>();
				d_eventStateMap = new Dictionary<Knowledge.EventStateGroup, uint>();
				d_storage = new SortedDictionary<uint, SortedDictionary<uint, Queue<Node>>>();

				d_nextId = 0;
				d_nextEvId = 1;
			}

			public bool Empty
			{
				get { return d_storage.Count == 0; }
			}

			public void Enqueue(Node n)
			{
				uint evid;
				uint id;

				if (n.EventStateGroup == null)
				{
					evid = 0;
				}
				else if (!d_eventStateMap.TryGetValue(n.EventStateGroup, out evid))
				{
					evid = d_nextEvId;

					d_eventStateMap[n.EventStateGroup] = evid;
					d_nextEvId++;
				}

				SortedDictionary<uint, Queue<Node>> embeddingStorage;

				if (!d_storage.TryGetValue(evid, out embeddingStorage))
				{
					embeddingStorage = new SortedDictionary<uint, Queue<Node>>();
					d_storage[evid] = embeddingStorage;
				}

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
				if (!embeddingStorage.TryGetValue(id, out q))
				{
					q = new Queue<Node>();
					embeddingStorage[id] = q;
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
				var storage = e.Current.Value;

				var ee = storage.GetEnumerator();

				ee.MoveNext();

				var qid = ee.Current.Key;
				var q = ee.Current.Value;

				var ret = q.Dequeue();

				if (q.Count == 0)
				{
					storage.Remove(qid);
				}

				if (storage.Count == 0)
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
		private Dictionary<object, HashSet<Node>> d_objectToNodeMap;
		private Dictionary<State, Node> d_stateMap;
		private Dictionary<State, Tree.Embedding> d_embeddingsMap;
		private bool d_labeled;
		private Dictionary<uint, HashSet<uint>> d_reachablePairs;
		private uint d_labeler;

		private DependencyGraph()
		{
			d_root = new Node(null);

			d_unresolved = new Dictionary<object, List<Node>>();
			d_nodeMap = new Dictionary<object, Node>();
			d_objectToNodeMap = new Dictionary<object, HashSet<Node>>();
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

		private ulong LabelDependencyId(uint p, uint l)
		{
			return ((ulong)p) << 32 | (ulong)l;
		}

		private void Label(Node n, LinkedList<uint> parents)
		{
			if (n.Label != 0)
			{
				// Everything reachable by n is also reachable by its parents
				foreach (var p in parents)
				{
					d_reachablePairs[p].Add(n.Label);

					foreach (var pair in d_reachablePairs[n.Label])
					{
						d_reachablePairs[p].Add(pair);
					}
				}

				return;
			}

			n.Label = d_labeler++;
			d_reachablePairs[n.Label] = new HashSet<uint>();

			foreach (var p in parents)
			{
				d_reachablePairs[p].Add(n.Label);
			}

			if (n.Label != 0)
			{
				parents.AddLast(n.Label);
			}

			foreach (var dep in n.Dependencies)
			{
				Label(dep, parents);
			}

			if (n.Label != 0)
			{
				parents.RemoveLast();
			}
		}

		private void Label()
		{
			d_labeled = true;
			d_reachablePairs = new Dictionary<uint, HashSet<uint>>();
			d_labeler = 0;

			var l = new LinkedList<uint>();

			Label(d_root, l);
		}

		public bool DependsOn(State state, object obj)
		{
			if (!d_labeled)
			{
				Label();
			}

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

			HashSet<Node> others;

			if (!d_objectToNodeMap.TryGetValue(obj, out others))
			{
				return false;
			}

			var pairs = d_reachablePairs[node.Label];

			foreach (var o in others)
			{
				if (pairs.Contains(o.Label))
				{
					return true;
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

		private void AddObjectToNode(Node n, object o)
		{
			if (o == null)
			{
				return;
			}

			HashSet<Node> others;

			if (!d_objectToNodeMap.TryGetValue(o, out others))
			{
				others = new HashSet<Node>();
				d_objectToNodeMap[o] = others;
			}

			others.Add(n);
		}

		private void CollapseNode(DependencyGraph ret,
		                          Node node,
		                          HashSet<State> states,
		                          Node parent,
		                          HashSet<Node> seen,
		                          HashSet<Node> leafs,
		                          HashSet<Node> evgroups)
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

					ret.AddObjectToNode(newnode, node.State.Object);
					ret.AddObjectToNode(newnode, node.State.DataKey);
				}

				// Set dependencies
				parent.Dependencies.Add(newnode);
				newnode.DependencyFor.Add(parent);

				if (node.EventStateGroup != null)
				{
					evgroups.Add(newnode);
				}

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
				CollapseNode(ret, dependency, states, parent, seen, leafs, evgroups);
			}

			if (checkleaf && parent.Dependencies.Count == 0 && parent.State != null)
			{
				leafs.Add(parent);
			}
		}

		public DependencyGraph Collapse(HashSet<State> states)
		{
			HashSet<Node> leafs;

			return Collapse(states, out leafs);
		}

		private void CollapseEventStateGroups(HashSet<Node> evgroups)
		{
			foreach (var node in evgroups)
			{
				var ingrp = new HashSet<Node>();
				var q = new Queue<Node>();

				q.Enqueue(node);
				ingrp.Add(node);

				while (q.Count != 0)
				{
					var nn = q.Dequeue();

					bool ok = true;

					if (nn != node)
					{
						foreach (var dep in nn.DependencyFor)
						{
							if (!ingrp.Contains(dep))
							{
								ingrp.Remove(dep);
								ok = false;
								break;
							}
						}
					}

					if (!ok)
					{
						continue;
					}

					nn.EventStateGroup = node.EventStateGroup;

					foreach (var n in nn.Dependencies)
					{
						if ((n.EventStateGroup == null || n.EventStateGroup == node.EventStateGroup) && !ingrp.Contains(n) && (n.State.Type & (State.Flags.Promoted | State.Flags.EventAction)) != 0)
						{
							q.Enqueue(n);
							ingrp.Add(n);
						}
					}
				}
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

			var evgroups = new HashSet<Node>();

			CollapseNode(ret,
			             d_root,
			             states,
			             ret.d_root,
			             new HashSet<Node>(),
			             leafs,
			             evgroups);

			ret.CollapseEventStateGroups(evgroups);
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
				if (ret.Count != 0 && ret[ret.Count - 1].Embedding == n.Embedding && ret[ret.Count - 1].EventStateGroup == n.EventStateGroup)
				{
					ret[ret.Count - 1].Add(n.State);
				}
				else
				{
					// Otherwise create a new group for it and append the group
					// to the resulting set
					DependencyGroup g = new DependencyGroup(n.Embedding, n.EventStateGroup);
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

		private void Unlabel()
		{
			Queue<Node> q = new Queue<Node>();
			q.Enqueue(d_root);

			while (q.Count > 0)
			{
				var n = q.Dequeue();
				n.Label = 0;

				foreach (var dep in n.Dependencies)
				{
					q.Enqueue(dep);
				}
			}

			d_labeled = false;
		}

		public void Add(State state, Dictionary<object, State> mapping)
		{
			if (d_labeled)
			{
				Unlabel();
			}

			Node node = new Node(state);

			d_stateMap[state] = node;

			AddObjectToNode(node, state.DataKey);
			AddObjectToNode(node, state.Object);

			if (!(state is EventSetState))
			{
				d_nodeMap[state.DataKey] = node;
			}

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
			// 4) State represents an event set
			//
			// We do this because some states in the table have a double State
			// associated with it (e.g. one representing initial value and
			// another representing simply the state).
			if ((state.Type & State.Flags.Integrated) == 0 ||
			    (state.Type & State.Flags.Initialization) != 0 ||
			    (state.Type & State.Flags.Derivative) != 0 ||
			    (state.Type & State.Flags.Constraint) != 0 ||
			    (state.Type & State.Flags.EventSet) != 0)
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
			InstructionCustomFunction cusfn;

			if (As(instruction, out variable))
			{
				Resolve(node, variable.Variable, seen, mapping);
			}
			else if (As(instruction, out rand))
			{
				AddDependency(node, rand, null);
			}
			else if (As(instruction, out cusfn))
			{
				Resolve(node, cusfn.Function.Expression, seen, mapping);
			}
			else if (As(instruction, out cusop))
			{
				var fn = cusop.Operator.PrimaryFunction;

				if (fn != null)
				{
					Resolve(node, fn.Expression, seen, mapping);
				}

				AddDependency(node, cusop, null);
			}
		}
	}
}
