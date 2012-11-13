using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class InstructionTranslator : DynamicVisitor
	{
		class OperatorSpec
		{
			public Cdn.MathFunctionType Type;
			public int Priority;
			public bool LeftAssociation;
			
			public OperatorSpec(Cdn.MathFunctionType type, int priority, bool leftAssociation)
			{
				Type = type;
				Priority = priority;
				LeftAssociation = leftAssociation;
			}
		}

		private static Dictionary<Cdn.MathFunctionType, OperatorSpec> s_operatorSpecs;
		
		private static void AddSpec(Cdn.MathFunctionType type, int priority, bool leftAssociation)
		{
			s_operatorSpecs[type] = new OperatorSpec(type, priority, leftAssociation);
		}
		
		static InstructionTranslator()
		{
			s_operatorSpecs = new Dictionary<MathFunctionType, OperatorSpec>();
			
			AddSpec(MathFunctionType.Multiply, 7, true);
			AddSpec(MathFunctionType.Divide, 7, true);
			AddSpec(MathFunctionType.Modulo, 7, true);
			AddSpec(MathFunctionType.Plus, 6, true);
			AddSpec(MathFunctionType.Minus, 6, true);
			AddSpec(MathFunctionType.UnaryMinus, 8, false);
			
			AddSpec(MathFunctionType.Negate, 8, false);
			AddSpec(MathFunctionType.Greater, 5, true);
			AddSpec(MathFunctionType.Less, 5, true);
			AddSpec(MathFunctionType.GreaterOrEqual, 5, true);
			AddSpec(MathFunctionType.LessOrEqual, 5, true);
			AddSpec(MathFunctionType.Equal, 4, true);
			AddSpec(MathFunctionType.Or, 2, true);
			AddSpec(MathFunctionType.And, 3, true);
			
			AddSpec(MathFunctionType.Ternary, 1, false);
		}

		public InstructionTranslator() : base(typeof(string),
		                                      BindingFlags.Default,
		                                      System.Reflection.BindingFlags.Default |
		                                      System.Reflection.BindingFlags.NonPublic |
		                                      System.Reflection.BindingFlags.Public |
		                                      System.Reflection.BindingFlags.Instance |
		                                      System.Reflection.BindingFlags.InvokeMethod,
		                                      a => a.Name == "Translate" || a.Name == "TranslateV",
		                                      typeof(Instruction),
		                                      typeof(Context))
		{
		}
		
		public static string QuickTranslate(Context context)
		{
			return (new InstructionTranslator()).Translate(context);
		}

		private bool NodeIsOne(Tree.Node node)
		{
			if (!node.Dimension.IsOne)
			{
				return false;
			}
			else
			{
				foreach (var child in node.Children)
				{
					if (!child.Dimension.IsOne)
					{
						return false;
					}
				}
			}

			return true;
		}
		
		private string TranslateAssign(Context context, Tree.Node node, string assignto)
		{
			if (node.Dimension.IsOne)
			{
				return String.Format("*({0}) = {1}", assignto, Translate(context, node));
			}
			else
			{
				context.PushRet(assignto);
				var ret = Translate(context, node);
				context.PopRet();
				
				return ret;
			}
		}
		
		private string Translate(Context context)
		{
			string ret;
			
			if (context.TryMapping(context.Node, out ret))
			{
				return ret;
			}

			InvokeSelector sel;

			if (NodeIsOne(context.Node))
			{
				sel = a => a.Name == "Translate";
			}
			else
			{
				sel = a => a.Name == "TranslateV";
			}

			return InvokeSelect<string>(sel, context.Node.Instruction, context);
		}

		public string Translate(Context context, Tree.Node child)
		{
			string ret;

			context.Push(child);
			ret = Translate(context);
			context.Pop();

			return ret;
		}
		
		public string Translate(Context context, int child)
		{
			return Translate(context, context.Node.Children[child]);
		}

		private bool HasPriority(Cdn.MathFunctionType a, Cdn.MathFunctionType b)
		{
			OperatorSpec s1;
			OperatorSpec s2;
			
			if (!s_operatorSpecs.TryGetValue(a, out s1) || !s_operatorSpecs.TryGetValue(b, out s2))
			{
				return false;
			}
			
			return s1.Priority >= s2.Priority;
		}

		private string SimpleOperator(Context context, InstructionFunction inst, string glue)
		{
			int num = context.Node.Children.Count;
			
			if (num == 1)
			{
				return String.Format("{0}{1}", glue, Translate(context, 0)).Trim();
			}

			string[] args = new string[num];
			
			for (int i = 0; i < num; ++i)
			{
				args[i] = Translate(context, i);
			}
			
			bool needsparen = false;
			
			if (context.Node.Parent != null)
			{
				InstructionFunction op = context.Node.Parent.Instruction as InstructionFunction;
				
				needsparen = inst == null || (op != null && HasPriority((Cdn.MathFunctionType)op.Id, (Cdn.MathFunctionType)inst.Id));
			}
			
			if (!needsparen)
			{
				return String.Join(glue, args).Trim();
			}
			else
			{
				return String.Format("({0})", String.Join(glue, args).Trim());
			}
		}
		
		private string Translate(InstructionNumber instruction, Context context)
		{
			if (instruction.Representation.ToLower() == "pi")
			{
				return "M_PI";
			}
			else if (instruction.Representation.ToLower() == "e")
			{
				return "M_E";
			}

			return NumberTranslator.Translate(instruction.Value, context);
		}
		
		private string TranslateOperator(InstructionFunction instruction, Context context)
		{
			switch ((Cdn.MathFunctionType)instruction.Id)
			{
			case MathFunctionType.And:
				return SimpleOperator(context, instruction, " && ");
			case MathFunctionType.Divide:
				return SimpleOperator(context, instruction, " / ");
			case MathFunctionType.Equal:
				return SimpleOperator(context, instruction, " == ");
			case MathFunctionType.Greater:
				return SimpleOperator(context, instruction, " > ");
			case MathFunctionType.GreaterOrEqual:
				return SimpleOperator(context, instruction, " >= ");
			case MathFunctionType.Less:
				return SimpleOperator(context, instruction, " < ");
			case MathFunctionType.LessOrEqual:
				return SimpleOperator(context, instruction, " <= ");
			case MathFunctionType.Minus:
				return SimpleOperator(context, instruction, " - ");
			case MathFunctionType.UnaryMinus:
				return SimpleOperator(context, instruction, " -");
			case MathFunctionType.Multiply:
				return SimpleOperator(context, instruction, " * ");
			case MathFunctionType.Negate:
				return SimpleOperator(context, instruction, " !");
			case MathFunctionType.Or:
				return SimpleOperator(context, instruction, " || ");
			case MathFunctionType.Plus:
				return SimpleOperator(context, instruction, " + ");
			case MathFunctionType.Modulo:
				return String.Format("{0}{1}",
					                     Context.MathFunctionDefine(Cdn.MathFunctionType.Modulo, context.Node.Children.Count),
					                     SimpleOperator(context, null, ", "));
			case MathFunctionType.Power:
				return String.Format("{0}{1}",
					                     Context.MathFunctionDefine(Cdn.MathFunctionType.Pow, context.Node.Children.Count),
					                     SimpleOperator(context, null, ", "));
			case MathFunctionType.Ternary:
				return String.Format("({0} ? {1} : {2})",
					                     Translate(context, 0),
					                     Translate(context, 1),
					                     Translate(context, 2));
			}
			
			throw new NotImplementedException(String.Format("The operator `{0}' is not implemented", instruction.Name));
		}
		
		private string Translate(InstructionVariable instruction, Context context)
		{
			Cdn.Variable prop = instruction.Variable;
			
			if (!context.Program.StateTable.Contains(prop))
			{
				throw new NotImplementedException(String.Format("The variable `{0}' is not implemented", prop.FullName));
			}
			
			DataTable.DataItem item = context.Program.StateTable[prop];
			return String.Format("{0}[{1}]", context.Program.StateTable.Name, item.AliasOrIndex);
		}

		private string Translate(InstructionRand instruction, Context context)
		{
			string val;

			if (context.Program.StateTable.Contains(instruction))
			{
				var item = context.Program.StateTable[instruction];
				val = String.Format("{0}[{1}]", context.Program.StateTable.Name, item.AliasOrIndex);
			}
			else
			{
				val = "CDN_MATH_RAND ()";
			}

			return val;
		}
		
		private string Translate(InstructionFunction instruction, Context context)
		{
			if (instruction.Id < (uint)Cdn.MathFunctionType.NumOperators)
			{
				return TranslateOperator(instruction, context);
			}

			string name = Context.MathFunctionDefine(instruction);
			string[] args = new string[instruction.GetStackManipulation().Pop.Num];
			
			for (int i = 0; i < instruction.GetStackManipulation().Pop.Num; ++i)
			{
				args[i] = Translate(context, i);
			}
						
			if (Math.FunctionIsVariable((Cdn.MathFunctionType)instruction.Id))
			{
				string ret = "";

				// This does not work in the general case, but anyway
				for (int i = args.Length - 2; i >= 0; --i)
				{
					if (i != args.Length - 2)
					{
						ret = String.Format("{0} ({1}, {2})", name, args[i], ret);
					}
					else
					{
						ret = String.Format("{0} ({1}, {2})", name, args[i], args[i + 1]);
					}
				}
				
				return ret;
			}
			
			return String.Format("{0} ({1})", name, String.Join(", ", args));
		}
		
		private string Translate(InstructionCustomOperator instruction, Context context)
		{
			OperatorDelayed delayed;
			
			delayed = instruction.Operator as OperatorDelayed;
			
			if (delayed == null)
			{
				throw new NotSupportedException(String.Format("The custom operator `{0}' is not yet implemented in rawc...", instruction.Operator.Name));
			}

			double delay;

			if (!Knowledge.Instance.LookupDelay(instruction, out delay))
			{
				throw new NotSupportedException("Unable to determine delay of delayed operator");
			}
			
			DataTable.DataItem item = context.Program.StateTable[new DelayedState.Key(delayed, delay)];

			return String.Format("{0}[{1}]",
			                     context.Program.StateTable.Name,
			                     item.AliasOrIndex);
		}
		
		private string Translate(Instructions.Function instruction, Context context)
		{
			string name = instruction.FunctionCall.Name.ToUpper();
			List<string > args = new List<string>();

			args.Add(context.Program.StateTable.Name);

			if (!instruction.FunctionCall.IsCustom)
			{
				foreach (Tree.Embedding.Argument argument in instruction.FunctionCall.OrderedArguments)
				{
					args.Add(Translate(context, context.Node.FromPath(argument.Path)));
				}
			}
			else
			{
				foreach (Tree.Node child in context.Node.Children)
				{
					args.Add(Translate(context, child));
				}
			}

			return String.Format("{0} ({1})", name, String.Join(", ", args.ToArray()));
		}
		
		private string Translate(Instructions.State instruction, Context context)
		{
			return String.Format("{0}[{1}]", instruction.Item.Table.Name, instruction.Item.AliasOrIndex);
		}
		
		private string Translate(Instructions.Variable instruction, Context context)
		{
			return instruction.Name;
		}
		
		private bool InstructionHasStorage(Cdn.Instruction instruction, Context context)
		{
			var v = instruction as Cdn.InstructionVariable;
			
			if (v != null)
			{
				return true;
			}
			
			return context.Program.StateTable.Contains(instruction);
		}
		
		/* Translators for multidimension values */
		private string TranslateV(Cdn.InstructionMatrix instruction, Context context)
		{
			string[] args = new string[context.Node.Children.Count];
			int argi = 0;
			
			var tmp = context.PeekRet();

			for (int i = 0; i < context.Node.Children.Count; ++i)
			{
				var child = context.Node.Children[i];
				
				// Translate such that we compute the result of the child
				// at the tmp + argi location
				if (argi == 0)
				{
					args[i] = TranslateAssign(context, child, String.Format("{0}", tmp));
				}
				else
				{
					args[i] = TranslateAssign(context, child, String.Format("{0} + {1}", tmp, argi));
				}	

				argi += child.Dimension.Size();
			}
			
			return String.Format("({0}, {1})", String.Join(",\n ", args), tmp);
		}

		private string TranslateChildV(Tree.Node child, Context context)
		{
			string ret;

			if (child.Dimension.IsOne || InstructionHasStorage(child.Instruction, context))
			{
				context.PushRet(null);
				ret = Translate(context, child);
				context.PopRet();
			}
			else
			{
				context.PushRet(context.AcquireTemporary(child));
				ret = Translate(context, child);
				context.PopRet();
			}

			return ret;
		}
		
		private string TranslateV(InstructionFunction instruction, Context context)
		{
			Cdn.MathFunctionType type;
			
			type = (Cdn.MathFunctionType)instruction.Id;
			
			var def = Context.MathFunctionDefineV(type, instruction.GetStackManipulation());
			string ret = null;

			if (!context.Node.Dimension.IsOne)
			{
				ret = context.PeekRet();
			}
			
			Context.MathDefines.Add(def);
			
			List<string> args = new List<string>(context.Node.Children.Count + 1);

			int cnt = 0;
			context.SaveTemporaryStack();
			
			for (int i = 0; i < context.Node.Children.Count; ++i)
			{
				var child = context.Node.Children[i];

				args.Add(TranslateChildV(child, context));

				var s = child.Dimension.Size();

				if (s > cnt)
				{
					cnt = s;
				}
			}

			context.RestoreTemporaryStack();

			switch (type)
			{
			case MathFunctionType.Transpose:
			{
				var dim = context.Node.Children[0].Dimension;

				args.Add(dim.Rows.ToString());
				args.Add(dim.Columns.ToString());
			}
				break;
			case MathFunctionType.Hcat:
			{
				var dim1 = context.Node.Children[0].Dimension;
				var dim2 = context.Node.Children[1].Dimension;

				args.Add(dim1.Rows.ToString());
				args.Add(dim1.Columns.ToString());
				args.Add(dim2.Columns.ToString());
			}
				break;
			case MathFunctionType.Multiply:
				if (def == "CDN_MATH_MATRIX_MULTIPLY_V")
				{
					var d1 = context.Node.Children[0].Dimension;
					var d2 = context.Node.Children[1].Dimension;

					args.Add(d1.Rows.ToString());
					args.Add(d1.Columns.ToString());
					args.Add(d2.Columns.ToString());
				}
				else
				{
					args.Add(cnt.ToString());
				}
				break;
			case MathFunctionType.Index:
				break;
			default:
				args.Add(cnt.ToString());
				break;
			}

			if (ret != null)
			{
				return String.Format("{0} ({1}, {2})", def, ret, String.Join(", ", args));
			}
			else
			{
				return String.Format("{0} ({1})", def, String.Join(", ", args));
			}
		}
		
		private string TranslateV(InstructionVariable instruction, Context context)
		{
			Cdn.Variable prop = instruction.Variable;
			
			if (!context.Program.StateTable.Contains(prop))
			{
				throw new NotImplementedException(String.Format("The variable `{0}' is not implemented", prop.FullName));
			}
			
			DataTable.DataItem item = context.Program.StateTable[prop];
			var ret = context.PeekRet();
			
			if (ret != null)
			{
				return String.Format("(memcpy ({0}, {1} + {2}, sizeof(ValueType) * {3}), {0})",
				                     ret,
				                     context.Program.StateTable.Name,
				                     item.AliasOrIndex,
				                     prop.Dimension.Size());
			}
			else if (item.DataIndex == 0)
			{
				return String.Format("{0}", context.Program.StateTable.Name);
			}
			else
			{
			
				return String.Format("({0} + {1})", context.Program.StateTable.Name, item.AliasOrIndex);
			}
		}

		private string TranslateV(Instructions.Function instruction, Context context)
		{
			string name = instruction.FunctionCall.Name.ToUpper();
			List<string > args = new List<string>();

			var ret = context.PeekRet();

			args.Add(context.Program.StateTable.Name);
			args.Add(ret);

			context.SaveTemporaryStack();

			if (!instruction.FunctionCall.IsCustom)
			{
				foreach (Tree.Embedding.Argument argument in instruction.FunctionCall.OrderedArguments)
				{
					args.Add(TranslateChildV(context.Node.FromPath(argument.Path), context));
				}
			}
			else
			{
				foreach (Tree.Node child in context.Node.Children)
				{
					args.Add(TranslateChildV(child, context));
				}
			}

			context.RestoreTemporaryStack();

			return String.Format("{0} ({1})", name, String.Join(", ", args.ToArray()));
		}
	}
}

