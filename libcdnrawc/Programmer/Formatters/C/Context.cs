using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class Context : CLike.Context
	{
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
			return String.Format("CDN_MATH_{0}", base.MathFunctionV(type, node).ToUpper());
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
	}
}

