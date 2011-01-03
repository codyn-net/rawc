using System;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class LoopFinder
	{
		private List<States.State> d_states;
		private int d_all;

		public LoopFinder(States.State[] states)
		{
			d_states = new List<States.State>(states);
			
			Find();
		}
		
		private bool LoopsConflict(List<Loop> loops, Loop loop)
		{
			foreach (Loop l in loops)
			{
				if (l.ConflictsWith(loop))
				{
					return true;
				}
			}
			
			return false;
		}
		
		private int Cost(List<Loop> loops)
		{
			// Cost of each loop + cost of remaining states
			int cost = 0;
			int num = 0;

			foreach (Loop loop in loops)
			{
				cost += loop.Cost();
				num += loop.Count;
			}

			return cost;
		}
		
		private List<Loop> BestLoops(List<Loop> loops)
		{
			loops.Sort((a, b) => b.Count.CompareTo(a.Count));
			
			int trynext = 0;
			
			List<List<Loop>> s = new List<List<Loop>>();
			
			while (trynext >= 0)
			{
				List<Loop> ret = new List<Loop>();
				ret.Add(loops[trynext]);
				int start = trynext + 1;
				
				trynext = -1;
				
				for (int i = start; i < loops.Count; ++i)
				{
					if (loops[i].Count < 3)
					{
						break;
					}
					
					if (!LoopsConflict(ret, loops[i]))
					{
						ret.Add(loops[i]);
					}
					else if (trynext < 0)
					{
						trynext = i;
					}
				}

				s.Add(loops);
			}
			
			s.Sort((a, b) => Cost(a).CompareTo(Cost(b)));
			
			if (s.Count > 0)
			{
				return s[0];
			}
			else
			{
				return new List<Loop>();
			}
		}
		
		private void Find()
		{
			List<Loop> loops = new List<Loop>();
			int total = 0;
			
			d_all = 0;

			// Collect expressions
			foreach (States.State state in d_states)
			{
				foreach (LinkAction action in state.Actions)
				{
					bool isopt;
					
					Expression e = Expression.Expand(action.Equation);
					Expression epc = new Expression(Expression.Precompute(e, out isopt));

					bool eadded = false;
					bool epcadded = false;
					
					foreach (Loop loop in loops)
					{
						if (loop.Add(state, e))
						{
							eadded = true;
						}
						else if (isopt)
						{
							epcadded |= loop.Add(state, epc);
						}
					}
					
					if (!eadded)
					{
						loops.Add(new Loop(state, e));
					}
					
					if (!epcadded && isopt)
					{
						loops.Add(new Loop(state, epc));
					}

					total += 1;
				}
			}
			
			// Sort loops which cover most expressions
			loops.Sort((a, b) => b.Count.CompareTo(a.Count));
			
			BestLoops(loops);
		}
	}
}

