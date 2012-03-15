using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cdn.RawC
{
	public class Generator
	{
		private string d_filename;
		private Cdn.Network d_network;
		private string[] d_writtenFiles;
		private List<Cdn.Monitor> d_monitors;
		private List<double[] > d_monitored;

		public Generator(string filename)
		{
			d_filename = filename;
		}
		
		public void Generate()
		{
			Log.WriteLine("Generating code for network...");
			
			LoadNetwork();

			if (Options.Instance.Validate)
			{
				StaticRandExpressions();

				d_network.Compiled += delegate {
					StaticRandExpressions();
				};

				// Run the network now, with the static rands
				d_monitors = new List<Cdn.Monitor>();

				d_monitors.Add(new Cdn.Monitor(d_network, d_network.Integrator.Variable("t")));

				Knowledge.Initialize(d_network);

				foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Integrated))
				{
					d_monitors.Add(new Cdn.Monitor(d_network, v));
				}

				foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Out))
				{
					d_monitors.Add(new Cdn.Monitor(d_network, v));
				}

				double ts;

				if (Options.Instance.DelayTimeStep <= 0)
				{
					ts = Options.Instance.ValidateRange[1];
				}
				else
				{
					ts = Options.Instance.DelayTimeStep;
				}

				d_network.Run(Options.Instance.ValidateRange[0],
					          ts,
					          Options.Instance.ValidateRange[2]);

				// Extract the validation data
				d_monitored = new List<double[]>();

				for (int i = 0; i < d_monitors.Count; ++i)
				{
					d_monitored.Add(d_monitors[i].GetData());
				}
			}

			if (Options.Instance.DelayTimeStep > 0)
			{
				Cdn.Expression expr = new Cdn.Expression(Options.Instance.DelayTimeStep.ToString("R"));
				Cdn.Instruction[] instr = new Cdn.Instruction[1];
				instr[0] = new Cdn.InstructionNumber(Options.Instance.DelayTimeStep);
				expr.Instructions = instr;

				d_network.Integrator.AddVariable(new Cdn.Variable("delay_dt", expr, Cdn.VariableFlags.Out));
			}

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
			if (Options.Instance.Validate)
			{
				// Create a new temporary directory for the output files
				string path = Path.GetTempFileName();
				File.Delete(path);

				Directory.CreateDirectory(path);

				program.Options.Output = path;
				program.Options.Validate = true;
			}

			d_writtenFiles = Options.Instance.Formatter.Write(program);
			
			if (Options.Instance.PrintCompileSource)
			{
				if (Options.Instance.Validate)
				{
					foreach (string filename in d_writtenFiles)
					{
						Console.WriteLine("File: {0}", filename);
						Console.WriteLine(File.ReadAllText(filename));
					}
				}
				else
				{
					Console.WriteLine(Options.Instance.Formatter.CompileSource());
				}
			}

			if (Options.Instance.PrintMexSource)
			{
				Console.WriteLine(Options.Instance.Formatter.MexSource());
			}

			if (Options.Instance.Validate && !Options.Instance.PrintCompileSource)
			{
				try
				{
					Validate();
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e.Message);

					Directory.Delete(program.Options.Output, true);
					Environment.Exit(1);
				}

				Directory.Delete(program.Options.Output, true);
			}
			else if (Options.Instance.Compile != null)
			{
				Log.WriteLine("Compiling code...");
				Options.Instance.Formatter.Compile(Options.Instance.Compile, Options.Instance.Verbose);
			}
		}

		private void StaticRandExpression(Cdn.Expression expr)
		{
			Instruction[] instructions = expr.Instructions;
			Cdn.Stack stack = new Cdn.Stack(expr.StackSize);
			Stack<List<Instruction >> args = new Stack<List<Instruction>>();

			foreach (Cdn.Instruction inst in instructions)
			{
				InstructionRand func = inst as InstructionRand;

				StackManipulation smanip = inst.GetStackManipulation();

				if (func != null)
				{
					func.Next();
					func.Execute(stack);

					double val = stack.At((int)stack.Count() - 1);

					for (int i = 0; i < smanip.NumPop; ++i)
					{
						args.Pop();
					}

					List<Instruction > r = new List<Instruction>();
					r.Add(new InstructionNumber(val));

					args.Push(r);
				}
				else
				{
					InstructionCustomFunction ff = inst as InstructionCustomFunction;

					if (ff != null)
					{
						StaticRandExpression(ff.Function.Expression);
					}

					List<Instruction > ret = new List<Instruction>();

					for (int i = 0; i < smanip.NumPop; ++i)
					{
						List<Instruction > ir = new List<Instruction>(args.Pop());
						ir.Reverse();

						ret.AddRange(ir);
					}

					ret.Reverse();

					ret.Add((Instruction)inst.Copy());
					args.Push(ret);

					inst.Execute(stack);
				}
			}

			List<Instruction > newinst = new List<Instruction>();

			foreach (List<Instruction> r in args)
			{
				newinst.AddRange(r);
			}

			expr.Instructions = newinst.ToArray();
		}

		private void StaticRandExpressions()
		{
			d_network.ForeachExpression((expr) => {
				expr.ResetCache();
			});

			d_network.ForeachExpression((expr) => {
				StaticRandExpression(expr);
			});

			d_network.Taint();

			var err = new CompileError();

			if (!d_network.Compile(null, err))
			{
				Console.Error.WriteLine("Failed to compile network after static rand: {0}", err.FormattedString);
				Environment.Exit(1);
			}
		}
		
		private void Validate()
		{
			Log.WriteLine("Validating generated network...");

			string tmpprog = Path.GetTempFileName();
			
			Options opts = Options.Instance;
			opts.Formatter.Compile(tmpprog, opts.Verbose);

			double ts;

			if (opts.DelayTimeStep <= 0)
			{
				ts = opts.ValidateRange[1];
			}
			else
			{
				ts = opts.DelayTimeStep;
			}
			
			Process process = new Process();
			process.StartInfo.FileName = tmpprog;
			process.StartInfo.Arguments = String.Format("{0} {1} {2}", opts.ValidateRange[0], ts, opts.ValidateRange[2]);
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.UseShellExecute = false;

			process.Start();

			process.StandardInput.WriteLine("t");

			for (int i = 1; i < d_monitors.Count; ++i)
			{
				Cdn.Variable prop;
				string name;

				prop = d_monitors[i].Variable;

				if (prop.Object is Cdn.Integrator || prop.Object is Cdn.Network)
				{
					name = prop.Name;
				}
				else
				{
					name = prop.FullName;
				}

				process.StandardInput.WriteLine(name);
			}
			
			process.StandardInput.Close();

			string output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			
			List<List<double >> data = new List<List<double>>();

			string[] lines = output.Split('\n');
			
			foreach (string line in lines)
			{
				if (String.IsNullOrEmpty(line))
				{
					continue;
				}

				try
				{
					data.Add(new List<double>(Array.ConvertAll<string, double>(line.Split('\t'), a => {
						string trimmed = a.Trim().ToLower();

						if (trimmed == "nan")
						{
							return Double.NaN;
						}
						else if (trimmed == "-nan")
						{
							return -Double.NaN;
						}
						else if (trimmed == "inf")
						{
							return Double.PositiveInfinity;
						}
						else if (trimmed == "-inf")
						{
							return Double.NegativeInfinity;
						}
						else
						{
							return Double.Parse(a);
						}
					})));
				}
				catch
				{
					throw new Exception(String.Format("Could not parse number:\n{0}", line));
				}
			}

			List<string > failures = new List<string>();

			for (int i = 0; i < data.Count; ++i)
			{
				List<double > raw = data[i];
				
				for (int j = 0; j < raw.Count; ++j)
				{
					if (System.Math.Abs(d_monitored[j][i] - raw[j]) > opts.ValidatePrecision ||
					    double.IsNaN(d_monitored[j][i]) != double.IsNaN(raw[j]))
					{
						failures.Add(String.Format("{0} (got {1} but expected {2})",
							         d_monitors[j].Variable.FullName,
							         raw[j],
							         d_monitored[j][i]));
					}
				}

				if (failures.Count > 0)
				{
					throw new Exception(String.Format("Discrepancy detected at t = {0}:\n  {1}",
						                              opts.ValidateRange[0] + (i * ts),
						                              String.Join("\n  ", failures.ToArray())));

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
			ret.DelayTimeStep = Options.Instance.DelayTimeStep;
			
			if (String.IsNullOrEmpty(ret.Basename))
			{
				ret.Basename = Path.GetFileNameWithoutExtension(ret.Network.Filename);
			}
			
			if (String.IsNullOrEmpty(ret.Output))
			{
				ret.Output = Path.GetDirectoryName(ret.Network.Filename);
			}

			ret.OriginalOutput = ret.Output;
			
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

			foreach (State state in Knowledge.Instance.InitializeStates)
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
				d_network = new Cdn.Network(d_filename);
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
				forest.Add(Tree.Node.Create(state));
			}

			foreach (State state in Knowledge.Instance.InitializeStates)
			{
				forest.Add(Tree.Node.Create(state));
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

