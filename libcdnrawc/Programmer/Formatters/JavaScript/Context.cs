using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC.Programmer.Formatters.JavaScript
{
	public class Context : CLike.Context
	{
		public Context(Program program, CLike.Options options) : this(program, options, null, null)
		{
		}

		public Context(Program program, CLike.Options options, Tree.Node node, Dictionary<Tree.NodePath, string> mapping) : base(program, options, node, mapping)
		{
		}

		protected override CLike.Context Clone()
		{
			return new Context(Program, Options);
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
			StringBuilder ret = new StringBuilder();
			ret.Append('[');

			for (int i = 0; i < size; ++i)
			{
				if (i != 0)
				{
					ret.Append(", ");
				}

				ret.Append('0');
			}

			ret.Append(']');
			return ret.ToString();
		}

		public override string This(string name)
		{
			{
				return "this.data." + name;
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
			return String.Format("Cdn.Utils.memzero({0}, {1}, {2})",
			                     dest,
			                     destStart,
			                     nelem);
		}
	}
}

