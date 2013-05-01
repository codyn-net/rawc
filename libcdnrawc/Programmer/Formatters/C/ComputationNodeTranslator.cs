using System;
using System.Text;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.C
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

			ret.AppendLine("{");
			ret.AppendLine("\tint i;");
			ret.AppendLine();

			foreach (Computation.Rand.IndexRange range in node.Ranges(context.Program.StateTable))
			{
				ret.AppendFormat("\tfor (i = {0}; i <= {1}; ++i)", range.Start, range.End);
				ret.AppendLine();
				ret.AppendLine("\t{");

				if (Cdn.RawC.Options.Instance.Validate)
				{
					if (context.Program.NodeIsInitialization(node))
					{
						ret.AppendLine();
						ret.AppendFormat("\t\tinitstate (rand_seeds[i - {0}], rand_states[i - {0}], sizeof(RandState));",
						                 range.ZeroOffset);
					}
					else
					{
						ret.AppendFormat("\t\tsetstate (rand_states[i - {0}]);", range.ZeroOffset);
					}

					ret.AppendLine();
				}

				ret.AppendFormat("\t\t{0}[i] = CDN_MATH_RAND ();",
				                 context.Program.StateTable.Name);
				ret.AppendLine();
				ret.AppendLine("\t}");
			}

			ret.Append("}");

			return ret.ToString();
		}

		protected override string Translate(Computation.ZeroMemory node, CLike.Context context)
		{
			// Override default copy loop for more efficient memset
			if (node.Name == null)
			{
				return String.Format("memset (network, 0, CDN_RAWC_NETWORK_{0}_SIZE);", context.Options.CPrefixUp);
			}
			else
			{
				return base.Translate(node, context);
			}
		}
	}
}