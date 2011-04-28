using System;

namespace Cpg.RawC.Programmer.Formatters.C
{
	public class InstructionTranslator : DynamicVisitor
	{
		public InstructionTranslator() : base(typeof(string),
		                                      BindingFlags.Default,
		                                      System.Reflection.BindingFlags.Default |
		                                      System.Reflection.BindingFlags.NonPublic |
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
		
		public string Translate(Context context, int child)
		{
			string ret;

			context.Push(context.Node.Children[child]);
			ret = Translate(context);
			context.Pop();

			return ret;
		}

		private string SimpleOperator(Context context, string glue)
		{
			int num = context.Node.Children.Count;
			string[] args = new string[num];
			
			for (int i = 0; i < num; ++i)
			{
				args[i] = Translate(context, i);
			}
			
			return String.Format("({0})", String.Join(glue, args).Trim());
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
					return SimpleOperator(context, " && ");
				case MathOperatorType.Divide:
					return SimpleOperator(context, " / ");
				case MathOperatorType.Equal:
					return SimpleOperator(context, " == ");
				case MathOperatorType.Greater:
					return SimpleOperator(context, " > ");
				case MathOperatorType.GreaterOrEqual:
					return SimpleOperator(context, " >= ");
				case MathOperatorType.Less:
					return SimpleOperator(context, " < ");
				case MathOperatorType.LessOrEqual:
					return SimpleOperator(context, " <= ");
				case MathOperatorType.Minus:
					return SimpleOperator(context, " -");
				case MathOperatorType.UnaryMinus:
					return SimpleOperator(context, " -");
				case MathOperatorType.Multiply:
					return SimpleOperator(context, " * ");
				case MathOperatorType.Negate:
					return SimpleOperator(context, " !");
				case MathOperatorType.Or:
					return SimpleOperator(context, " || ");
				case MathOperatorType.Plus:
					return SimpleOperator(context, " + ");
				case MathOperatorType.Power:
					return String.Format("{0}{1}",
					                     Context.MathFunctionDefine(Cpg.MathFunctionType.Pow, context.Node.Children.Count),
					                     SimpleOperator(context, ", "));
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
			int num = instruction.FunctionCall.NumArguments;
			string[] args = new string[num];
			
			for (int i = 0; i < num; ++i)
			{
				args[i] = Translate(context, i);
			}
			
			return String.Format("{0} ({1})", name, String.Join(", ", args));
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

