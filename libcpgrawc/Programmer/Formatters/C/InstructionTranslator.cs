using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer.Formatters.C
{
	public class InstructionTranslator : DynamicVisitor
	{
		class OperatorSpec
		{
			public Cpg.MathOperatorType Type;
			public int Priority;
			public bool LeftAssociation;
			
			public OperatorSpec(Cpg.MathOperatorType type, int priority, bool leftAssociation)
			{
				Type = type;
				Priority = priority;
				LeftAssociation = leftAssociation;
			}
		}

		private static Dictionary<Cpg.MathOperatorType, OperatorSpec> s_operatorSpecs;
		
		private static void AddSpec(Cpg.MathOperatorType type, int priority, bool leftAssociation)
		{
			s_operatorSpecs[type] = new OperatorSpec(type, priority, leftAssociation);
		}
		
		static InstructionTranslator()
		{
			s_operatorSpecs = new Dictionary<MathOperatorType, OperatorSpec>();
			
			AddSpec(MathOperatorType.Multiply, 7, true);
			AddSpec(MathOperatorType.Divide, 7, true);
			AddSpec(MathOperatorType.Modulo, 7, true);
			AddSpec(MathOperatorType.Plus, 6, true);
			AddSpec(MathOperatorType.Minus, 6, true);
			AddSpec(MathOperatorType.UnaryMinus, 8, false);
			
			AddSpec(MathOperatorType.Negate, 8, false);
			AddSpec(MathOperatorType.Greater, 5, true);
			AddSpec(MathOperatorType.Less, 5, true);
			AddSpec(MathOperatorType.GreaterOrEqual, 5, true);
			AddSpec(MathOperatorType.LessOrEqual, 5, true);
			AddSpec(MathOperatorType.Equal, 4, true);
			AddSpec(MathOperatorType.Or, 2, true);
			AddSpec(MathOperatorType.And, 3, true);
			
			AddSpec(MathOperatorType.Ternary, 1, false);
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

		private bool HasPriority(Cpg.MathOperatorType a, Cpg.MathOperatorType b)
		{
			OperatorSpec s1;
			OperatorSpec s2;
			
			if (!s_operatorSpecs.TryGetValue(a, out s1) || !s_operatorSpecs.TryGetValue(b, out s2))
			{
				return false;
			}
			
			return s1.Priority > s2.Priority || (s1.Priority == s2.Priority && !s2.LeftAssociation);
		}

		private string SimpleOperator(Context context, InstructionOperator inst, string glue)
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
				InstructionOperator op = context.Node.Parent.Instruction as InstructionOperator;
				
				needsparen = inst == null || (op != null && HasPriority((Cpg.MathOperatorType)op.Id, (Cpg.MathOperatorType)inst.Id));
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
			return NumberTranslator.Translate(instruction.Value);
		}
		
		private string Translate(InstructionOperator instruction, Context context)
		{
			switch ((Cpg.MathOperatorType)instruction.Id)
			{
				case MathOperatorType.And:
					return SimpleOperator(context, instruction, " && ");
				case MathOperatorType.Divide:
					return SimpleOperator(context, instruction, " / ");
				case MathOperatorType.Equal:
					return SimpleOperator(context, instruction, " == ");
				case MathOperatorType.Greater:
					return SimpleOperator(context, instruction, " > ");
				case MathOperatorType.GreaterOrEqual:
					return SimpleOperator(context, instruction, " >= ");
				case MathOperatorType.Less:
					return SimpleOperator(context, instruction, " < ");
				case MathOperatorType.LessOrEqual:
					return SimpleOperator(context, instruction, " <= ");
				case MathOperatorType.Minus:
					return SimpleOperator(context, instruction, " - ");
				case MathOperatorType.UnaryMinus:
					return SimpleOperator(context, instruction, " -");
				case MathOperatorType.Multiply:
					return SimpleOperator(context, instruction, " * ");
				case MathOperatorType.Negate:
					return SimpleOperator(context, instruction, " !");
				case MathOperatorType.Or:
					return SimpleOperator(context, instruction, " || ");
				case MathOperatorType.Plus:
					return SimpleOperator(context, instruction, " + ");
				case MathOperatorType.Power:
					return String.Format("{0}{1}",
					                     Context.MathFunctionDefine(Cpg.MathFunctionType.Pow, context.Node.Children.Count),
					                     SimpleOperator(context, null, ", "));
				case MathOperatorType.Ternary:
					return String.Format("({0} ? {1} : {2})",
					                     Translate(context, 0),
					                     Translate(context, 1),
					                     Translate(context, 2));
			}
			
			throw new NotImplementedException(String.Format("The operator `{0}' is not implemented", instruction.Name));
		}
		
		private string Translate(InstructionProperty instruction, Context context)
		{
			Cpg.Property prop = instruction.Property;
			
			if (!context.Program.StateTable.Contains(prop))
			{
				throw new NotImplementedException(String.Format("The property `{0}' is not implemented", prop.FullName));
			}
			
			DataTable.DataItem item = context.Program.StateTable[prop];
			return String.Format("{0}[{1}]", context.Program.StateTable.Name, item.AliasOrIndex);
		}
		
		private string Translate(InstructionFunction instruction, Context context)
		{
			string name = Context.MathFunctionDefine(instruction);
			string[] args = new string[instruction.Arguments];
			
			for (int i = 0; i < instruction.Arguments; ++i)
			{
				args[i] = Translate(context, i);
			}
			
			return String.Format("{0} ({1})", name, String.Join(", ", args));
		}
		
		private string Translate(Instructions.Function instruction, Context context)
		{
			string name = instruction.FunctionCall.Name;
			List<string> args = new List<string>();
			
			foreach (Tree.Embedding.Argument argument in instruction.FunctionCall.OrderedArguments)
			{
				args.Add(Translate(context, context.Node.FromPath(argument.Path)));
			}

			return String.Format("{0} ({1})", name, String.Join(", ", args.ToArray()));
		}
		
		private string Translate(Instructions.Variable instruction, Context context)
		{
			return instruction.Name;
		}
		
		public string Translate(InstructionConstant instruction, Context context)
		{
			switch (instruction.Symbol)
			{
				case "pi":
				case "PI":
					return "M_PI";
				case "e":
				case "E":
					return "M_E";
			}
			
			throw new NotImplementedException(String.Format("The symbol `{0}' is not yet supported...", instruction.Symbol));
		}
	}
}

