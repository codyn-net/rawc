using System;

namespace Cdn.RawC.Programmer.Computation
{
	public class IncrementDelayedCounters : INode
	{
		private DataTable d_counters;
		private DataTable d_countersSize;

		public IncrementDelayedCounters(DataTable counters, DataTable countersSize)
		{
			d_counters = counters;
			d_countersSize = countersSize;
		}

		public DataTable Counters
		{
			get
			{
				return d_counters;
			}
		}

		public DataTable CountersSize
		{
			get
			{
				return d_countersSize;
			}
		}
	}
}

