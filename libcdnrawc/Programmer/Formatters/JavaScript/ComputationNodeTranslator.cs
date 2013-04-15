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
				                 context.DeclareValueVariable("int", "i"),
				                 range.Start,
				                 range.End);

				ret.AppendLine();
				ret.AppendLine("\t{");

				ret.AppendFormat("\t\t{0}[i] = Cdn.Math.rand();",
				                 context.This(context.Program.StateTable));
				ret.AppendLine();
				ret.AppendLine("\t}");
			}

			ret.Append("}");
			
			return ret.ToString();
		}

		protected override string Translate(Computation.ZeroMemory node, CLike.Context context)
		{
			if (node.Name == null)
			{
				return "this._clear_data();";
			}
			else
			{
				return base.Translate(node, context);
			}
		}
	}
}