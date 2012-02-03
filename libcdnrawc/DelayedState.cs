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
			
			public Key(OperatorDelayed delayed)
			{
				d_delayed = delayed;
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
					return new Size((uint)System.Math.Round(d_delayed.Delay / Options.Instance.FixedTimeStep) + 1);
				}
			}

			public override int GetHashCode()
			{
				Tree.Expression val = new Tree.Expression(d_delayed.Expression);
				string s = val.HashString;
				
				if (d_delayed.InitialValue != null)
				{
					val = new Cdn.RawC.Tree.Expression(d_delayed.InitialValue);
	
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
				
				return d_delayed.Equal(other.d_delayed);
			}
		}

		private OperatorDelayed d_delayed;
		private Size d_size;
		
		public DelayedState(OperatorDelayed delayed) : this(delayed, Flags.None)
		{
		}

		public DelayedState(OperatorDelayed delayed, Flags type) : base(delayed.Expression, delayed.InitialValue, Flags.Delayed | type)
		{
			d_delayed = delayed;
			
			d_size = new Size((uint)System.Math.Round(d_delayed.Delay / Options.Instance.FixedTimeStep) + 1);
		}
		
		public double Delay
		{
			get
			{
				return d_delayed.Delay;
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
				return d_delayed;
			}
		}
	}
}

