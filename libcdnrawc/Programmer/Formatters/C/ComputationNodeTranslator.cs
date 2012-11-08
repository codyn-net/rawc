using System;
using System.Text;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class ComputationNodeTranslator : DynamicVisitor
	{
		public ComputationNodeTranslator() : base(typeof(string),
		                                          BindingFlags.Default,
		                                          System.Reflection.BindingFlags.Default |
		                                          System.Reflection.BindingFlags.NonPublic |
		                                          System.Reflection.BindingFlags.Instance |
		                                          System.Reflection.BindingFlags.InvokeMethod,
		                                          new Type[] {typeof(Computation.INode), typeof(Context)})
		{
		}

		public static string Reindent(string s, string indent)
		{
			if (String.IsNullOrEmpty(s))
			{
				return s;
			}

			string[] lines = s.Split('\n');
			return indent + String.Join("\n" + indent, lines).Replace("\n" + indent + "\n", "\n\n");
		}
		
		public static string Translate(Computation.INode node, Context context)
		{
			return (new ComputationNodeTranslator()).Invoke<string>(node, context);
		}

		private string Translate(Computation.Rand node, Context context)
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

		private string Translate(Computation.Block node, Context context)
		{
			var ret = new StringBuilder();

			ret.AppendLine("{");

			for (int i = 0; i < node.Body.Count; ++i)
			{
				var child = node.Body[i];

				if (i != node.Body.Count - 1 || !(child is Computation.Empty))
				{
					ret.AppendLine(Reindent(Translate(child, context), "\t"));
				}
			}

			ret.AppendLine("}");

			return ret.ToString();
		}

		private string Translate(Computation.StateConditional node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			var indices = node.EventStateGroup.Indices;
			List<string> conditions = new List<string>();

			foreach (var idx in indices)
			{
				var evstate = Knowledge.Instance.EventStates[idx];
				var container = Knowledge.Instance.EventStatesMap[evstate.Node];

				conditions.Add(String.Format("evstates[{0}] == {1}", container.Index, idx));
			}

			var cond = String.Join(" || ", conditions);

			ret.AppendLine("{");
			ret.AppendFormat("\t{0} const *evstates = ({0} const *)({1} + {2});",
			                 context.Options.EventStateType,
			                 context.Program.StateTable.Name,
			                 context.Program.StateTable.Count);

			ret.AppendLine();
			ret.AppendLine();

			ret.AppendFormat("\tif ({0})", cond);
			ret.AppendLine();
			ret.AppendLine("\t{");

			for (int i = 0; i < node.Body.Count; ++i)
			{
				var child = node.Body[i];

				if (i != node.Body.Count - 1 || !(child is Computation.Empty))
				{
					ret.AppendLine(Reindent(Translate(child, context), "\t\t"));
				}
			}

			ret.AppendLine("\t}");
			ret.Append("}");

			return ret.ToString();
		}

		private string Translate(Computation.CallAPI node, Context context)
		{
			StringBuilder ret = new StringBuilder();

			ret.AppendFormat("{0}_{1} (", context.Options.CPrefixDown, node.Function.Name);

			for (int i = 0; i < node.Arguments.Length; ++i)
			{
				if (i != 0)
				{
					ret.Append(", ");
				}

				var arg = node.Arguments[i];

				string eq = InstructionTranslator.QuickTranslate(context.Base().Push(arg));
				ret.Append(eq);
			}

			ret.AppendFormat(");");
			return ret.ToString();
		}

		private string Translate(Computation.Loop node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			
			Context ctx = new Context(context.Program, context.Options, node.Expression, node.Mapping);

			ret.AppendLine("{");
			ret.AppendLine("\tint i;");
			ret.AppendLine();
			ret.AppendFormat("\tfor (i = 0; i < {0}; ++i)", node.Items.Count);
			ret.AppendLine();
			ret.AppendLine("\t{");

			if (Cdn.RawC.Options.Instance.Verbose)
			{
				var dt = (node.IndexTable[0].Object as Computation.Loop.Index).DataItem;

				ret.AppendFormat("\t\t/* {0}[{1}] = {2} ({3}",
				                 context.Program.StateTable.Name,
				                 dt.AliasOrIndex.Replace("/*", "//").Replace("*/", "//"),
				                 node.Function.Name,
				                 context.Program.StateTable.Name);

				var eq = node.Items[0].Equation;

				foreach (Tree.Embedding.Argument arg in node.Function.OrderedArguments)
				{
					Tree.Node subnode = eq.FromPath(arg.Path);
					DataTable.DataItem it = context.Program.StateTable[subnode];
				
					ret.AppendFormat(", {0}[{1}]",
					                 context.Program.StateTable.Name,
					                 it.AliasOrIndex.Replace("/*", "//").Replace("*/", "//"));
				}

				ret.AppendLine("); */");
			}

			ret.AppendFormat("\t\t{0}[{1}[i][0]] = {2};",
			               context.Program.StateTable.Name,
			               node.IndexTable.Name,
			               InstructionTranslator.QuickTranslate(ctx));
			ret.AppendLine();
			ret.AppendLine("\t}");
			ret.Append("}");
			
			return ret.ToString();
		}

		private string Translate(Computation.InitializeDelayHistory node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			string eq = InstructionTranslator.QuickTranslate(context.Base().Push(node.State, node.Equation));

			if (node.State.Operator.InitialValue == null)
			{
				ret.AppendLine(Translate(new Computation.ZeroMemory(node.History), context));

				ret.AppendFormat("{0}[{1}] = 0.0;",
					context.Program.StateTable.Name,
					context.Program.StateTable[node.State].AliasOrIndex);

				return ret.ToString();
			}
			else if (!node.OnTime)
			{
				ret.AppendLine("{");
				ret.AppendLine("\tint i;");
				ret.AppendFormat("\t{0} _tmp = {1};", context.Options.ValueType, eq);
				ret.AppendLine();
				ret.AppendLine();

				ret.AppendFormat("\tfor (i = 0; i < {0}; ++i)", node.History.Count);
				ret.AppendLine();
				ret.AppendLine("\t{");
				ret.AppendFormat("\t\t{0}[i] = _tmp;", node.History.Name);
				ret.AppendLine();
				ret.AppendLine("\t}");
				ret.AppendLine();
				ret.AppendFormat("\t{0}[{1}] = _tmp;", context.Program.StateTable.Name, context.Program.StateTable[node.State].AliasOrIndex);
				ret.AppendLine();
				ret.Append("}");

				return ret.ToString();
			}

			var n = new Tree.Node(null, new InstructionVariable(Knowledge.Instance.Time.Object as Variable));
			var ctx = new Context(context.Program, context.Options, n, null);

			string ss = InstructionTranslator.QuickTranslate(ctx);

			ret.AppendFormat("{0} = t - {1};", ss, context.Program.Options.DelayTimeStep * (node.History.Count + 1));
			ret.AppendLine();
			ret.AppendLine();
			ret.AppendLine("{");
			ret.AppendLine("\tint i;");
			ret.AppendLine();

			ret.AppendFormat("\tfor (i = 0; i < {0}; ++i)", node.History.Count + 1);
			ret.AppendLine();
			ret.AppendLine("\t{");

			ret.AppendFormat("\t\t{0} += {1};", ss, context.Program.Options.DelayTimeStep);
			ret.AppendLine();
			ret.AppendLine();

			foreach (Computation.INode dep in node.Dependencies)
			{
				ret.AppendLine(Reindent(Translate(dep, context), "\t\t"));
			}

			ret.AppendLine();
			ret.AppendFormat("\t\tif (i != 0)");
			ret.AppendLine();
			ret.AppendLine("\t\t{");
			ret.AppendFormat("\t\t\t{0}[i] = {1};", node.History.Name, eq);
			ret.AppendLine();
			ret.AppendLine("\t\t}");
			ret.AppendLine("\t\telse");
			ret.AppendLine("\t\t{");
			ret.AppendFormat("\t\t\t{0}[{1}] = {2};",
				context.Program.StateTable.Name,
				context.Program.StateTable[node.State].AliasOrIndex,
				eq);
			ret.AppendLine();
			ret.AppendLine("\t\t}");

			ret.AppendLine("\t}");
			ret.Append("}");

			return ret.ToString();
		}
		
		private string Translate(Computation.IncrementDelayedCounters node, Context context)
		{
			StringBuilder ret = new StringBuilder();

			ret.AppendLine("{");
			ret.AppendLine("\tint i;");
			ret.AppendLine();
			ret.AppendFormat("\tfor (i = 0; i < {0}; ++i)", node.Counters.Count);
			ret.AppendLine();
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\tif ({0}[i] == {1}[i] - 1)", node.Counters.Name, node.CountersSize.Name);
			ret.AppendLine();
			ret.AppendLine("\t\t{");
			ret.AppendFormat("\t\t\t{0}[i] = 0;", node.Counters.Name);
			ret.AppendLine();
			ret.AppendLine("\t\t}");
			ret.AppendLine("\t\telse");
			ret.AppendLine("\t\t{");
			ret.AppendFormat("\t\t\t++{0}[i];", node.Counters.Name);
			ret.AppendLine();
			ret.AppendLine("\t\t}");
			ret.AppendLine("\t}");
			ret.Append("}");
			
			Dictionary<DataTable, bool > seen = new Dictionary<DataTable, bool>();
			bool first = true;
			
			// Update counter loop indices
			foreach (Computation.Loop loop in context.Program.Loops)
			{
				DataTable table = loop.IndexTable;
				
				if (seen.ContainsKey(table))
				{
					continue;
				}
				
				for (int i = 0; i < table.Count; ++i)
				{
					DataTable.DataItem item = table[i];
					
					if (item.HasType(DataTable.DataItem.Flags.Delayed))
					{
						int r = i % table.Columns;
						int c = i / table.Columns;
						Computation.Loop.Index sidx = (Computation.Loop.Index)item.Key;

						DataTable.DataItem state = context.Program.StateTable[(int)sidx.Value];
						DelayedState.Key delayed = (DelayedState.Key)state.Key;
						DataTable.DataItem idx = context.Program.DelayedCounters[delayed.Size];
						
						if (first)
						{
							ret.AppendLine();
							first = false;
						}

						ret.AppendLine();
						ret.AppendFormat("{0}[{1}][{2}] = {3} + {4}[{5}];",
						                 table.Name, r, c,
						                 sidx.Value,
						                 context.Program.DelayedCounters.Name, idx.Index);
					}
				}
				
				seen[table] = true;
			}
			
			return ret.ToString();
		}
		
		private string Translate(Computation.Assignment node, Context context)
		{
			string eq = InstructionTranslator.QuickTranslate(context.Base().Push(node.State, node.Equation));

			if (node.State is DelayedState)
			{
				DelayedState ds = (DelayedState)node.State;
				StringBuilder ret = new StringBuilder();

				uint size = (uint)System.Math.Round(ds.Delay / Cdn.RawC.Options.Instance.DelayTimeStep);
				DataTable.DataItem counter = context.Program.DelayedCounters[new DelayedState.Size(size)];
				DataTable table = context.Program.DelayHistoryTable(ds);

				ret.AppendFormat("{0}[{1}] = {2}[{3}[{4}]];",
					node.Item.Table.Name,
					node.Item.AliasOrIndex,
					table.Name,
					context.Program.DelayedCounters.Name,
					counter.Index);

				ret.AppendLine();
				ret.AppendFormat("{0}[{1}[{2}]] = {3};",
					table.Name,
					context.Program.DelayedCounters.Name,
					counter.Index,
					eq);

				return ret.ToString();
			}
			else
			{
				return String.Format("{0}[{1}] = {2};",
				                     node.Item.Table.Name,
				                     node.Item.AliasOrIndex,
				                     eq);
			}
		}
		
		private string Translate(Computation.ZeroMemory node, Context context)
		{
			if (node.Name == null)
			{
				return String.Format("memset (network, 0, CDN_RAWC_NETWORK_{0}_SIZE);", context.Options.CPrefixUp);
			}
			else if (node.DataTable == null)
			{
				return string.Format("memset ({0}, 0, {1});", node.Name, node.Size);
			}
			else if (node.DataTable.IntegerType)
			{
				return String.Format("memset ({0}, 0, sizeof ({0}));",
				                     node.DataTable.Name);
			}
			else
			{
				return String.Format("memset ({0}, 0, sizeof (ValueType) * {1});",
				                     node.DataTable.Name,
				                     node.DataTable.Count);
			}
		}
		
		private string Translate(Computation.Empty node, Context context)
		{
			return "";
		}
		
		private string Translate(Computation.Comment node, Context context)
		{
			return String.Format("/* {0} */", node.Text);
		}
		
		private string Translate(Computation.CopyTable node, Context context)
		{
			string target = node.Target.Name;
			string source = node.Source.Name;

			if (node.TargetIndex != 0)
			{
				target = String.Format("{0} + {1}", target, node.TargetIndex);
			}
			
			if (node.SourceIndex != 0)
			{
				source = String.Format("{0} + {1}", source, node.SourceIndex);
			}

			if (node.Size > 0)
			{
				return String.Format("memcpy ({0}, {1}, sizeof ({2}) * {3});",
				                     target,
				                     source,
				                     context.Options.ValueType,
				                     node.Size);
			}
			else if (node.Size < 0 && node.Source.Count > 0)
			{
				return string.Format("memcpy ({0}, {1}, sizeof ({1}));", target, source);
			}
			else
			{
				return "/* No constants to copy */";
			}
		}
	}
}