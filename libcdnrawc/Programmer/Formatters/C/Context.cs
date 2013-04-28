using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class Context : CLike.Context
	{
		public class Workspace
		{
			public Cdn.MathFunctionType Type;
			public int Order;
			public int WorkSize;
			public Cdn.Dimension Dimension;
		}
		
		private static Dictionary<string, Workspace> s_workspaces;
				
		static Context()
		{
			s_workspaces = new Dictionary<string, Workspace>();
		}
		
		public static Dictionary<string, Workspace> Workspaces
		{
			get { return s_workspaces; }
		}

		public Context(Program program, CLike.Options options) : this(program, options, null, null)
		{
		}

		public Context(Program program, CLike.Options options, Tree.Node node, Dictionary<Tree.NodePath, object> mapping) : base(program, options, node, mapping)
		{
		}

		public override string MathFunction(Cdn.MathFunctionType type, int arguments)
		{
			return String.Format("CDN_MATH_{0}", base.MathFunction(type, arguments).ToUpper());
		}
		
		public override string MathFunctionV(Cdn.MathFunctionType type, Tree.Node node)
		{
			switch (type)
			{
			case MathFunctionType.Linsolve:
			{
				var d2 = node.Children[1].Dimension;
				var ret = String.Format("CDN_MATH_LINSOLVE_V_{0}", d2.Rows);
				
				s_workspaces[ret] = new Workspace {
					Type = type,
					Order = d2.Rows,
					Dimension = d2,
					WorkSize = d2.Rows,
				};

				return ret;
			}
			case MathFunctionType.Inverse:
			{
				var d2 = node.Children[0].Dimension;
				var ret = String.Format("CDN_MATH_INVERSE_V_{0}", d2.Rows);
				
				if (!s_workspaces.ContainsKey(ret))
				{
					int ws = Lapack.InverseWorkspace(d2.Rows);
					
					s_workspaces[ret] = new Workspace {
						Type = type,
						WorkSize = ws,
						Dimension = d2,
						Order = d2.Rows,
					};
				}
				
				return ret;
			}
			default:
				return String.Format("CDN_MATH_{0}", base.MathFunctionV(type, node).ToUpper());
			}
		}

		public override bool SupportsPointers
		{
			get { return true; }
		}

		public override string MemCpy(string dest, string destStart, string source, string sourceStart, string type, int nelem)
		{
			if (destStart != "0" && destStart != "")
			{
				dest = String.Format("({0}) + {1}", dest, destStart);
			}

			if (sourceStart != "0" && sourceStart != "")
			{
				source = String.Format("({0}) + {1}", source, sourceStart);
			}

			type = Context.TypeToCType(type);

			return String.Format("({0} *)memcpy({1}, {2}, sizeof({3}) * {4})",
			                     type, dest, source, type, nelem);
		}

		public override string MemZero(string dest, string destStart, string type, int nelem)
		{
			if (destStart != "0" && destStart != "")
			{
				dest = String.Format("({0}) + {1}", dest, destStart);
			}

			return String.Format("memset({0}, 0, sizeof({1}) * {2})",
			                     dest,
			                     Context.TypeToCType(type),
			                     nelem);
		}

		public static string TypeToCType(string type)
		{
			if (type == "ValueType")
			{
				return type;
			}

			return type + "_t";
		}

		public override CLike.Context Clone(Program program, CLike.Options options, Tree.Node node, Dictionary<Tree.NodePath, object> mapping)
		{
			return new Context(program, options, node, mapping);
		}

		public override string DeclareArrayVariable(string type, string name, int size)
		{
			return base.DeclareArrayVariable(type, name, size) + " = {0,}";
		}

		public override string APIName(Computation.CallAPI node)
		{
			return String.Format("{0}_{1}", Options.CPrefixDown, node.Function.Name);
		}

		public override string FunctionCallName(Function function)
		{
			string name = Context.ToAsciiOnly(function.Name);

			if (function.CanBeOverridden)
			{
				name = name.ToUpper();
			}

			return name;
		}
		
		public override void TranslateFunctionDimensionArguments(InstructionFunction instruction, List<string> args, int cnt)
		{
			switch ((Cdn.MathFunctionType)instruction.Id)
			{
			case MathFunctionType.Linsolve:
			{
				// Add rows of A and columns of B arguments
				var dim1 = Node.Children[1].Dimension;
				var dim2 = Node.Children[0].Dimension;
				
				// Here we also reorder the arguments A and b. In codyn these
				// are on the stack in reversed order to make it more efficient
				// but here we really don't need that and it's more logical
				// the other way around
				var tmp = args[0];
				
				args[0] = args[1];
				args[1] = tmp;
				
				args.Add(dim1.Rows.ToString());
				args.Add(dim2.Columns.ToString());
			}
				break;
			case MathFunctionType.Inverse:
				break;
			default:
				base.TranslateFunctionDimensionArguments(instruction, args, cnt);
				break;
			}
		}
	}
}

