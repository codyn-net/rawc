using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class InitializeDelayHistory : INode
	{
		private DataTable d_history;
		private DelayedState d_state;
		private List<INode> d_deps;
		private Tree.Node d_equation;
		private bool d_ontime;

		public InitializeDelayHistory(DelayedState state, DataTable history, Tree.Node equation, IEnumerable<INode> deps, bool ontime)
		{
			d_state = state;
			d_history = history;
			d_deps = new List<INode>(deps);
			d_equation = equation;
			d_ontime = ontime;
		}

		public DataTable History
		{
			get { return d_history; }
		}

		public DelayedState State
		{
			get { return d_state; }
		}

		public IEnumerable<INode> Dependencies
		{
			get { return d_deps; }
		}

		public Tree.Node Equation
		{
			get { return d_equation; }
		}

		public bool OnTime
		{
			get { return d_ontime; }
		}
	}
}

