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

		protected virtual string Translate(Computation.Block node, Context context)
		{
			var ret = new StringBuilder();

			ret.AppendLine(context.BeginBlock);

			for (int i = 0; i < node.Body.Count; ++i)
			{
				var child = node.Body[i];

				if (i != node.Body.Count - 1 || !(child is Computation.Empty))
				{
					ret.AppendLine(Context.Reindent(Translate(child, context), "\t"));
				}
			}

			ret.AppendLine(context.EndBlock);

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
				                             context.This(context.Program.EventStatesTable),
				                             container.Index,
				                             idx));
			}

			var cond = String.Join(" || ", conditions);

			ret.AppendLine(context.BeginBlock);
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

			if (node.Else.Count != 0)
			{
				ret.AppendLine("\telse");
				ret.AppendLine("\t{");

				for (int i = 0; i < node.Else.Count; ++i)
				{
					var child = node.Else[i];
	
					if (i != node.Else.Count - 1 || !(child is Computation.Empty))
					{
						ret.AppendLine(Context.Reindent(Translate(child, context), "\t\t"));
					}
				}

				ret.AppendLine("\t}");
			}

			ret.Append(context.EndBlock);

			return ret.ToString();
		}

		protected virtual string Translate(Computation.CallAPI node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			bool isfirst = true;

			ret.AppendFormat("{0}(", context.APIName(node));

			if (context.This("") == "")
			{
				ret.Append("data");
				isfirst = false;
			}

			for (int i = 0; i < node.Arguments.Length; ++i)
			{
				if (!isfirst)
				{
					ret.Append(", ");
				}
				else
				{
					isfirst = false;
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
			
			Context ctx = context.Clone(context.Program, context.Options, node.Expression, node.Mapping);

			ret.AppendFormat("for ({0} = 0; i < {1}; ++i)",
			                 context.DeclareValueVariable("int", "i"),
			                 node.Items.Count);

			ret.AppendLine();
			ret.AppendLine("{");

			if (Cdn.RawC.Options.Instance.Verbose)
			{
				var dt = (node.IndexTable[0].Object as Computation.Loop.Index).DataItem;

				ret.AppendFormat("\t{0} {1}[{2}] = {3}({4}",
				                 context.BeginComment,
				                 context.This(context.Program.StateTable),
				                 dt.AliasOrIndex.Replace(context.BeginComment, "//").Replace(context.EndComment, "//"),
				                 context.ThisCall(node.Function.Name),
				                 context.This(context.Program.StateTable));

				var eq = node.Items[0].Equation;

				foreach (Tree.Embedding.Argument arg in node.Function.OrderedArguments)
				{
					Tree.Node subnode = eq.FromPath(arg.Path);
					DataTable.DataItem it = context.Program.StateTable[subnode];
				
					ret.AppendFormat(", {0}[{1}]",
					                 context.This(context.Program.StateTable),
					                 it.AliasOrIndex.Replace(context.BeginComment, "//").Replace(context.EndComment, "//"));
				}

				ret.Append("); ");
				ret.Append(context.EndComment);
				ret.AppendLine();
			}

			ret.AppendLine(Context.Reindent(TranslateAssignment(String.Format("{0}[i][0]",
			                                                                  context.This(node.IndexTable)),
			                                                    ctx),
			                                "\t"));
			ret.Append("}");
			
			return ret.ToString();
		}

		protected virtual string Translate(Computation.InitializeDelayHistory node, Context context)
		{
			StringBuilder ret = new StringBuilder();
			string eq = InstructionTranslator.QuickTranslate((Context)context.Base().Push(node.State, node.Equation));

			if (node.State.Operator.InitialValue == null)
			{
				ret.AppendLine(Translate(new Computation.ZeroMemory(node.History), context));

				ret.AppendFormat("{0}[{1}] = 0.0;",
				                 context.This(context.Program.StateTable),
				                 context.Program.StateTable[node.State].AliasOrIndex);

				return ret.ToString();
			}
			else if (!node.OnTime)
			{
				ret.AppendLine(context.BeginBlock);
				ret.AppendFormat("\t{0} = {1};",
				                 context.DeclareValueVariable("ValueType", "_tmp"),
				                 eq);
				ret.AppendLine();
				ret.AppendLine();

				ret.AppendFormat("\tfor ({0} = 0; i < {0}; ++i)",
				                 context.DeclareValueVariable("int", "i"),
				                 node.History.Count);

				ret.AppendLine();
				ret.AppendLine("\t{");
				ret.AppendFormat("\t\t{0}[i] = _tmp;",
				                 context.This(node.History));
				ret.AppendLine();
				ret.AppendLine("\t}");
				ret.AppendLine();
				ret.AppendFormat("\t{0}[{1}] = _tmp;",
				                 context.This(context.Program.StateTable),
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
			ret.AppendFormat("for ({0} = 0; i < {1}; ++i)",
			                 context.DeclareValueVariable("int", "i"),
			                 node.History.Count + 1);
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
			ret.AppendFormat("\t\t{0}[i] = {1};", context.This(node.History), eq);
			ret.AppendLine();
			ret.AppendLine("\t}");
			ret.AppendLine("\telse");
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t{0}[{1}] = {2};",
			                 context.This(context.Program.StateTable),
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

			ret.AppendFormat("for ({0} = 0; i < {1}; ++i)",
			                 context.DeclareValueVariable("int", "i"),
			                 node.Counters.Count);

			ret.AppendLine();
			ret.AppendLine("{");
			ret.AppendFormat("\tif ({0}[i] == {1}[i] - 1)", context.This(node.Counters), context.This(node.CountersSize));
			ret.AppendLine();
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t{0}[i] = 0;", context.This(node.Counters));
			ret.AppendLine();
			ret.AppendLine("\t}");
			ret.AppendLine("\telse");
			ret.AppendLine("\t{");
			ret.AppendFormat("\t\t++{0}[i];", context.This(node.Counters));
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
						                 context.This(table), r, c,
						                 sidx.Value,
						                 context.This(context.Program.DelayedCounters), idx.DataIndex);
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
			                 context.This(node.Item.Table),
			                 node.Item.AliasOrIndex,
			                 context.This(table),
			                 context.This(context.Program.DelayedCounters),
			                 counter.DataIndex);

			ret.AppendLine();
			ret.AppendFormat("{0}[{1}[{2}]] = {3};",
			                 context.This(table),
			                 context.This(context.Program.DelayedCounters),
			                 counter.DataIndex,
				eq);

			return ret.ToString();
		}

		private string WriteWithTemporaryStorage(Context ctx, string ret)
		{
			if (ctx.TemporaryStorage.Count == 0)
			{
				return ret;
			}

			StringBuilder s = new StringBuilder();

			s.AppendLine(ctx.BeginBlock);

			foreach (var tmp in ctx.TemporaryStorage)
			{
				s.AppendFormat("\t{0};",
				               ctx.DeclareArrayVariable("ValueType", tmp.Name, tmp.Size));

				s.AppendLine();
			}

			s.AppendLine(Context.Reindent(ret, "\t"));
			s.AppendLine(ctx.EndBlock);

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

				if (slice != null && ctx.SupportsFirstClassArrays)
				{
					ret = String.Format("{0}[{1}][{2}] = {3};",
					                    ctx.This(ctx.Program.StateTable),
					                    index,
					                    slice[0],
					                    eq);
				}
				else if (slice != null && slice[0] != 0)
				{
					ret = String.Format("{0}[{1} + {2}] = {3};",
					                    ctx.This(ctx.Program.StateTable),
					                    index,
					                    slice[0],
					                    eq);
				}
				else
				{
					ret = String.Format("{0}[{1}] = {2};",
					                    ctx.This(ctx.Program.StateTable),
					                    index,
					                    eq);
				}
			}
			else
			{
				string retval = null;
				bool hastmp = false;
				bool contidx = (slice != null && Context.IndicesAreContinuous(slice));

				if (ctx.SupportsPointers)
				{
					if (slice != null)
					{
						// If a slice is continous, than just have the offset
						// into the state table and write there directly
						if (contidx)
						{
							retval = String.Format("{0} + {1} + {2}",
							                       ctx.This(ctx.Program.StateTable),
							                       index,
							                       slice[0]);

						}
						else
						{
							// No magic, here we allocate a temporary to write
							// to which will afterwards be copied to the slice
							retval = ctx.AcquireTemporary(ctx.Node);
							hastmp = true;
						}
					}
					else
					{
						// No slice, just write directly into the state table
						retval = String.Format("{0} + {1}",
						                       ctx.This(ctx.Program.StateTable),
						                       index);
					}
				}
				else if (!ctx.SupportsFirstClassArrays || (slice != null && !contidx))
				{
					// For now, just make a temporary and copy back afterwards
					retval = ctx.AcquireTemporary(ctx.Node);
					hastmp = true;
				}

				ctx.PushRet(retval);
				ret = InstructionTranslator.QuickTranslate(ctx);
				ctx.PopRet();

				if (hastmp)
				{
					if (slice != null && !contidx)
					{
						StringBuilder sret = new StringBuilder("(");

						if (ctx.SupportsFirstClassArrays)
						{
							sret.AppendFormat("{0} = ", retval);
						}

						sret.Append(ret);
						
						// Copy temporary to slice
						for (int i = 0; i < slice.Length; ++i)
						{
							string writeloc;

							if (ctx.SupportsFirstClassArrays)
							{
								writeloc = String.Format("[{0}][{1}]",
								                         index,
								                         slice[i]);
							}
							else
							{
								writeloc = String.Format("[{0} + {1}]",
								                         index,
								                         slice[i]);
							}

							sret.AppendFormat(", {0}{1} = {2}[{3}]",
							                  ctx.This(ctx.Program.StateTable),
							                  writeloc,
							                  retval,
							                  i);
						}
	
						sret.Append(");");
						ret = sret.ToString();
					}
					else
					{
						StringBuilder sret = new StringBuilder("(");
						sret.Append(ret);
						sret.Append(", ");

						if (slice == null)
						{
							// Just copy the whole thing
							sret.Append(ctx.MemCpy(ctx.This(ctx.Program.StateTable),
							                 index,
							                 retval,
							                 "0",
							                 "ValueType",
							                 ctx.Node.Dimension.Size()));
						}
						else
						{
							// We have a slice, but it has continous indices
							sret.Append(ctx.MemCpy(ctx.This(ctx.Program.StateTable),
							                 index,
							                 retval,
							                 slice[0].ToString(),
							                 "ValueType",
							                 slice[slice.Length - 1] - slice[0]));
						}

						sret.Append(");");
						ret = sret.ToString();
					}
				}
				else if (retval == null)
				{
					if (slice != null)
					{
						// Memcpy to the slice
						ret = ctx.MemCpy(String.Format("{0}[{1}]",
						                               ctx.This(ctx.Program.StateTable),
						                               index),
						                 slice[0].ToString(),
						                 ret,
						                 "0",
						                 "ValueType",
						                 slice.Length) + ";";
					}
					else
					{
						// Simply assign
						ret = String.Format("{0}[{1}] = {2};",
						                    ctx.This(ctx.Program.StateTable),
						                    index,
						                    ret);
					}
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
				// Assignments to delayed states are handled differently
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
				throw new Exception("Unsupported zero memory for network. The formatter should implement the Translate(Computation.ZeroMemory node, CLike.Context context) on its ComputationNodeTranslator...");
			}
			else if (node.DataTable == null)
			{
				// TODO
				throw new Exception("Unsupported stuff");
			}
			else
			{
				return context.MemZero(context.This(node.DataTable),
				                       "0",
				                       node.DataTable.TypeName,
				                       node.DataTable.Size) + ";";
			}
		}
		
		protected virtual string Translate(Computation.CopyTable node, Context context)
		{
			string target = context.This(node.Target);
			string source = context.This(node.Source);

			if (node.Size > 0)
			{
				return context.MemCpy(target,
				                      node.TargetIndex.ToString(),
				                      source,
				                      node.SourceIndex.ToString(),
				                      node.Source.TypeName,
				                      node.Size) + ";";
			}
			else if (node.Size < 0 && node.Source.Count > 0)
			{
				return context.MemCpy(target,
				                      node.TargetIndex.ToString(),
				                      source,
				                      node.SourceIndex.ToString(),
				                      node.Source.TypeName,
				                      node.Source.Count - node.SourceIndex) + ";";
			}
			else
			{
				return String.Format("{0} No constants to copy {1}", context.BeginComment, context.EndComment);
			}
		} 
		
		protected virtual string Translate(Computation.Empty node, Context context)
		{
			return "";
		}

		protected virtual string Translate(Computation.Comment node, Context context)
		{
			return String.Format("{0} {1} {2}", context.BeginComment, node.Text, context.EndComment);
		}
	}
}