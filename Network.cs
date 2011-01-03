using System;

namespace Cpg.RawC
{
	public class Network
	{
		private string d_filename;
		private Cpg.Network d_network;

		public Network(string filename)
		{
			d_filename = filename;
		}
		
		public void Generate()
		{
			LoadNetwork();
			
			// Initialize the knowledge
			Knowledge.Initialize(d_network);

			FindLoops(Knowledge.Instance.States.Integrated);
			FindLoops(Knowledge.Instance.States.Direct);
		}
		
		private void FindLoops(States.State[] states)
		{
		}
		
		private void LoadNetwork()
		{
			try
			{
				d_network = new Cpg.Network(d_filename);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Failed to load network: {0}", e.Message);
				Environment.Exit(1);
			}
			
			CompileError error = new CompileError();

			if (!d_network.Compile(null, error))
			{
				Console.Error.WriteLine("Failed to compile network: {0}", error.Message);
				Environment.Exit(1);
			}
		}
	}
}

