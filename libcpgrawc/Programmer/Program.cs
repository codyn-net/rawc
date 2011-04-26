using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer
{
	public class Program
	{
		private Options d_options;

		private List<IComputationNode> d_direct;
		private List<IComputationNode> d_integrated;
		private List<Function> d_functions;
		private List<Tree.Embedding> d_embeddings;
		private List<IComputationNode> d_initialization;
		private List<Cpg.Function> d_usedCustomFunctions;
		private Dictionary<string, Function> d_functionMap;

		private DataTable d_datatable;
		private Dictionary<State, Tree.Node> d_equations;

		public Program(Options options, IEnumerable<Tree.Embedding> embeddings, Dictionary<State, Tree.Node> equations)
		{
			// Write out equations and everything
			d_datatable = new DataTable("ss");
			d_functions = new List<Function>();
			d_embeddings = new List<Tree.Embedding>(embeddings);
			d_direct = new List<IComputationNode>();
			d_integrated = new List<IComputationNode>();
			d_initialization = new List<IComputationNode>();
			d_usedCustomFunctions = new List<Cpg.Function>();
			d_functionMap = new Dictionary<string, Function>();

			d_equations = equations;
			d_options = options;
			
			ProgramDataTable();			
			ProgramFunctions();
			ProgramCustomFunctions();
			ProgramBody();
		}
		
		public DataTable DataTable
		{
			get
			{
				return d_datatable;
			}
		}
		
		public Options Options
		{
			get
			{
				return d_options;
			}
		}
		
		public Tree.Node Lookup(State state)
		{
			Tree.Node node = null;
			
			d_equations.TryGetValue(state, out node);
			return node;
		}
		
		public IEnumerable<Function> Functions
		{
			get
			{
				return d_functions;
			}
		}
		
		private void ProgramDataTable()
		{
			// Add integrated state variables
			foreach (State state in Knowledge.Instance.IntegratedStates)
			{
				d_datatable.Add(state);
			}
			
			// Add direct state variables
			foreach (State state in Knowledge.Instance.DirectStates)
			{
				d_datatable.Add(state);
			}
			
			// Add in variables
			foreach (Cpg.Property prop in Knowledge.Instance.InProperties)
			{
				d_datatable.Add(prop);
			}
			
			// Add out variables
			foreach (Cpg.Property prop in Knowledge.Instance.OutProperties)
			{
				d_datatable.Add(prop);
			}
		}
		
		private string GenerateFunctionName(string templ)
		{
			int num = 0;
			string name = templ;
			
			while (d_functionMap.ContainsKey(name))
			{
				name = String.Format("{0}{1}", name, ++num);
			}
			
			return name;
		}
		
		private void ProgramFunctions()
		{		
			// Generate functions for all the embeddings
			foreach (Tree.Embedding embedding in d_embeddings)
			{
				string name = GenerateFunctionName(String.Format("f_{0}", d_functions.Count));
				Function function = new Function(name, embedding);
				
				foreach (Tree.Embedding.Instance instance in embedding.Instances)
				{
					Nodes.Function node = new Nodes.Function(instance, function);
					
					if (instance.Path.Count == 0)
					{
						d_equations[instance.State] = node;
					}
					else
					{
						instance.Top.Replace(instance.Path, node);
					}
				}

				d_functions.Add(function);
				d_functionMap[name] = function;
			}
		}
		
		public IEnumerable<T> CollectInstructions<T>() where T : Cpg.Instruction
		{
			foreach (KeyValuePair<State, Tree.Node> eq in d_equations)
			{
				foreach (Tree.Node node in eq.Value.Collect<T>())
				{
					yield return (T)node.Instruction;
				}
			}
		}
		
		private void ProgramCustomFunctions()
		{			
			Dictionary<Cpg.Function, List<Tree.Node>> usage = new Dictionary<Cpg.Function, List<Tree.Node>>();
			
			// Calculate map from a custom function to the nodes that use that function
			foreach (KeyValuePair<State, Tree.Node> eq in d_equations)
			{
				foreach (Tree.Node node in eq.Value.Collect<Cpg.InstructionCustomFunction>())
				{
					List<Tree.Node> lst;
					Cpg.Function f = ((Cpg.InstructionCustomFunction)node.Instruction).Function;
						
					if (!usage.TryGetValue(f, out lst))
					{
						lst = new List<Tree.Node>();
						usage[f] = lst;
						
						d_usedCustomFunctions.Add(f);
					}
					
					lst.Add(node);
				}
			}
			
			// Foreach custom function that is used
			foreach (Cpg.Function function in usage.Keys)
			{
				// Create a new node for the custom function expression
				Tree.Node node = Tree.Node.Create(null, function.Expression.Instructions);
				
				// Calculate all the paths to where the arguments for this function
				// are used in the expression. All arguments are implemented as properties
				List<Cpg.Property> arguments = new List<Cpg.Property>();
				
				foreach (Cpg.FunctionArgument arg in function.Arguments)
				{
					arguments.Add(function.Property(arg.Name));
				}
				
				List<Tree.Embedding.Argument> args = new List<Tree.Embedding.Argument>();
				
				foreach (Tree.Node child in node.Collect<InstructionProperty>())
				{
					InstructionProperty prop = child.Instruction as InstructionProperty;
					int idx = arguments.IndexOf(prop.Property);
					
					if (idx == -1)
					{
						continue;
					}
					
					// Create new embedding argument
					args.Add(new Tree.Embedding.Argument(node.RelPath(child), (uint)idx));
				}
				
				// Here it's a little messy, maybe can be improved. For now we create an
				// embedding for the custom function as if it's a normal function. Then
				// we create instances of that embedding for all the nodes where the custom
				// function is used.
				Tree.Embedding embedding = new Tree.Embedding(node, args);
				string name = String.Format("cf_{0}", function.Id.ToLower());

				Function func = new Function(name, embedding);
				d_functions.Add(func);
				d_functionMap[name] = func;
				
				foreach (Tree.Node nn in usage[function])
				{
					Tree.Node inst = (Tree.Node)node.Clone();
					
					foreach (Tree.Embedding.Argument arg in args)
					{
						inst.FromPath(arg.Path).Replace(nn.Children[(int)arg.Index]);
					}

					Nodes.Function f = new Nodes.Function(embedding.Embed(inst, new Tree.NodePath()), func);
					nn.Replace(f);
				}
			}
		}
		
		private void ProgramBody()
		{
			// Set direct and integration lists of computations
			foreach (State state in Knowledge.Instance.DirectStates)
			{
				if (d_equations.ContainsKey(state))
				{
					d_direct.Add(new Assignment(d_datatable[state], d_equations[state]));
				}
			}
			
			foreach (State state in Knowledge.Instance.IntegratedStates)
			{
				if (d_equations.ContainsKey(state))
				{
					d_integrated.Add(new Addition(d_datatable[state], d_equations[state]));
				}
			}
		}
		
		private void ProgramInitialization()
		{
			foreach (State state in Knowledge.Instance.InitializeStates)
			{
				if (!d_datatable.Contains(state))
				{
					continue;
				}

				d_initialization.Add(new Assignment(d_datatable[state], d_equations[state]));
			}
		}
		
		public IEnumerable<Cpg.Function> UsedCustomFunctions
		{
			get
			{
				return d_usedCustomFunctions;
			}
		}
	}
}

