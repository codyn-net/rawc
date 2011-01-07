using System;
using System.IO;
using System.Collections.Generic;

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
			
			List<ExpressionTree.Tree> trees = new List<ExpressionTree.Tree>();
			
			foreach (States.State state in Knowledge.Instance.States)
			{
				ExpressionTree.Tree tree = ExpressionTree.Tree.Create(state);
				trees.Add(tree);
			}
			
			//ExpressionTree.Dot dot = new ExpressionTree.Dot(trees.ToArray());
			//dot.Write(d_filename + ".dot");
			
			/*ExpressionTree.Tree t1 = new ExpressionTree.Tree(1);
			ExpressionTree.Tree t2 = new ExpressionTree.Tree(11);
			ExpressionTree.Node[] nodes = new ExpressionTree.Node[26];
			
			for (int i = 2; i <= 25; ++i)
			{
				nodes[i] = new Cpg.RawC.ExpressionTree.Node((uint)i);
			}
			
			nodes[3].Add(nodes[4]);
			nodes[2].Add(nodes[3]);
			nodes[2].Add(nodes[5]);
			nodes[7].Add(nodes[8]);
			nodes[9].Add(nodes[10]);
			nodes[6].Add(nodes[7]);
			nodes[6].Add(nodes[9]);
			t1.Add(nodes[2]);
			t1.Add(nodes[6]);
			
			nodes[15].Add(nodes[16]);
			nodes[14].Add(nodes[15]);
			nodes[14].Add(nodes[17]);
			nodes[13].Add(nodes[14]);
			nodes[18].Add(nodes[19]);
			nodes[13].Add(nodes[18]);
			nodes[20].Add(nodes[21]);
			nodes[22].Add(nodes[23]);
			nodes[20].Add(nodes[22]);
			nodes[12].Add(nodes[13]);
			nodes[12].Add(nodes[20]);
			nodes[24].Add(nodes[25]);
			t2.Add(nodes[12]);
			t2.Add(nodes[24]);

			foreach (int i in (new int[] {4, 5, 8, 10}))
			{
				nodes[i].IsLeaf = true;
				t1.Leaves.Add(nodes[i]);
			}
			
			foreach (int i in (new int[] {16, 17, 19, 21, 23, 25}))
			{
				nodes[i].IsLeaf = true;
				t2.Leaves.Add(nodes[i]);
			}*/
			
			ExpressionTree.Dot dot = new ExpressionTree.Dot(trees.ToArray());
			dot.Write("lala.dot");
			
			ExpressionTree.Graph graph = new ExpressionTree.Graph(true, trees.ToArray());
			
			List<KeyValuePair<double, ExpressionTree.Node>> scores = new List<KeyValuePair<double, ExpressionTree.Node>>();
			
			foreach (ExpressionTree.Node node in graph.Nodes)
			{
				double score = 0;
				
				foreach (ExpressionTree.Node child in graph.ReverseMapping[node])
				{
					if (child.Descendants > 0)
					{
						score += child.Descendants - 1;
					}
				}

				scores.Add(new KeyValuePair<double, ExpressionTree.Node>(score, node));
			}
			
			scores.Sort((a, b) => b.Key.CompareTo(a.Key));
			
			ExpressionTree.Node root = graph.ReverseMapping[scores[0].Value][0];
			
			dot = new Cpg.RawC.ExpressionTree.Dot(root);
			dot.Write("best.dot");
			
			foreach (KeyValuePair<double, ExpressionTree.Node> pair in scores)
			{
				Console.WriteLine("{0} -> {1}", pair.Key / (pair.Value.Descendants - 1), pair.Value);
			}
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

