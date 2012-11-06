using System;

namespace Cdn.RawC
{
	public class EventSetState : State
	{
		private Cdn.EventSetVariable d_setvar;

		public EventSetState(Cdn.EventSetVariable v) : base(v.Variable, v.Value, State.Flags.EventSet)
		{
			d_setvar = v;
		}

		public Cdn.EventSetVariable SetVariable
		{
			get { return d_setvar; }
		}

		public override string ToString()
		{
			return String.Format("{0} (ev-set)", base.ToString());
		}
	}
}

