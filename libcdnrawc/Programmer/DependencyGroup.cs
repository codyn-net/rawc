using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class DependencyGroup : IEnumerable<DependencyGroup>
	{
		private Tree.Embedding d_embedding;
		private List<State> d_states;
		private DependencyGroup d_previous;
		private DependencyGroup d_next;
		private uint d_id;
		
		public DependencyGroup(Tree.Embedding embedding)
		{
			d_embedding = embedding;
			d_states = new List<State>();
			d_id = 0;
		}
		
		public IEnumerator<DependencyGroup> GetEnumerator()
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
				d_next.UpdateIdForwards();
			}
		}
		
		public Tree.Embedding Embedding
		{
			get { return d_embedding; }
			set { d_embedding = value; }
		}
		
		public IEnumerable<State> States
		{
			get { return d_states; }
		}
		
		public int StatesCount
		{
			get { return d_states.Count; }
		}
		
		public DependencyGroup Previous
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

		public DependencyGroup Last
		{
			get
			{
				var ret = this;

				while (ret.d_next != null)
				{
					ret = ret.d_next;
				}

				return ret;
			}
		}

		public DependencyGroup First
		{
			get
			{
				var ret = this;

				while (ret.d_previous != null)
				{
					ret = ret.d_previous;
				}

				return ret;
			}
		}

		public DependencyGroup Next
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

		public void Sort(Comparison<State> comparer)
		{
			d_states.Sort(comparer);
		}
		
		public void Add(State state)
		{
			d_states.Add(state);
		}
	}
}

