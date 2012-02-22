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
		                                      new Type[] {typeof(Instruction), typeof(Context)})
		{
		}
		
		public static string QuickTranslate(Context context)
		{
			return (new InstructionTranslator()).Translate(context);
		}
		
		private string Translate(Context context)
		{
			string ret;
			
			if (context.TryMapping(context.Node, out ret))
			{
				return ret;
			}

			return Invoke<string>(context.Node.Instruction, context);
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

			return NumberTranslator.Translate(instruction.Value);
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
			
			if ((context.State == null || (context.State.Type & (State.Flags.Initialization | State.Flags.AfterIntegrated)) == 0) && context.Program.IntegrateTable.ContainsKey(item))
			{
				// Instead use the conserved state
				item = context.Program.StateTable[context.Program.IntegrateTable[item]];
			}

			return String.Format("{0}[{1}]", context.Program.StateTable.Name, item.AliasOrIndex);
		}

		private string Translate(InstructionRand instruction, Context context)
		{
			int numpop = instruction.GetStackManipulation().NumPop;

			string name = String.Format("CDN_MATH_RAND{0}", numpop);

			string[] args = new string[instruction.GetStackManipulation().NumPop];
			
			for (int i = 0; i < instruction.GetStackManipulation().NumPop; ++i)
			{
				args[i] = Translate(context, i);
			}
			
			return String.Format("{0} ({1})", name, String.Join(", ", args));
		}
		
		private string Translate(InstructionFunction instruction, Context context)
		{
			if (instruction.Id < (uint)Cdn.MathFunctionType.NumOperators)
			{
				return TranslateOperator(instruction, context);
			}

			string name = Context.MathFunctionDefine(instruction);
			string[] args = new string[instruction.GetStackManipulation().NumPop];
			
			for (int i = 0; i < instruction.GetStackManipulation().NumPop; ++i)
			{
				args[i] = Translate(context, i);
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

			if (!Knowledge.Instance.Delays.TryGetValue(delayed, out delay))
			{
				throw new NotSupportedException("Enable to determine delay of delayed operator");
			}
			
			uint size = (uint)System.Math.Round(delay / Cdn.RawC.Options.Instance.DelayTimeStep) + 1;

			DataTable.DataItem item = context.Program.StateTable[new DelayedState.Key(delayed, delay)];
			DataTable.DataItem counter = context.Program.DelayedCounters[new DelayedState.Size(size)];
				
			return String.Format("{0}[{1} + {2}[{3}]]",
			                     context.Program.StateTable.Name,
			                     item.AliasOrIndex,
			                     context.Program.DelayedCounters.Name,
			                     counter.Index);
		}
		
		private string Translate(Instructions.Function instruction, Context context)
		{
			string name = instruction.FunctionCall.Name.ToUpper();
			List<string > args = new List<string>();

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
	}
}

