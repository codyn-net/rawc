using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC.Programmer.Formatters.JavaScript
{
	public class Context : CLike.Context
	{
		public static string DataName = "_data";

		public Context(Program program, CLike.Options options) : this(program, options, null, null)
		{
		}

		public Context(Program program, CLike.Options options, Tree.Node node, Dictionary<Tree.NodePath, object> mapping) : base(program, options, node, mapping)
		{
		}

		public override string MathFunction(Cdn.MathFunctionType type, int arguments)
		{
			var val = base.MathFunction(type, arguments);
			return "Cdn.Math." + val;
		}

		public override string MathFunctionV(Cdn.MathFunctionType type, Tree.Node node)
		{
			var val = base.MathFunctionV(type, node);
			return "Cdn.Math." + val;
		}

		public static string ZeroArrayOfSize(int size)
		{
			return ZeroArrayOfSize(size, true);
		}

		public static string ZeroArrayOfSize(int size, bool isfloat)
		{
			StringBuilder ret = new StringBuilder();
			ret.Append('[');

			for (int i = 0; i < size; ++i)
			{
				if (i != 0)
				{
					ret.Append(", ");
				}

				if (isfloat)
				{
					ret.Append("0.0");
				}
				else
				{
					ret.Append('0');
				}
			}

			ret.Append(']');
			return ret.ToString();
		}

		public override string ThisCall(string name)
		{
			return "this." + name;
		}

		public override string This(string name)
		{
			return "this." + DataName + "." + name;
		}

		public override string This(DataTable table)
		{
			if (table.IsConstant)
			{
				return String.Format("Cdn.{0}.{1}", Options.CPrefix, table.Name);
			}
			else
			{
				return String.Format("this.{0}.{1}", DataName, table.Name);
			}
		}

		public override bool SupportsPointers
		{
			get { return false; }
		}

		public override string MemCpy(string dest, string destStart, string source, string sourceStart, string type, int nelem)
		{
			return String.Format("Cdn.Utils.memcpy({0}, {1}, {2}, {3}, {4})",
			                     dest,
			                     destStart,
			                     source,
			                     sourceStart,
			                     nelem);
		}

		public override string MemZero(string dest, string destStart, string type, int nelem)
		{
			var n = "memzero";

			if (type == "ValueType")
			{
				n += "_f";
			}

			return String.Format("Cdn.Utils.{0}({1}, {2}, {3})",
			                     n,
			                     dest,
			                     destStart,
			                     nelem);
		}

		public override CLike.Context Clone(Program program, CLike.Options options, Tree.Node node, Dictionary<Tree.NodePath, object> mapping)
		{
			return new Context(program, options, node, mapping);
		}

		public override bool TryMapping(Tree.Node node, out string ret)
		{
			Tree.NodePath path = node.RelPath(Root);

			object o;

			if (Mapping.TryGetValue(path, out o))
			{
				var li = o as Computation.Loop.Mapped;

				if (li != null && !li.Node.Dimension.IsOne)
				{
					ret = String.Format("Cdn.Utils.array_slice({0}, {1}[i][{2}], {1}[i][{2}] + {3})",
					                    This(Program.StateTable),
					                    This(li.IndexTable),
					                    li.Index,
					                    li.Node.Dimension.Size());
					return true;
				}
			}

			return base.TryMapping(node, out ret);
		}

		public override string ArraySlice(string v, string start, string end)
		{
			return String.Format("Cdn.Utils.array_slice({0}, {1}, {2})", v, start, end);
		}

		public override string ArraySliceIndices(string v, int[] indices)
		{
			var ar = String.Join(", ", Array.ConvertAll(indices, a => a.ToString()));
			return String.Format("Cdn.Utils.array_slice_indices({0}, [{1}])", v, ar);
		}

		public override string ArrayConcat(string[] arrays)
		{
			return String.Format("Cdn.Utils.array_concat({0})", String.Join(", ", arrays));
		}

		public override bool SupportsFirstClassArrays
		{
			get { return true; }
		}

		public override string DeclareValueVariable(string type, string name)
		{
			return String.Format("var {0}", name);
		}

		public override string DeclareArrayVariable(string type, string name, int size)
		{
			return DeclareValueVariable(type, name);
		}

		public override string APIName(Computation.CallAPI node)
		{
			return ThisCall(node.Function.Name);
		}

		public override string FunctionCallName(Programmer.Function function)
		{
			return ThisCall(function.Name);
		}

	}
}

