using System;

namespace Cdn.RawC
{
	public class EventActionState : State
	{
		private Cdn.EdgeAction d_action;
		private Cdn.Variable d_variable;

		public EventActionState(Cdn.EdgeAction action, Cdn.Variable v) : base(v, action.Equation, State.Flags.EventAction | (action.TargetVariable.HasFlag(VariableFlags.Integrated) ? State.Flags.Derivative : 0))
		{
			d_action = action;
			d_variable = v;
		}

		public Cdn.EdgeAction Action
		{
			get { return d_action; }
		}

		public Cdn.Variable Variable
		{
			get { return d_variable; }
		}

		public override string ToString()
		{
			return String.Format("{0} (ev)", base.ToString());
		}
	}
}

