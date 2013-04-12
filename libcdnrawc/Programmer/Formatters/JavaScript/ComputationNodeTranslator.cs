using System;
using System.Text;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.JavaScript
{
	public class ComputationNodeTranslator : CLike.ComputationNodeTranslator<ComputationNodeTranslator>
	{
		protected override string Translate(Computation.Rand node, CLike.Context context)
		{
			if (node.Empty)
			{
				return "";
			}

			StringBuilder ret = new StringBuilder();

			foreach (Computation.Rand.IndexRange range in node.Ranges(context.Program.StateTable))
			{
				ret.AppendFormat("\tfor ({0} = {1}; i <= {2}; ++i)",
				                 DeclareValueVariable("int", "i", context),
				                 range.Start,
				                 range.End);

				ret.AppendLine();
				ret.AppendLine("\t{");

				ret.AppendFormat("\t\t{0}[i] = Cdn.Math.rand();",
				                 context.Program.StateTable.Name);
				ret.AppendLine();
				ret.AppendLine("\t}");
			}

			ret.Append("}");
			
			return ret.ToString();
		}

		protected override string BeginBlock
		{
			get { return "function() {"; }
		}

		protected override string EndBlock
		{
			get { return "}();"; }
		}

		protected override string APIName(Computation.CallAPI node, CLike.Context context)
		{
			return "this." + node.Function.Name;
		}

		protected override string DeclareValueVariable(string type, string name, CLike.Context context)
		{
			return String.Format("var {0}", name);
		}

		protected override string DeclareArrayVariable(string type, string name, int size, CLike.Context context)
		{
			return String.Format("var {0} {1}[{2}] = {3}",
			                     DeclareValueVariable("ValueType", name, context),
			                     size,
			                     Context.ZeroArrayOfSize(size));
		}
	}
}