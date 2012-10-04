using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer
{
	public class DependencyFilter : HashSet<State>
	{
		private DependencyFilter d_not;
		private bool d_filter;

		public DependencyFilter()
		{
			d_filter = false;
		}

		public DependencyFilter(IEnumerable<State> other) : base(other)
		{
			d_filter = false;
		}

		public delegate object ObjectSelector(State state);

		public DependencyFilter DependencyOf(IEnumerable<State> states,
		                                     ObjectSelector selector)
		{
			DependencyFilter ret = (d_filter ? this : new DependencyFilter(this));
			ret.d_not = null;

			ret.RemoveWhere((s) => {
				bool doesdepend = false;
				object obj = (selector == null ? s.Object : selector(s));

				foreach (State other in states)
				{
					if (Knowledge.Instance.DependsOn(other, obj))
					{
						doesdepend = true;
						break;
					}
				}
				
				if (!doesdepend)
				{
					if (ret.d_not == null)
					{
						ret.d_not = new DependencyFilter();
					}

					ret.d_not.Add(s);
				}

				return !doesdepend;
			});
			
			return ret;
		}

		public DependencyFilter DependencyOf(IEnumerable<State> states)
		{
			return DependencyOf(states, null);
		}

		// Returns a set of @this which depends on any state in @states.
		// In addition, @rest will contain the inverse set (i.e. everyting in
		// @this which did _not_ depend on any state in @states).
		public DependencyFilter DependsOn(IEnumerable<State> states,
		                                  ObjectSelector selector)
		{
			DependencyFilter ret = (d_filter ? this : new DependencyFilter(this));
			ret.d_not = null;

			// Check for each state if it depends on a state in states. If so, it should
			// be in the result set, if not it should be in the rest set
			ret.RemoveWhere((s) => {
				bool doesdepend = false;

				foreach (State other in states)
				{
					object obj = (selector == null ? other.Object : selector(other));

					if (Knowledge.Instance.DependsOn(s, obj))
					{
						doesdepend = true;
						break;
					}
				}

				if (!doesdepend)
				{
					if (ret.d_not == null)
					{
						ret.d_not = new DependencyFilter();
					}

					ret.d_not.Add(s);
				}

				return !doesdepend;
			});

			return ret;
		}

		// Returns a set of @this which depends on any state in @states.
		public DependencyFilter DependsOn(IEnumerable<State> states)
		{
			return DependsOn(states, null);
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
					d_not = new DependencyFilter(this);
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
					return new DependencyFilter();
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

