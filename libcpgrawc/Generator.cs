using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cpg.RawC
{
	public class Generator
	{
		private string d_filename;
		private Cpg.Network d_network;
		private string[] d_writtenFiles;

		public Generator(string filename)
		{
			d_filename = filename;
		}
		
		public void Generate()
		{
			Log.WriteLine("Generating code for network...");
			
			LoadNetwork();
			
			// Initialize the knowledge
			Knowledge.Initialize(d_network);
			
			// Collect all the equations
			Tree.Collectors.Result collection = Collect();
			
			// Filter conflicts and resolve final embeddings
			Tree.Embedding[] embeddings = Filter(collection);
			
			// Resolve final equations
			Dictionary<State, Tree.Node> equations = ResolveEquations(embeddings);
			
			// Create program
			Programmer.Program program = new Programmer.Program(ProgrammerOptions(), embeddings, equations);
			
			// Write program
			d_writtenFiles = Options.Instance.Formatter.Write(program);
			
			if (Options.Instance.PrintCompileSource)
			{
				Console.WriteLine(Options.Instance.Formatter.CompileSource());
			}
			if (Options.Instance.Validate)
			{
				Validate();
			}
			else if (Options.Instance.Compile != null)
			{
				Log.WriteLine("Compiling code...");
				Options.Instance.Formatter.Compile(Options.Instance.Compile, Options.Instance.Verbose);
			}
		}
		
		private void Validate()
		{
			Log.WriteLine("Validating generated network...");

			string tmpprog = Path.GetTempFileName();
			
			Options opts = Options.Instance;
			opts.Formatter.Compile(tmpprog, opts.Verbose);
			
			Process process = new Process();
			process.StartInfo.FileName = tmpprog;
			process.StartInfo.Arguments = String.Format("{0} {1} {2}", opts.ValidateRange[0], opts.ValidateRange[1], opts.ValidateRange[2]);
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.UseShellExecute = false;

			process.Start();
			
			process.StandardInput.WriteLine("t");
			List<Cpg.Monitor> monitors = new List<Cpg.Monitor>();
			
			monitors.Add(new Cpg.Monitor(Knowledge.Instance.Network, Knowledge.Instance.Network.Integrator.Property("t")));
			
			foreach (State state in Knowledge.Instance.IntegratedStates)
			{
				process.StandardInput.WriteLine(state.Property.FullName);
				monitors.Add(new Cpg.Monitor(Knowledge.Instance.Network, state.Property));
			}
			
			process.StandardInput.Close();

			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			
			List<List<double>> data = new List<List<double>>();

			string[] lines = output.Split('\n');
			
			foreach (string line in lines)
			{
				if (String.IsNullOrEmpty(line))
				{
					continue;
				}

				try
				{
					data.Add(new List<double>(Array.ConvertAll<string, double>(line.Split('\t'), a => Double.Parse(a))));
				}
				catch
				{
					Console.Error.WriteLine("Could not parse number:");
					Console.Error.WriteLine(line);
					
					Environment.Exit(1);
				}
			}
			
			// Now simulate network internally also
			Knowledge.Instance.Network.Run(opts.ValidateRange[0], opts.ValidateRange[1], opts.ValidateRange[2]);
			
			// Compare values
			List<double[]> monitored = new List<double[]>();
			
			for (int i = 0; i < monitors.Count; ++i)
			{
				monitored.Add(monitors[i].GetData());
			}

			for (int i = 0; i < data.Count; ++i)
			{
				List<double> raw = data[i];
				
				for (int j = 0; j < raw.Count; ++j)
				{
					if (System.Math.Abs(monitored[j][i] - raw[j]) > opts.ValidatePrecision)
					{
						Console.Error.WriteLine("Discrepancy detected at t = {0} in {1} (got {2} but expected {3})",
						                        opts.ValidateRange[0] + (i * opts.ValidateRange[1]),
						                        monitors[j].Property.FullName,
						                        raw[j],
						                        monitored[j][i]);
						Environment.Exit(1);
					}
				}
			}
			
			Log.WriteLine("Network {0} successfully validated...", d_network.Filename);
		}
		
		public string[] WrittenFiles
		{
			get
			{
				return d_writtenFiles;
			}
		}
		
		private Programmer.Options ProgrammerOptions()
		{
			Programmer.Options ret = new Programmer.Options();
			
			ret.Network = d_network;
			ret.Basename = Options.Instance.Basename;
			ret.Output = Options.Instance.Output;
			
			if (String.IsNullOrEmpty(ret.Basename))
			{
				ret.Basename = Path.GetFileNameWithoutExtension(ret.Network.Filename);
			}
			
			if (String.IsNullOrEmpty(ret.Output))
			{
				ret.Output = Path.GetDirectoryName(ret.Network.Filename);
			}
			
			return ret;
		}
		
		private void ResolveState(State state, Tree.Embedding[] embeddings, Dictionary<State, List<Tree.Node>> mapping, Dictionary<State, Tree.Node> ret)
		{
			List<Tree.Node> instances;
			Tree.Node node = Tree.Node.Create(state);

			if (mapping.TryGetValue(state, out instances))
			{
				// Otherwise, merge all the embeddings into the equation
				instances.Sort(SortDeepestFirst);
			
				foreach (Tree.Node instance in instances)
				{
					Tree.NodePath path = instance.Path;
					
					if (path.Count == 0)
					{
						node = instance;
						break;
					}
					else
					{
						node.Replace(path, instance);
					}
				}
			}

			ret[state] = node;
		}
		
		private Dictionary<State, Tree.Node> ResolveEquations(Tree.Embedding[] embeddings)
		{
			// Create a map from state to all non-conflicting embedding instances for that state
			Dictionary<State, List<Tree.Node>> mapping;			
			mapping = Collect(embeddings);
			
			Dictionary<State, Tree.Node> ret = new Dictionary<State, Tree.Node>();
			
			foreach (State state in Knowledge.Instance.States)
			{
				ResolveState(state, embeddings, mapping, ret);
			}
			
			return ret;
		}
		
		private int SortDeepestFirst(Tree.Node a, Tree.Node b)
		{
			if (a.Path.Count == 0)
			{
				return 1;
			}
			else if (b.Path.Count == 0)
			{
				return -1;
			}
			else
			{
				return b.Path.Peek().CompareTo(a.Path.Peek());
			}
		}
		
		private Dictionary<State, List<Tree.Node>> Collect(Tree.Embedding[] embeddings)
		{
			Dictionary<State, List<Tree.Node>> mapping = new Dictionary<State, List<Tree.Node>>();
			
			foreach (Tree.Embedding embedding in embeddings)
			{
				foreach (Tree.Node instance in embedding.Instances)
				{
					List<Tree.Node> inst;

					if (!mapping.TryGetValue(instance.State, out inst))
					{
						inst = new List<Tree.Node>();
						mapping[instance.State] = inst;
					}
					
					inst.Add(instance);
				}
			}
			
			return mapping;
		}
		
		private void LoadNetwork()
		{
			try
			{
				d_network = new Cpg.Network(d_filename);
			}
			catch (Exception e)
			{
				throw new Exception(String.Format("Failed to load network: {0}", e.Message));
			}
			
			CompileError error = new CompileError();

			if (!d_network.Compile(null, error))
			{
				throw new Exception(String.Format("Failed to compile network: {0}", error.Message));
			}
		}
		
		private Tree.Collectors.Result Collect()
		{
			if (Options.Instance.NoEmbeddings)
			{
				return new Tree.Collectors.Result();
			}

			Options parser = Options.Instance;
			Tree.Collectors.ICollector collector;
			
			if (parser.Collector != null)
			{
				Plugins.Plugins plugins = Plugins.Plugins.Instance;
				Type type = plugins.Find(typeof(Tree.Collectors.ICollector), parser.Collector);
				
				if (type == null)
				{
					throw new Exception(String.Format("The collector `{0}' could not be found...", parser.Collector));
				}
				
				collector = (Tree.Collectors.ICollector)type.GetConstructor(new Type[] {}).Invoke(new object[] {});
			}
			else
			{
				collector = new Tree.Collectors.Default();
			}
			
			List<Tree.Node> forest = new List<Tree.Node>();
			
			foreach (State state in Knowledge.Instance.States)
			{
				Tree.Node tree = Tree.Node.Create(state);
				forest.Add(tree);
			}
			
			return collector.Collect(forest.ToArray());
		}
		
		private Tree.Embedding[] Filter(Tree.Collectors.Result collection)
		{
			Options parser = Options.Instance;
			Tree.Filters.IFilter filter;
			
			if (parser.Filter != null)
			{
				Plugins.Plugins plugins = Plugins.Plugins.Instance;
				Type type = plugins.Find(typeof(Tree.Filters.IFilter), parser.Filter);
				
				if (type == null)
				{
					throw new Exception(String.Format("The filter `{0}' could not be found...", parser.Filter));
				}
				
				filter = (Tree.Filters.IFilter)type.GetConstructor(new Type[] {}).Invoke(new object[] {});
			}
			else
			{
				filter = new Tree.Filters.Default();
			}

			return filter.Filter(collection.Prototypes);
		}
	}
}

