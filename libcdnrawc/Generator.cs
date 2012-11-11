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
		private Validator d_validator;

		public Generator(string filename)
		{
			d_filename = filename;
		}

		public void Generate()
		{
			if ((Options.Instance.Compile == null && !Options.Instance.Validate) || Options.Instance.Verbose)
			{
				Log.WriteLine("Generating code for network...");
			}
			
			LoadNetwork();

			if (Options.Instance.Validate)
			{
				d_validator = new Validator(d_network);
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
			bool outistemp = false;
			
			// Write program
			if (Options.Instance.Validate || Options.Instance.Compile != null)
			{
				// Create a new temporary directory for the output files
				string path = Path.GetTempFileName();
				File.Delete(path);

				Directory.CreateDirectory(path);
				program.Options.Output = path;

				outistemp = true;
			}
			else
			{
				Directory.CreateDirectory(program.Options.Output);
			}

			d_writtenFiles = Options.Instance.Formatter.Write(program);
			
			if (Options.Instance.PrintCompileSource)
			{
				foreach (string filename in d_writtenFiles)
				{
					Console.WriteLine("File: {0}", filename);
					Console.WriteLine(File.ReadAllText(filename));
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
					d_validator.Validate(program);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e.Message);

					Directory.Delete(program.Options.Output, true);
					Environment.Exit(1);
				}
			}
			else if (Options.Instance.Compile != null)
			{
				var files = Options.Instance.Formatter.Compile(Options.Instance.Verbose);

				if (Options.Instance.Verbose)
				{
					Log.WriteLine("Compiled {0}...", String.Join(", ", Array.ConvertAll<string, string>(files, a => Path.GetFileName(a))));
				}

				foreach (var f in files)
				{
					var dest = Path.Combine(Options.Instance.Compile, Path.GetFileName(f));

					try
					{
						File.Delete(dest);
					} catch {}

					File.Move(f, dest);
				}
			}

			if (outistemp)
			{
				try
				{
					Directory.Delete(program.Options.Output, true);
				} catch {};
			}
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
				var dirname = Path.GetDirectoryName(ret.Network.Filename);
				ret.Output = Path.Combine(dirname, "rawc_" + ret.Basename);
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

			foreach (State state in Knowledge.Instance.ExternalConstraintStates)
			{
				ResolveState(state, embeddings, mapping, ret);
			}

			foreach (var ev in Knowledge.Instance.EventSetStates)
			{
				foreach (var state in ev.Value)
				{
					ResolveState(state, embeddings, mapping, ret);
				}
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
			if (Options.Instance.Validate)
			{
				Cdn.InstructionRand.UseStreams = true;
				Cdn.Function.RandAsArgument = true;
			}

			try
			{
				d_network = new Cdn.Network(d_filename);
			}
			catch (GLib.GException e)
			{
				throw new Exception(String.Format("Failed to load network: {0}", e.Message));
			}
			
			CompileError error = new CompileError();

			if (!d_network.Compile(null, error))
			{
				throw new Exception(String.Format("Failed to compile network: {0}", error.FormattedString));
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
			var added = new HashSet<State>();
			
			foreach (State state in Knowledge.Instance.States)
			{
				added.Add(state);

				if (state != Knowledge.Instance.Time && state != Knowledge.Instance.TimeStep)
				{
					forest.Add(Tree.Node.Create(state));
				}
			}

			foreach (State state in Knowledge.Instance.InitializeStates)
			{
				if (added.Add(state))
				{
					forest.Add(Tree.Node.Create(state));
				}
			}
			
			var ret = collector.Collect(forest.ToArray());
			CollectSpecialSingles(ret, forest);

			return ret;
		}

		private void CollectSpecialSingles(Tree.Collectors.Result ret, IEnumerable<Tree.Node> forest)
		{
			// Special case for single instructions. Generate embeddings for
			// those, but separate them strictly.
			Dictionary<uint, List<Tree.Node>> constnodes = new Dictionary<uint, List<Tree.Node>>();
			List<uint> morethanoneconst = new List<uint>();

			foreach (Tree.Node node in forest)
			{
				if (node.ChildCount == 0)
				{
					uint code = Tree.Node.InstructionCode(node.Instruction, true);
					List<Tree.Node> clst;

					if (!constnodes.TryGetValue(code, out clst))
					{
						clst = new List<Tree.Node>();
						constnodes[code] = clst;
					}

					if (clst.Count == 1)
					{
						morethanoneconst.Add(code);
					}

					clst.Add(node);
				}
			}

			foreach (uint code in morethanoneconst)
			{
				var lst = constnodes[code];
				var proto = (Tree.Node)lst[0].Clone();
			
				// Create embedding
				var embedding = ret.Prototype(proto, new Tree.NodePath[] {});
				embedding.Inline = true;
			
				foreach (Tree.Node node in lst)
				{
					embedding.Embed(node);
				}
			}
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

			// Prefilter remove rands
			List<Tree.Embedding> ret = new List<Tree.Embedding>();

			foreach (Tree.Embedding embed in collection.Prototypes)
			{
				if (!(embed.Expression.Instruction is InstructionRand))
				{
					ret.Add(embed);
				}
			}

			return filter.Filter(ret);
		}
	}
}

