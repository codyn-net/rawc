using System;

namespace Cdn.RawC.Programmer
{
	public class Options
	{
		/* The network */
		public Network Network;

		/* Output directory */
		public string Output;

		/* Original output directory */
		public string OriginalOutput;
		
		/* Output file basename */
		public string Basename;
		
		public double DelayTimeStep;

		public bool Validate;
	}
}

