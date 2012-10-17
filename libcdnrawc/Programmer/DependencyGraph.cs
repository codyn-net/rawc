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
				return false;
			}

			if (!d_stateMap.TryGetValue(state, out node))
			{
				return false;
			}

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

			if (checkleaf && parent.Dependencies.Count == 0)
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

		public DependencyGroup Sort(HashSet<State> states)
		{
			HashSet<Node> leafs;

			var collapsed = Collapse(states, out leafs);

			var ret = new DependencyGroup(null);
			var seen = new Dictionary<Node, DependencyGroup>();

			seen[collapsed.d_root] = null;

			// Go from all the leafs upwards
			foreach (Node leaf in leafs)
			{
				ComputeNodeInGroups(leaf, ret, seen);
			}

			foreach (var grp in ret)
			{
				grp.Sort((a, b) => {
					if (a == b)
					{
						return 0;
					}

					if (DependsOn(a, b.DataKey))
					{
						return 1;
					}
					else if (DependsOn(b, a.DataKey))
					{
						return -1;
					}
					else
					{
						return 0;
					}
				});
			}

			return ret;
		}

		private void ComputeNodeInGroups(Node node,
		                                 DependencyGroup grp,
		                                 Dictionary<Node, DependencyGroup> seen)
		{
			if (seen.ContainsKey(node))
			{
				return;
			}

			var root = grp;

			// Find right-most dependency
			foreach (Node dep in node.Dependencies)
			{
				DependencyGroup depgrp;

				if (!seen.TryGetValue(dep, out depgrp))
				{
					continue;
				}

				if (depgrp.Id > root.Id)
				{
					root = depgrp;
				}
			}

			DependencyGroup before = null;

			// Find left-most dependency if needed
			foreach (Node dep in node.DependencyFor)
			{
				DependencyGroup depgrp;

				if (!seen.TryGetValue(dep, out depgrp))
				{
					continue;
				}

				if (before == null || depgrp.Id < before.Id)
				{
					before = depgrp;
				}
			}

			// Need to add between 'root' and 'before'
			while (root.Embedding != node.Embedding && root.StatesCount > 0)
			{
				var next = root.Next;

				if (next == before)
				{
					// Insert new empty group for the node
					root.Next = new DependencyGroup(node.Embedding);
					root = root.Next;
					break;
				}
				else
				{
					root = next;
				}
			}

			if (root.StatesCount == 0)
			{
				root.Embedding = node.Embedding;
			}

			root.Add(node.State);
			seen[node] = root;

			// Recursively go up, i.e. nodes that depend on 'node'.
			foreach (Node dep in node.DependencyFor)
			{
				ComputeNodeInGroups(dep, root, seen);
			}
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
			    (state.Type & State.Flags.Derivative) != 0)
			{
				var v = state.Object as Cdn.Variable;

				// Additionally check if the variable which the state represents
				// is not an IN or ONCE variable, unless this state is actually
				// representing the intial value computation of that variable
				if (v == null || ((v.Flags & (Cdn.VariableFlags.In | Cdn.VariableFlags.Once)) == 0 || (state.Type & State.Flags.Initialization) != 0))
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