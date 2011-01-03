using System;

namespace Cpg.RawC
{
	public class Knowledge
	{
		private static Knowledge s_instance;
		private States d_states;
		private Cpg.Network d_network;

		public static Knowledge Initialize(Cpg.Network network)
		{
			s_instance = new Knowledge(network);
			return s_instance;
		}
		
		public static Knowledge Instance
		{
			get
			{
				return s_instance;
			}
		}

		public Knowledge(Cpg.Network network)
		{
			d_network = network;
			
			d_states = new States(d_network);
		}
		
		public bool IsVariadic(Cpg.Expression expression)
		{
			// See if the expression is variadic. An expression is variadic if it depends on a variadic operator/function
			// or if it depends on a persistent property
			foreach (Instruction inst in expression.Instructions)
			{
				if (inst is InstructionVariadicFunction)
				{
					return true;
				}
			}
			
			foreach (Property property in expression.Dependencies)
			{
				if (IsVariadic(property))
				{
					return true;
				}
			}
			
			return false;
		}
		
		public bool IsVariadic(Property property)
		{
			return IsPersist(property) || IsVariadic(property.Expression);
		}
		
		public bool IsPersist(Property property)
		{
			// A property is persistent (i.e. needs a persistent storage) if:
			//
			// 1) it is a state (has links that act on it)
			// 2) is either IN or OUT
			// 3) is ONCE and variadic (meaning it needs separate initialization)
			States.State state = d_states.FromProperty(property);
			
			if (state != null)
			{
				return false;
			}
			
			if ((property.Flags & (PropertyFlags.In | PropertyFlags.Out)) != PropertyFlags.None)
			{
				return true;
			}
			
			if ((property.Flags & PropertyFlags.Once) != PropertyFlags.None)
			{
				return IsVariadic(property.Expression);
			}
			
			return false;
		}
		
		public States States
		{
			get
			{
				return d_states;
			}
		}
	}
}

