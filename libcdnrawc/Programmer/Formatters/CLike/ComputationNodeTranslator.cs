using System;
using System.Text;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.CLike
{
	public class ComputationNodeTranslator<T> : DynamicVisitor where T : new()
	{
		public ComputationNodeTranslator() : base(typeof(string),
		                                          BindingFlags.Default,
		                                          System.Reflection.BindingFlags.Default |
		                                          System.Reflection.BindingFlags.NonPublic |
		                                          System.Reflection.BindingFlags.Instance |
		                                          System.Reflection.BindingFlags.InvokeMethod,
		                                          a => a.Name == "Translate",
		                                          typeof(Computation.INode),
		                                          typeof(Context))
		{
		}

		public static string Translate(Computation.INode node, Context context)
		{
			return (new T() as ComputationNodeTranslator<T>).Invoke<string>(node, context);
		}

		protected virtual string Translate(Computation.Rand node, Context context)
		{
			throw new Exception("Random numbers are not supporter for this format.");
		}

		protected virtual string BeginBlock
		{
			get { return "{"; }
		}

		protected virtual string EndBlock
		{
			get { return "}"; }
		}

		protected virtual string Translate(Computation.Block node, Context context)
		{
			var ret = new StringBuilder();

			ret.AppendLine(BeginBlock);;

			for (int i = 0; i < node.Body.Count; ++i)
			{
				var child = node.Body[i];

				if (i != node.Body.Count - 1 || !(child is Computation.Empty))
				{
					ret.AppendLine(Context.Reindent(Translate(child, context), "\t"));
				}
			}

			ret.AppendLine(EndBlock);

			return ret.ToString();
		}

		protected virtual string Translate(Computation.StateConditional node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			var indices = node.EventStateGroup.Indices;
			List<string> conditions = new List<string>();

			foreach (var idx in indices)
			{
				var evstate = Knowledge.Instance.EventStates[idx];
				var container = Knowledge.Instance.EventStatesMap[evstate.Node];

				conditions.Add(String.Format("{0}[{1}] == {2}",
				                             context.This("evstates"),
				                             container.Index,
				                             idx));
			}

			var cond = String.Join(" || ", conditions);

			ret.AppendLine(BeginBlock);
			ret.AppendFormat("\tif ({0})", cond);
			ret.AppendLine();
			ret.AppendLine("\t{");

			for (int i = 0; i < node.Body.Count; ++i)
			{
				var child = node.Body[i];

				if (i != node.Body.Count - 1 || !(child is Computation.Empty))
				{
					ret.AppendLine(Context.Reindent(Translate(child, context), "\t\t"));
				}
			}

			ret.AppendLine("\t}");
			ret.Append(EndBlock);

			return ret.ToString();
		}

		protected virtual string APIName(Computation.CallAPI node, Context context)
		{
			return context.This(node.Function.Name);
		}

		protected virtual string Translate(Computation.CallAPI node, Context context)
		{
			StringBuilder ret = new StringBuilder();

			ret.AppendFormat("{0}(", APIName(node, context));

			for (int i = 0; i < node.Arguments.Length; ++i)
			{
				if (i != 0)
				{
					ret.Append(", ");
				}

				var arg = node.Arguments[i];

				string eq = InstructionTranslator.QuickTranslate((Context)context.Base().Push(arg));
				ret.Append(eq);
			}

			ret.AppendFormat(");");
			return ret.ToString();
		}

		protected virtual string Translate(Computation.Loop node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			
			Context ctx = new Context(context.Program, context.Options, node.Expression, node.Mapping);

			ret.AppendFormat("for ({0} = 0; i < {1}; ++i)", DeclareValueVariable("int", "i", context), node.Items.Count);
			ret.AppendLine();
			ret.AppendLine("{");

			if (Cdn.RawC.Options.Instance.Verbose)
			{
				var dt = (node.IndexTable[0].Object as Computation.Loop.Index).DataItem;

				ret.AppendFormat("\t{0} {1}[{2}] = {3}({4}",
				                 BeginComment,
				                 context.This(context.Program.StateTable.Name),
				                 dt.AliasOrIndex.Replace(BeginComment, "//").Replace(EndComment, "//"),
				                 context.This(node.Function.Name),
				                 context.This(context.Program.StateTable.Name));

				var eq = node.Items[0].Equation;

				foreach (Tree.Embedding.Argument arg in node.Function.OrderedArguments)
				{
					Tree.Node subnode = eq.FromPath(arg.Path);
					DataTable.DataItem it = context.Program.StateTable[subnode];
				
					ret.AppendFormat(", {0}[{1}]",
					                 context.This(context.Program.StateTable.Name),
					                 it.AliasOrIndex.Replace(BeginComment, "//").Replace(EndComment, "//"));
				}

				ret.Append("); ");
				ret.Append(EndComment);
				ret.AppendLine();
			}

			ret.AppendLine(Context.Reindent(TranslateAssignment(String.Format("{0}[i][0]",
			                                                                  context.This(node.IndexTable.Name)),
			                                                    ctx),
			                                "\t"));
			ret.Append("}");
			
			return ret.ToString();
		}

		protected virtual string DeclareValueVariable(string type, string name, Context context)
		{
			return String.Format("{0} {1}", type, name);
		}

		protected virtual string Translate(Computation.InitializeDelayHistory node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			string eq = InstructionTranslator.QuickTranslate((Context)context.Base().Push(node.State, node.Equation));

			if (node.State.Operator.InitialValue == null)
			{
				ret.AppendLine(Translate(new Computation.ZeroMemory(node.History), context));

				ret.AppendFormat("{0}[{1}] = 0.0;",
				                 context.This(context.Program.StateTable.Name),
				                 context.Program.StateTable[node.State].AliasOrIndex);

				return ret.ToString();
			}
			else if (!node.OnTime)
			{
				ret.AppendLine(BeginBlock);
				ret.AppendFormat("\t{0} = {1};", DeclareValueVariable("ValueType", "_tmp", context), eq);
				ret.AppendLine();
				ret.AppendLine();

				ret.AppendFormat("\tfor ({0} = 0; i < {0}; ++i)", DeclareValueVariable("int", "i", context), node.History.Count);
				ret.AppendLine();
				ret.AppendLine("\t{");
				ret.AppendFormat("\t\t{0}[i] = _tmp;",
				                 context.This(node.History.Name));
				ret.AppendLine();
				ret.AppendLine("\t}");
				ret.AppendLine();
				ret.AppendFormat("\t{0}[{1}] = _tmp;",
				                 context.This(context.Program.StateTable.Name),
				                 context.Program.StateTable[node.State].AliasOrIndex);
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
			ret.AppendFormat("for ({0} = 0; i < {1}; ++i)", DeclareValueVariable("int", "i", context), node.History.Count + 1);
			ret.AppendLine();
			ret.AppendLine("{");

			ret.AppendFormat("\t{0} += {1};", ss, context.Program.Options.DelayTimeStep);
			ret.AppendLine();
			ret.AppendLine();

			foreach (Computation.INode dep in node.Dependencies)
			{
				ret.AppendLine(Context.Reindent(Translate(dep, context), "\t"));
			}

			ret.AppendLine();
			ret.AppendFormat("\tif (i != 0)");
			ret.AppendLine();
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t{0}[i] = {1};", context.This(node.History.Name), eq);
			ret.AppendLine();
			ret.AppendLine("\t}");
			ret.AppendLine("\telse");
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t{0}[{1}] = {2};",
			                 context.This(context.Program.StateTable.Name),
			                 context.Program.StateTable[node.State].AliasOrIndex,
				eq);
			ret.AppendLine();
			ret.AppendLine("\t}");

			ret.Append("}");

			return ret.ToString();
		}
		
		protected virtual string Translate(Computation.IncrementDelayedCounters node, Context context)
		{
			StringBuilder ret = new StringBuilder();

			ret.AppendFormat("for ({0} = 0; i < {1}; ++i)", DeclareValueVariable("int", "i", context), node.Counters.Count);
			ret.AppendLine();
			ret.AppendLine("{");
			ret.AppendFormat("\tif ({0}[i] == {1}[i] - 1)", context.This(node.Counters.Name), context.This(node.CountersSize.Name));
			ret.AppendLine();
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t{0}[i] = 0;", context.This(node.Counters.Name));
			ret.AppendLine();
			ret.AppendLine("\t}");
			ret.AppendLine("\telse");
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t++{0}[i];", context.This(node.Counters.Name));
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
						                 context.This(table.Name), r, c,
						                 sidx.Value,
						                 context.This(context.Program.DelayedCounters.Name), idx.DataIndex);
					}
				}
				
				seen[table] = true;
			}
			
			return ret.ToString();
		}
		
		protected virtual string TranslateDelayAssignment(Computation.Assignment node, Context context)
		{
			string eq = InstructionTranslator.QuickTranslate((Context)context.Base().Push(node.State, node.Equation));

			StringBuilder ret = new StringBuilder();
			
			var ds = (DelayedState)node.State;

			uint size = (uint)System.Math.Round(ds.Delay / Cdn.RawC.Options.Instance.DelayTimeStep);
			DataTable.DataItem counter = context.Program.DelayedCounters[new DelayedState.Size(size)];
			DataTable table = context.Program.DelayHistoryTable(ds);

			ret.AppendFormat("{0}[{1}] = {2}[{3}[{4}]];",
			                 context.This(node.Item.Table.Name),
			                 node.Item.AliasOrIndex,
			                 context.This(table.Name),
			                 context.This(context.Program.DelayedCounters.Name),
			                 counter.DataIndex);

			ret.AppendLine();
			ret.AppendFormat("{0}[{1}[{2}]] = {3};",
			                 context.This(table.Name),
			                 context.This(context.Program.DelayedCounters.Name),
			                 counter.DataIndex,
				eq);

			return ret.ToString();
		}

		protected virtual string DeclareArrayVariable(string type, string name, int size, Context context)
		{
			return String.Format("{0} {1}[{2}]",
				               DeclareValueVariable("ValueType", name, context),
				               size);
		}

		private string WriteWithTemporaryStorage(Context ctx, string ret)
		{
			if (ctx.TemporaryStorage.Count == 0)
			{
				return ret;
			}

			StringBuilder s = new StringBuilder();

			s.AppendLine(BeginBlock);

			foreach (var tmp in ctx.TemporaryStorage)
			{
				s.AppendFormat("\t{0};",
				               DeclareArrayVariable("ValueType", tmp.Name, tmp.Size, ctx));

				s.AppendLine();
			}

			s.AppendLine(Context.Reindent(ret, "\t"));
			s.AppendLine(EndBlock);

			return s.ToString();
		}

		private string TranslateAssignment(string index, Context ctx)
		{
			string eq;
			string ret;

			var slice = ctx.Node.Slice;

			// Compute the equation
			if (ctx.Node.Dimension.IsOne)
			{
				eq = InstructionTranslator.QuickTranslate(ctx);

				if (slice != null && slice[0] != 0)
				{
					ret = String.Format("{0}[{1} + {2}] = {3};",
					                    ctx.This(ctx.Program.StateTable.Name),
					                    index,
					                    slice[0],
					                    eq);
				}
				else
				{
					ret = String.Format("{0}[{1}] = {2};",
					                    ctx.This(ctx.Program.StateTable.Name),
					                    index,
					                    eq);
				}
			}
			else
			{
				string retval;

				if (ctx.SupportsPointers && (slice == null || Context.IndicesAreContinuous(slice)))
				{
					if (slice != null)
					{
						retval = String.Format("{0} + {1} + {2}",
						                       ctx.This(ctx.Program.StateTable.Name),
						                       index,
						                       slice[0]);
						slice = null;
					}
					else
					{
						retval = String.Format("{0} + {1}",
						                       ctx.This(ctx.Program.StateTable.Name),
						                       index);
					}
				}
				else
				{
					// Make temporary first
					retval = ctx.AcquireTemporary(ctx.Node);
					slice = null;
				}

				ctx.PushRet(retval);
				ret = InstructionTranslator.QuickTranslate(ctx);
				ctx.PopRet();

				if (slice != null)
				{
					// Implies that pointers are supported
					StringBuilder sret = new StringBuilder("(");
					sret.Append(ret);
					
					// Copy temporary to slice
					for (int i = 0; i < slice.Length; ++i)
					{
						sret.AppendFormat(", {0}[{1} + {2}] = {3}[{4}]",
						                  ctx.This(ctx.Program.StateTable.Name),
						                  index,
						                  slice[i],
						                  retval,
						                  i);
					}

					sret.Append(");");
					ret = sret.ToString();
				}
				else
				{
					ret = String.Format("{0};", ret);
				}
			}

			return WriteWithTemporaryStorage(ctx, ret.ToString());
		}
		
		protected virtual string Translate(Computation.Assignment node, Context context)
		{
			if (node.State is DelayedState)
			{
				return TranslateDelayAssignment(node, context);
			}
			
			var ctx = (Context)context.Base();
			ctx.Push(node.State, node.Equation);

			return TranslateAssignment(node.Item.AliasOrIndex, ctx);
		}

		protected virtual string Translate(Computation.ZeroMemory node, Context context)
		{
			if (node.Name == null)
			{
				throw new Exception("Unsupported zero memory for network");
			}
			else if (node.DataTable == null)
			{
				// TODO
				throw new Exception("Unsupported stuff");
			}
			else if (node.DataTable.IntegerType)
			{
				return context.MemZero(context.This(node.DataTable.Name),
				                       "0",
				                       node.DataTable.MaxSizeTypeName,
				                       node.DataTable.Size);
			}
			else
			{
				return context.MemZero(context.This(node.DataTable.Name),
				                       "0",
				                       "ValueType",
				                       node.DataTable.Size);
			}
		}
		
		protected virtual string Translate(Computation.CopyTable node, Context context)
		{
			string target = context.This(node.Target.Name);
			string source = context.This(node.Source.Name);

			if (node.Size > 0)
			{
				return context.MemCpy(target,
				                      node.TargetIndex.ToString(),
				                      source,
				                      node.SourceIndex.ToString(),
				                      "ValueType",
				                      node.Size);
			}
			else if (node.Size < 0 && node.Source.Count > 0)
			{
				return context.MemCpy(target,
				                      node.TargetIndex.ToString(),
				                      source,
				                      node.SourceIndex.ToString(),
				                      "ValueType",
				                      node.Source.Count - node.SourceIndex);
			}
			else
			{
				return String.Format("{0} No constants to copy {1}", BeginComment, EndComment);
			}
		} 
		
		protected virtual string Translate(Computation.Empty node, Context context)
		{
			return "";
		}

		protected virtual string BeginComment
		{
			get { return "/*"; }
		}

		protected virtual string EndComment
		{
			get { return "*/"; }
		}
		
		protected virtual string Translate(Computation.Comment node, Context context)
		{
			return String.Format("{0} {1} {2}", BeginComment, node.Text, EndComment);
		}
	}
}