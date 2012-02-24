using System;

namespace Cdn.RawC
{
	public class DelayedState : State
	{
		public class Size
		{
			private uint d_size;
			
			public Size(uint s)
			{
				d_size = s;
			}
			
			public static implicit operator uint(Size s)
			{
				return s.d_size;
			}
			
			public override int GetHashCode()
			{
				return d_size.GetHashCode();
			}
			
			public override bool Equals(object obj)
			{
				if (obj == null)
				{
					return false;
				}
				
				Size other = obj as Size;
				
				if (other == null)
				{
					return false;
				}
				
				return d_size.Equals(other.d_size);
			}
		}
		
		public class Key
		{
			private OperatorDelayed d_delayed;
			private double d_delay;
			
			public Key(OperatorDelayed delayed, double delay)
			{
				d_delayed = delayed;
				d_delay = delay;
			}
			
			public OperatorDelayed Operator
			{
				get
				{
					return d_delayed;
				}
			}
			
			public DelayedState.Size Size
			{
				get
				{
					return new Size((uint)System.Math.Round(d_delay / Options.Instance.DelayTimeStep) + 1);
				}
			}

			public override int GetHashCode()
			{
				Tree.Expression val = new Tree.Expression(d_delayed.Expression, true);
				string s = val.HashString;
				
				if (d_delayed.InitialValue != null)
				{
					val = new Cdn.RawC.Tree.Expression(d_delayed.InitialValue, true);
	
					s += ", " + val.HashString;
				}
				
				return s.GetHashCode();
			}
			
			public override bool Equals(object obj)
			{
				if (obj == null)
				{
					return false;
				}
				
				Key other = obj as Key;
				
				if (other == null)
				{
					return false;
				}
				
				return d_delayed.Equal(other.d_delayed, false);
			}
		}

		private InstructionCustomOperator d_delayed;
		private Size d_size;
		private double d_delay;
		
		public DelayedState(InstructionCustomOperator delayed, double delay) : this(delayed, delay, Flags.None)
		{
		}

		public DelayedState(InstructionCustomOperator delayed, double delay, Flags type) : base(delayed, (type & Flags.Initialization) != 0 ? ((OperatorDelayed)delayed.Operator).InitialValue : (Cdn.Expression)null, type)
		{
			d_delayed = delayed;
			d_delay = delay;
			
			d_size = new Size((uint)System.Math.Round(d_delay / Options.Instance.DelayTimeStep) + 1);
		}

		public override object DataKey
		{
			get
			{
				return new Key(Operator, Delay);
			}
		}
		
		public double Delay
		{
			get
			{
				return d_delay;
			}
		}
		
		public Size Count
		{
			get
			{
				return d_size;
			}
		}
		
		public OperatorDelayed Operator
		{
			get
			{
				return d_delayed.Operator as OperatorDelayed;
			}
		}
	}
}

