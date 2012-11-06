using System;

namespace Cdn.RawC
{
	public class EventActionState : State
	{
		public EventActionState(Cdn.EdgeAction action, Cdn.Variable v) : base(v, action.Equation, State.Flags.EventAction | State.Flags.Derivative)
		{
		}

		public override string ToString()
		{
			return String.Format("{0} (ev)", base.ToString());
		}
	}
}

