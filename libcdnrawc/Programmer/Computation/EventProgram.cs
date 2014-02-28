using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Computation
{
	public class EventProgram : INode
	{
		public Block Dependencies;
		public Block SetStates;
		public Block PostCompute;

		public EventProgram()
		{
			Dependencies = new Block();
			SetStates = new Block();
			PostCompute = new Block();
		}
	}
}

