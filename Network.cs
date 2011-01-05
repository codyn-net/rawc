using System;
using System.IO;

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

			//FindLoops(Knowledge.Instance.States.Integrated);
			//FindLoops(Knowledge.Instance.States.Direct);
			
			ExpressionTree.Tree tree = new ExpressionTree.Tree();
			
			foreach (States.State state in Knowledge.Instance.States)
			{
				tree.Add(state);
			}
			
			FileStream stream = new FileStream(d_filename + ".dot", FileMode.Create);
			StreamWriter writer = new StreamWriter(stream);
			
			tree.Dot(writer);
			
			writer.Flush();
			writer.Close();
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

