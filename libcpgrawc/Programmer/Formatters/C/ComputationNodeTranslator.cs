using System;

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
		
		private string Translate(Computation.Assignment node, Context context)
		{
			return String.Format("{0}[{1}] = {2};",
			                     node.Item.Table.Name,
			                     node.Item.Index,
			                     InstructionTranslator.QuickTranslate(context.Base().Push(node.Equation)));
		}
		
		private string Translate(Computation.Addition node, Context context)
		{
			return String.Format("{0}[{1}] += {2};",
			                     node.Item.Table.Name,
			                     node.Item.Index,
			                     InstructionTranslator.QuickTranslate(context.Base().Push(node.Equation)));
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