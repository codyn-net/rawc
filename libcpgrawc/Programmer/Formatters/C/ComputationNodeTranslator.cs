using System;
using System.Text;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer.Formatters.C
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
		
		public static string Translate(Computation.INode node, Context context)
		{
			return (new ComputationNodeTranslator()).Invoke<string>(node, context);
		}
		
		private string Translate(Computation.Loop node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			
			Context ctx = new Context(context.Program, context.Options, node.Expression, node.Mapping);
			
			ret.AppendFormat("for (i = 0; i < {0}; ++i)", node.Items.Count);
			ret.AppendLine();
			ret.AppendLine("{");
			ret.AppendFormat("\t{0}[{1}[i][0]] = {2};",
			               context.Program.StateTable.Name,
			               node.IndexTable.Name,
			               InstructionTranslator.QuickTranslate(ctx));
			ret.AppendLine();
			ret.Append("}");
			
			return ret.ToString();
		}
		
		private string Translate(Computation.IncrementDelayedCounters node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			
			ret.AppendFormat("for (i = 0; i < {0}; ++i)", node.Counters.Count);
			ret.AppendLine();
			ret.AppendLine("{");
			ret.AppendFormat("\tif ({0}[i] == {1}[i] - 1)", node.Counters.Name, node.CountersSize.Name);
			ret.AppendLine();
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t{0}[i] = 0;", node.Counters.Name);
			ret.AppendLine();
			ret.AppendLine("\t}");
			ret.AppendLine("\telse");
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t++{0}[i];", node.Counters.Name);
			ret.AppendLine();
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
			if ((node.Item.Type & DataTable.DataItem.Flags.Integrated) != 0 &&
				context.Program.NodeIsInitialization(node))
			{
				return String.Format("{0}[{1}] = {0}[{2}] = {3};",
			                     node.Item.Table.Name,
			                     node.Item.AliasOrIndex,
					             node.Item.Index + context.Program.IntegrateTable.Count,
			                     InstructionTranslator.QuickTranslate(context.Base().Push(node.State, node.Equation)));
			}

			return String.Format("{0}[{1}] = {2};",
			                     node.Item.Table.Name,
			                     node.Item.AliasOrIndex,
			                     InstructionTranslator.QuickTranslate(context.Base().Push(node.State, node.Equation)));
		}
		
		private string Translate(Computation.ZeroTable node, Context context)
		{
			return String.Format("memset ({0}, 0, sizeof ({0}));", node.DataTable.Name);
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

			return String.Format("memcpy ({0}, {1}, sizeof ({2}) * {3});", target, source, context.Options.ValueType, node.Size);
		}
	}
}