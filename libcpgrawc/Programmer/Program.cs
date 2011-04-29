using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer
{
	public class Program
	{
		private Options d_options;

		private List<Computation.INode> d_source;
		private List<Function> d_functions;
		private List<Tree.Embedding> d_embeddings;
		private List<Computation.INode> d_initialization;
		private List<Cpg.Function> d_usedCustomFunctions;
		private Dictionary<string, Function> d_functionMap;

		private DataTable d_statetable;
		private DataTable d_integratetable;
		private Dictionary<State, Tree.Node> d_equations;

		public Program(Options options, IEnumerable<Tree.Embedding> embeddings, Dictionary<State, Tree.Node> equations)
		{
			// Write out equations and everything
			d_statetable = new DataTable("ss", true);
			d_integratetable = new DataTable("si", false);

			d_functions = new List<Function>();
			d_embeddings = new List<Tree.Embedding>(embeddings);
			d_source = new List<Computation.INode>();
			d_initialization = new List<Computation.INode>();
			d_usedCustomFunctions = new List<Cpg.Function>();
			d_functionMap = new Dictionary<string, Function>();

			d_equations = equations;
			d_options = options;
			
			ProgramDataTables();			
			ProgramFunctions();
			ProgramCustomFunctions();
			ProgramInitialization();
			ProgramSource();
		}
		
		public DataTable StateTable
		{
			get
			{
				return d_statetable;
			}
		}
		
		public DataTable IntegrateTable
		{
			get
			{
				return d_integratetable;
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
		
		private void ProgramDataTables()
		{
			// Add integrated state variables
			foreach (State state in Knowledge.Instance.IntegratedStates)
			{
				d_statetable.Add(state);
				d_integratetable.Add(state);
			}
			
			// Add direct state variables
			foreach (State state in Knowledge.Instance.DirectStates)
			{
				d_statetable.Add(state);
			}
			
			// Add in variables
			foreach (Cpg.Property prop in Knowledge.Instance.InProperties)
			{
				d_statetable.Add(prop);
			}
			
			// Add out variables
			foreach (Cpg.Property prop in Knowledge.Instance.OutProperties)
			{
				d_statetable.Add(prop);
			}
			
			// Add intialized variables
			foreach (State state in Knowledge.Instance.InitializeStates)
			{
				d_statetable.Add(state);
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
				string name = GenerateFunctionName(String.Format("f_{0}", d_functions.Count + 1));
				Function function = new Function(name, embedding);
				
				foreach (Tree.Node instance in embedding.Instances)
				{
					instance.Instruction = new Instructions.Function(function);
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
			
			foreach (Function function in d_functions)
			{
				foreach (Tree.Node node in function.Expression.Collect<T>())
				{
					yield return (T)node.Instruction;
				}
			}
		}
		
		private void CustomFunctionUsage(Tree.Node node, Dictionary<Cpg.Function, List<Tree.Node>> usage)
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
		
		private void ProgramCustomFunctions()
		{			
			Dictionary<Cpg.Function, List<Tree.Node>> usage = new Dictionary<Cpg.Function, List<Tree.Node>>();
			
			// Calculate map from a custom function to the nodes that use that function
			foreach (KeyValuePair<State, Tree.Node> eq in d_equations)
			{
				foreach (Tree.Node node in eq.Value.Collect<Cpg.InstructionCustomFunction>())
				{
					CustomFunctionUsage(node, usage);
				}
			}
			
			// Check also in the generated function implementations
			foreach (Function function in d_functions)
			{
				foreach (Tree.Node node in function.Expression.Collect<Cpg.InstructionCustomFunction>())
				{
					CustomFunctionUsage(node, usage);
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
					args.Add(new Tree.Embedding.Argument(child.RelPath(node), (uint)idx));
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
					embedding.Embed(nn);

					nn.Instruction = new Instructions.Function(func);
				}
			}
		}
		
		private List<Computation.INode> AssignmentStates(IEnumerable<State> states)
		{
			List<Computation.INode> ret = new List<Computation.INode>();

			foreach (State state in states)
			{
				ret.Add(new Computation.Assignment(d_statetable[state], d_equations[state]));
			}
			
			return ret;
		}
		
		private void ProgramSource()
		{
			Cpg.Property dtprop = Knowledge.Instance.Network.Integrator.Property("dt");
			DataTable.DataItem dt = d_statetable[dtprop];
			Tree.Node dteq = new Tree.Node(null, new Instructions.Variable("timestep"));
			
			// Set dt
			d_source.Add(new Computation.Comment("Set timestep"));
			d_source.Add(new Computation.Assignment(dt, dteq));
			d_source.Add(new Computation.Empty());
			
			// Precompute for out properties
			if (Knowledge.Instance.PrecomputeBeforeDirectStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Out properties that depend on IN states and are needed by direct calculations"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.PrecomputeBeforeDirectStates));
				d_source.Add(new Computation.Empty());
			}
			
			// Direct links	
			if (Knowledge.Instance.DirectStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Direct equations"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.IntegratedStates));
				d_source.Add(new Computation.Empty());
			}
			
			// Precompute for out properties
			if (Knowledge.Instance.PrecomputeBeforeIntegratedStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Out properties that depend on direct states and are needed by integration calculations"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.PrecomputeBeforeIntegratedStates));
				d_source.Add(new Computation.Empty());
			}
			
			// Integrated links			
			if (Knowledge.Instance.IntegratedStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Clear integration update table"));
				d_source.Add(new Computation.CopyTable(d_statetable, d_integratetable, d_integratetable.Count));
				d_source.Add(new Computation.Empty());

				d_source.Add(new Computation.Comment("Integration equations"));

				foreach (State state in Knowledge.Instance.IntegratedStates)
				{
					Tree.Node node = new Tree.Node(null, new InstructionOperator((uint)Cpg.MathOperatorType.Multiply, "*", 2));
					Tree.Node left = d_equations[state];
					Tree.Node right = new Tree.Node(null, new Instructions.Variable("timestep"));
				
					node.Add(left);
					node.Add(right);

					d_source.Add(new Computation.Addition(d_integratetable[state], node));
				}

				d_source.Add(new Computation.Empty());
				d_source.Add(new Computation.Comment("Copy integrated values to state table"));
				d_source.Add(new Computation.CopyTable(d_integratetable, d_statetable, d_integratetable.Count));
				
				d_source.Add(new Computation.Empty());
			}
			
			// Postcompute for out properties
			if (Knowledge.Instance.PrecomputeAfterIntegratedStatesCount != 0)
			{
				d_source.Add(new Computation.Comment("Out properties that depend on integrated states or IN states"));
				d_source.AddRange(AssignmentStates(Knowledge.Instance.PrecomputeAfterIntegratedStates));
				d_source.Add(new Computation.Empty());
			}
			
			// Increase time
			DataTable.DataItem t = d_statetable[Knowledge.Instance.Network.Integrator.Property("t")];
			Tree.Node eq = new Tree.Node(null, new InstructionProperty(dtprop, InstructionPropertyBinding.None));

			d_source.Add(new Computation.Comment("Increase time"));
			d_source.Add(new Computation.Addition(t, eq));
		}
		
		private void ProgramInitialization()
		{
			foreach (State state in Knowledge.Instance.InitializeStates)
			{
				if (!d_statetable.Contains(state))
				{
					continue;
				}

				d_initialization.Add(new Computation.Assignment(d_statetable[state], d_equations[state]));
			}
		}
		
		public IEnumerable<Cpg.Function> UsedCustomFunctions
		{
			get
			{
				return d_usedCustomFunctions;
			}
		}
		
		public IEnumerable<Computation.INode> InitializationNodes
		{
			get
			{
				return d_initialization;
			}
		}
		
		public IEnumerable<Computation.INode> SourceNodes
		{
			get
			{
				return d_source;
			}
		}
		
		public IEnumerable<DataTable> DataTables
		{
			get
			{
				yield return d_statetable;
				yield return d_integratetable;
			}
		}
	}
}

