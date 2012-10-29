using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class DependencyFilter : HashSet<State>
	{
		private DependencyGraph d_graph;
		private DependencyFilter d_not;
		private bool d_filter;

		public DependencyFilter(DependencyGraph graph)
		{
			d_graph = graph;
			d_filter = false;
		}

		public DependencyFilter(DependencyGraph graph, IEnumerable<State> other) : base(other)
		{
			d_graph = graph;
			d_filter = false;
		}

		public void AddRange(IEnumerable<State> range)
		{
			foreach (State s in range)
			{
				Add(s);
			}
		}

		public DependencyFilter DependencyOf(IEnumerable<State> states)
		{
			DependencyFilter ret = (d_filter ? this : new DependencyFilter(d_graph, this));
			ret.d_not = null;

			ret.RemoveWhere((s) => {
				bool doesdepend = false;
				object obj = s.Object;

				foreach (State other in states)
				{
					if (d_graph.DependsOn(other, obj))
					{
						doesdepend = true;
						break;
					}
				}
				
				if (!doesdepend)
				{
					if (ret.d_not == null)
					{
						ret.d_not = new DependencyFilter(d_graph);
					}

					ret.d_not.Add(s);
				}

				return !doesdepend;
			});
			
			return ret;
		}

		// Returns a set of @this which depends on any state in @states.
		// In addition, @rest will contain the inverse set (i.e. everyting in
		// @this which did _not_ depend on any state in @states).
		public DependencyFilter DependsOn(IEnumerable<State> states)
		{
			DependencyFilter ret = (d_filter ? this : new DependencyFilter(d_graph, this));
			ret.d_not = null;

			// Check for each state if it depends on a state in states. If so, it should
			// be in the result set, if not it should be in the rest set
			ret.RemoveWhere((s) => {
				bool doesdepend = false;

				foreach (State other in states)
				{
					object obj = other.Object;

					if (d_graph.DependsOn(s, obj))
					{
						doesdepend = true;
						break;
					}
				}

				if (!doesdepend)
				{
					if (ret.d_not == null)
					{
						ret.d_not = new DependencyFilter(d_graph);
					}

					ret.d_not.Add(s);
				}

				return !doesdepend;
			});

			return ret;
		}

		public DependencyFilter Not()
		{
			if (d_filter)
			{
				var n = d_not;

				if (Count == 0)
				{
					d_not = null;
				}
				else
				{
					d_not = new DependencyFilter(d_graph, this);
				}

				Clear();

				if (n != null)
				{
					UnionWith(n);
				}

				return this;
			}
			else
			{
				if (d_not != null)
				{
					return d_not;
				}
				else
				{
					return new DependencyFilter(d_graph);
				}
			}
		}

		public DependencyFilter Filter()
		{
			d_filter = true;
			return this;
		}

		public DependencyFilter Unfilter()
		{
			d_filter = false;
			return this;
		}
	}
}

