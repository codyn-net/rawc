using System;

namespace Cpg.RawC
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			OptionParser options = OptionParser.Initialize(args);
			
			foreach (string filename in options.Files)
			{
				Network network = new Network(filename);
				
				network.Generate();
			}
		}
	}
}

