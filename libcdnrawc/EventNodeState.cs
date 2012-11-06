using System;

namespace Cdn.RawC
{
	public class EventNodeState : State
	{
		public enum StateType
		{
			Previous = 1,
			Current = 2,
			Distance = 3,
		}

		private StateType d_type;
		private Cdn.EventLogicalNode d_node;
		private Cdn.Event d_event;

		public EventNodeState(Cdn.Event ev, Cdn.EventLogicalNode node, StateType type) : base(node, (Cdn.Expression)(type == StateType.Current ? node.Expression : null), State.Flags.EventNode)
		{
			d_type = type;
			d_node = node;
			d_event = ev;
		}

		public Cdn.Event Event
		{
			get { return d_event; }
		}

		public Cdn.EventLogicalNode Node
		{
			get { return d_node; }
		}

		new public StateType Type
		{
			get { return d_type; }
		}

		public override object DataKey
		{
			get { return string.Format("{0}@{1}", ((Cdn.EventLogicalNode)Object).Handle, d_type); }
		}

		public override string ToString()
		{
			return String.Format("{0} (evn)", base.ToString());
		}
	}
}
