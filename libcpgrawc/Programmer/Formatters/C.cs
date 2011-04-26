using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer.Formatters
{
	[Plugins.Attributes.Plugin(Name="C",
	                           Description="Write compact C file",
	                           Author="Jesse van den Kieboom")]
	public class C : IFormatter, Plugins.IOptions
	{
		private class CustomOptions : CommandLine.OptionGroup
		{
			public enum PrecisionType
			{
				Double,
				Float
			}

			[CommandLine.Option("precision", Description="Type of precision to use (double/float)")]
			public PrecisionType Precision = PrecisionType.Double;
			
			[CommandLine.Option("custom-header", ArgumentName="FILENAME", Description="Custom header to include")]
			public string CustomHeader;
			
			[CommandLine.Option("no-separate-math-header", Description="Whether or not to use a separate header for math defines")]
			public bool NoSeparateMathHeader;
			
			public CustomOptions(string name) : base(name)
			{
			}
		}
		
		private CustomOptions d_options;
		private Programmer.Program d_program;
		private string d_cprefix;
		private string d_cprefixup;

		public C()
		{
			d_options = new CustomOptions("C Formatter");
		}
		
		public void Write(Program program)
		{
			d_program = program;

			WriteHeader();
			WriteSource();
		}
		
		public CommandLine.OptionGroup Options
		{
			get
			{
				return d_options;
			}
		}
		
		private string ToAsciiOnly(string name)
		{
			StringBuilder builder = new StringBuilder();

			foreach (char c in name)
			{
				if (!char.IsLetterOrDigit(c))
				{
					builder.Append("_");
				}
				else
				{
					builder.Append(c);
				}
			}
			
			return builder.ToString();
		}
		
		private string CPrefix
		{
			get
			{
				if (d_cprefix == null)
				{
					string[] parts = ToAsciiOnly(d_program.Options.Basename).ToLower().Split('_');
			
					parts = Array.FindAll(parts, a => !String.IsNullOrEmpty(a));
					parts = Array.ConvertAll(parts, a => char.ToUpper(a[0]) + a.Substring(1));
			
					d_cprefix = String.Join("", parts);
				}
				
				return d_cprefix;
			}
		}
		
		private string CPrefixUp
		{
			get
			{
				if (d_cprefixup == null)
				{
					d_cprefixup = ToAsciiOnly(d_program.Options.Basename).ToUpper();
				}
				
				return d_cprefixup;
			}
		}
		
		private string CPrefixDown
		{
			get
			{
				return CPrefixUp.ToLower();
			}
		}
		
		private string Precision
		{
			get
			{
				switch (d_options.Precision)
				{
					case CustomOptions.PrecisionType.Double:
						return "double";
					case CustomOptions.PrecisionType.Float:
						return "float";
				}
				
				return "double";
			}
		}
		
		private void WriteAccessorEnum(TextWriter writer)
		{
			if (d_program.DataTable.Count == 0)
			{
				return;
			}

			writer.WriteLine("typedef enum");
			writer.WriteLine("{");
			
			bool first = true;

			foreach (DataTable.DataItem item in d_program.DataTable)
			{
				Cpg.Property prop = item.Key as Cpg.Property;
				
				if (prop == null)
				{
					continue;
				}
				
				if (!first)
				{
					writer.WriteLine(",");
				}
				else
				{
					first = false;
				}
				
				string fullname;
				
				if (prop.Object == d_program.Options.Network)
				{
					fullname = prop.Name;
				}
				else
				{
					fullname = prop.FullName;
				}
				
				fullname =  ToAsciiOnly(fullname).ToUpper();

				writer.Write("\t{0}_STATE_{1} = {2}", CPrefixUp, fullname, item.Index);
			}
			
			writer.WriteLine();
			writer.WriteLine("}} {0}State;", CPrefix);
			writer.WriteLine();
		}
		
		private void WriteHeader()
		{
			string filename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".h");
			TextWriter writer = new StreamWriter(filename);
			
			// Include guard
			writer.WriteLine("#ifndef __{0}_H__", CPrefixUp);
			writer.WriteLine("#define __{0}_H__", CPrefixUp);
			writer.WriteLine();
			
			// Protect for including this from C++
			writer.WriteLine("#ifdef __cplusplus");
 			writer.WriteLine("extern \"C\" {");
 			writer.WriteLine("#endif");
 			
 			writer.WriteLine();
 			
 			// Write interface
 			WriteAccessorEnum(writer);
 			
 			writer.WriteLine("{0} {1}_get (int idx);", Precision, CPrefixDown);
 			writer.WriteLine("void {0}_set (int idx, {1} val);", CPrefixDown, Precision);
 			
 			writer.WriteLine();
 			
 			writer.WriteLine("void {0}_initialize (void);", CPrefixDown);
 			writer.WriteLine("void {0}_step ({1} timestep);", CPrefixDown, Precision);
 			
 			writer.WriteLine();

 			// End protect for including this from C++
 			writer.WriteLine("#ifdef __cplusplus");
 			writer.WriteLine("}");
 			writer.WriteLine("#endif");
 			
 			writer.WriteLine();
			
			// End include guard
			writer.WriteLine("#endif /* __{0}_H__ */", CPrefixUp);

			writer.Close();
		}
		
		private string MathFunctionDefine(Cpg.InstructionFunction instruction)
		{
			return MathFunctionDefine((Cpg.MathFunctionType)instruction.Id, instruction.Arguments);
		}
		
		private string MathFunctionDefine(Cpg.MathFunctionType type, int arguments)
		{
			string name = Enum.GetName(typeof(Cpg.MathFunctionType), type);
			string val;

			switch (type)
			{
				case MathFunctionType.Abs:
				case MathFunctionType.Acos:
				case MathFunctionType.Asin:
				case MathFunctionType.Atan:
				case MathFunctionType.Atan2:
				case MathFunctionType.Ceil:
				case MathFunctionType.Cos:
				case MathFunctionType.Cosh:
				case MathFunctionType.Exp:
				case MathFunctionType.Exp2:
				case MathFunctionType.Floor:
				case MathFunctionType.Hypot:
				case MathFunctionType.Invsqrt:
				case MathFunctionType.Lerp:
				case MathFunctionType.Ln:
				case MathFunctionType.Log10:
				case MathFunctionType.Max:
				case MathFunctionType.Min:
				case MathFunctionType.Pow:
				case MathFunctionType.Round:
				case MathFunctionType.Sin:
				case MathFunctionType.Sinh:
				case MathFunctionType.Sqrt:
				case MathFunctionType.Sqsum:
				case MathFunctionType.Tan:
				case MathFunctionType.Tanh:
				case MathFunctionType.Rand:
					val = name.ToUpper();
				break;
				default:
					throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
			}
			
			if (Cpg.Math.FunctionIsVariable(type))
			{
				val = String.Format("{0}{1}", val, arguments);
			}
			
			return val;
		}
		
		public bool IsDouble
		{
			get
			{
				return d_options.Precision == CustomOptions.PrecisionType.Double;
			}
		}
		
		private string GenerateArgsList(string prefix, int num)
		{
			return GenerateArgsList(prefix, num, 0);
		}
		
		private string GenerateArgsList(string prefix, int num, int numstart)
		{
			return GenerateArgsList(prefix, num, numstart, null);
		}
		
		private string GenerateArgsList(string prefix, int num, int numstart, string type)
		{
			string[] ret = new string[num];
			string extra = !String.IsNullOrEmpty(type) ? String.Format("{0} ", type) : "";
			
			for (int i = 0; i < num; ++i)
			{
				ret[i] = String.Format("{0}{1}{2}", extra, prefix, numstart + i);
			}
			
			return String.Join(", ", ret);
		}
		
		private string NestedImplementation(string name, int arguments, string implementation)
		{
			if (arguments == 2)
			{
				return implementation;
			}
			else
			{
				return String.Format("{0}2(x0, {0}{1}({2}))", name, arguments - 1, GenerateArgsList("x", arguments - 1, 1));
			}
		}
		
		private string MathFunctionMap(Cpg.InstructionFunction instruction)
		{
			return MathFunctionMap((Cpg.MathFunctionType)instruction.Id, instruction.Arguments);
		}
		
		private string MathFunctionMap(Cpg.MathFunctionType type, int arguments)
		{
			string name = Enum.GetName(typeof(Cpg.MathFunctionType), type);

			switch (type)
			{
				case MathFunctionType.Abs:
				case MathFunctionType.Acos:
				case MathFunctionType.Asin:
				case MathFunctionType.Atan:
				case MathFunctionType.Atan2:
				case MathFunctionType.Ceil:
				case MathFunctionType.Cos:
				case MathFunctionType.Cosh:
				case MathFunctionType.Exp:
				case MathFunctionType.Exp2:
				case MathFunctionType.Floor:
				case MathFunctionType.Hypot:
				case MathFunctionType.Sin:
				case MathFunctionType.Sinh:
				case MathFunctionType.Sqrt:
				case MathFunctionType.Ln:
				case MathFunctionType.Log10:
				case MathFunctionType.Pow:
				case MathFunctionType.Round:
				case MathFunctionType.Tan:
				case MathFunctionType.Tanh:
				{
					return String.Format("{0}{1}({2})", name.ToLower(), IsDouble ? "" : "f", GenerateArgsList("x", arguments));
				}
				case MathFunctionType.Lerp:
					return "(x0 + (x1 - x0) * x2)";
				case MathFunctionType.Max:
					return NestedImplementation("MAX", arguments, "(x0 > x1 ? x0 : x1)");
				case MathFunctionType.Min:
					return NestedImplementation("MIN", arguments, "(x0 < x1 ? x0 : x1)");
				case MathFunctionType.Sqsum:
					return NestedImplementation("SQSUM", arguments, "x0 * x0 + x1 * x1");
				case MathFunctionType.Invsqrt:
					return IsDouble ? "1 / sqrt(x0)" : "1 / sqrtf(x0)";
				default:
				break;
					
			}
			
			throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
		}
		
		private void WriteCustomMathDefine(TextWriter writer, Cpg.MathFunctionType type, int arguments, Dictionary<string, bool> generated)
		{
			string def = MathFunctionDefine(type, arguments);
			
			if (generated.ContainsKey(def))
			{
				return;
			}
			
			// Note: this does not actually work in the general case...
			if (Cpg.Math.FunctionIsVariable(type) && arguments > 2)
			{
				WriteCustomMathDefine(writer, type, arguments - 1, generated);
			}
			
			string mathmap = MathFunctionMap(type, arguments);
			List<string> args = new List<string>();
				
			for (int i = 0; i < arguments; ++i)
			{
				args.Add(String.Format("x{0}", i));
			}
			
			WriteDefine(writer, def, String.Format("({0})", GenerateArgsList("x", arguments)), mathmap);

			generated[def] = true;
		}
		
		private void WriteDefine(TextWriter writer, string name, string args, string val, params object[] objs)
		{
			WriteDefine(writer, name, args, val, null, objs);
		}
		
		private void WriteDefine(TextWriter writer, string name, string args, string val, string extra, params object[] objs)
		{
			writer.WriteLine("#ifndef {0}", name);
			writer.WriteLine(String.Format("#define {0}{1} {2}", name, args, String.Format(val, objs)));
			
			if (!String.IsNullOrEmpty(extra))
			{
				writer.WriteLine(extra);
			}

			writer.WriteLine("#endif /* {0} */", name);
			writer.WriteLine();
		}
		
		private void WriteCustomMathDefines(TextWriter writer)
		{
			// Always define random stuff, it's a bit special...
			WriteDefine(writer, "RAND2", "(a, b)", "(({0})(a + (random () / (double)RAND_MAX) * (b - a)))", null, Precision);
			WriteDefine(writer, "RAND1", "(a)", "RAND2(0, a)");
			WriteDefine(writer, "RAND0", "", "RAND1(1)");
			
			Dictionary<string, bool> generated = new Dictionary<string, bool>();
			
			generated["RAND2"] = true;
			generated["RAND1"] = true;
			generated["RAND0"] = true;

			foreach (Cpg.InstructionFunction inst in d_program.CollectInstructions<Cpg.InstructionFunction>())
			{
				if (!(inst is Cpg.InstructionOperator))
				{
					WriteCustomMathDefine(writer, (Cpg.MathFunctionType)inst.Id, inst.Arguments, generated);
				}
			}
		}
		
		private void WriteFunctionDefines(TextWriter writer)
		{
			foreach (Programmer.Function function in d_program.Functions)
			{
				string def = function.Name.ToUpper();
				string impl = function.Name;
				
				WriteDefine(writer, def, "", impl, String.Format("#define {0}_IS_DEFINED", def));
			}
		}
		
		private string ExpressionToC(Tree.Node node)
		{
			return ExpressionToC(node, node, new Dictionary<string, string>());
		}
		
		private string ExpressionToC(Tree.Node node, string format, IEnumerable<Tree.Embedding.Argument> args)
		{
			Dictionary<string, string> mapping = new Dictionary<string, string>();

			if (args != null && format != null)
			{
				foreach (Tree.Embedding.Argument arg in args)
				{
					mapping[arg.Path.ToString()] = String.Format(format, arg.Index);
				}
			}
			
			return ExpressionToC(node, node, mapping);
		}
		
		private bool As<T>(object o, out T ret)
		{
			if (o is T)
			{
				ret = (T)o;
				return true;
			}
			else
			{
				ret = default(T);
				return false;
			}
		}
		
		private string SimpleOperator(Tree.Node root, Tree.Node node, string glue, Dictionary<string, string> mapping)
		{
			string[] args = new string[node.Children.Count];
			
			for (int i = 0; i < args.Length; ++i)
			{
				args[i] = ExpressionToC(root, node.Children[i], mapping);
			}
			
			return String.Format("({0})", String.Join(glue, args).Trim());
		}
		
		private string OperatorToC(Tree.Node root, Tree.Node node, Cpg.InstructionOperator instop, Dictionary<string, string> mapping)
		{
			switch ((Cpg.MathOperatorType)instop.Id)
			{
				case MathOperatorType.And:
					return SimpleOperator(root, node, " && ", mapping);
				case MathOperatorType.Divide:
					return SimpleOperator(root, node, " / ", mapping);
				case MathOperatorType.Equal:
					return SimpleOperator(root, node, " == ", mapping);
				case MathOperatorType.Greater:
					return SimpleOperator(root, node, " > ", mapping);
				case MathOperatorType.GreaterOrEqual:
					return SimpleOperator(root, node, " >= ", mapping);
				case MathOperatorType.Less:
					return SimpleOperator(root, node, " < ", mapping);
				case MathOperatorType.LessOrEqual:
					return SimpleOperator(root, node, " <= ", mapping);
				case MathOperatorType.Minus:
					return SimpleOperator(root, node, " -", mapping);
				case MathOperatorType.Multiply:
					return SimpleOperator(root, node, " * ", mapping);
				case MathOperatorType.Negate:
					return SimpleOperator(root, node, " !", mapping);
				case MathOperatorType.Or:
					return SimpleOperator(root, node, " || ", mapping);
				case MathOperatorType.Plus:
					return SimpleOperator(root, node, " + ", mapping);
				case MathOperatorType.Power:
					return String.Format("{0}{1}",
					                     MathFunctionDefine(Cpg.MathFunctionType.Pow, node.Children.Count),
					                     SimpleOperator(root, node, ", ", mapping));
				case MathOperatorType.Ternary:
					return String.Format("({0} ? {1} : {2})",
					                     ExpressionToC(root, node.Children[0], mapping),
					                     ExpressionToC(root, node.Children[1], mapping),
					                     ExpressionToC(root, node.Children[2], mapping));
			}
			
			throw new NotImplementedException(String.Format("The operator `{0}' is not implemented", instop.Name));
		}
		
		private string ExpressionToC(Tree.Node root, Tree.Node node, Dictionary<string, string> mapping)
		{
			if (mapping.Count != 0)
			{
				string path = node.RelPath(root).ToString();
				string ret;
				
				if (mapping.TryGetValue(path, out ret))
				{
					return ret;
				}
			}
			
			Cpg.InstructionNumber instnum;
			Cpg.InstructionOperator instop;
			Cpg.InstructionProperty instprop;
			
			if (As<Cpg.InstructionNumber>(node.Instruction, out instnum))
			{
				return instnum.Value.ToString();
			}
			else if (As<Cpg.InstructionOperator>(node.Instruction, out instop))
			{
				return OperatorToC(root, node, instop, mapping);
			}
			else if (As<Cpg.InstructionProperty>(node.Instruction, out instprop))
			{
				Cpg.Property prop = instprop.Property;
				
				if (!d_program.DataTable.Contains(prop))
				{
					throw new NotImplementedException(String.Format("The property `{0}' is not implemented", prop.FullName));
				}
				
				DataTable.DataItem item = d_program.DataTable[prop];
				return String.Format("{0}[{1}]", d_program.DataTable.Name, item.Index);
			}
			
			throw new NotImplementedException(String.Format("The instruction `{0}' is not yet supported", node.Instruction));
		}
		
		private string FunctionToC(Programmer.Function function)
		{
			return ExpressionToC(function.Expression, "x{0}", function.Arguments); 
		}
		
		private void WriteFunctions(TextWriter writer)
		{
			// Write declarations
			foreach (Programmer.Function function in d_program.Functions)
			{
				writer.WriteLine("#ifdef {0}_IS_DEFINED", function.Name.ToUpper());
				writer.WriteLine("static {0} {1} ({2});", Precision, function.Name, GenerateArgsList("x", function.NumArguments, 0, Precision));
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", function.Name.ToUpper());
				writer.WriteLine();
			}
			
			foreach (Programmer.Function function in d_program.Functions)
			{
				writer.WriteLine("#ifdef {0}_IS_DEFINED", function.Name.ToUpper());
				writer.WriteLine("static {0} {1} ({2})", Precision, function.Name, GenerateArgsList("x", function.NumArguments, 0, Precision));
				writer.WriteLine("{");
				writer.WriteLine("\treturn {0};", FunctionToC(function));
				writer.WriteLine("}");
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", function.Name.ToUpper());
				writer.WriteLine();
			}
		}
		
		private void WriteSource()
		{
			string filename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".c");
			TextWriter writer = new StreamWriter(filename);
			
			writer.WriteLine("#include \"{0}.h\"", d_program.Options.Basename);
			writer.WriteLine("#include <math.h>");
			
			writer.WriteLine();
			
			if (!String.IsNullOrEmpty(d_options.CustomHeader))
			{
				writer.WriteLine("#include \"{0}\"", d_options.CustomHeader);
			}
			
			TextWriter math;
			string guard = null;
			
			if (!d_options.NoSeparateMathHeader)
			{
				string mathbase = d_program.Options.Basename + "_math.h";
				filename = Path.Combine(d_program.Options.Output, mathbase);
				
				writer.WriteLine("#include \"{0}\"", mathbase);
				writer.WriteLine();
			  	math = new StreamWriter(filename);
			  	
			  	guard = ToAsciiOnly(mathbase).ToUpper();
			
				math.WriteLine("#ifndef __{0}__", guard);
				math.WriteLine("#define __{0}__", guard);
				math.WriteLine();
			}
			else
			{
				math = writer;
			}
			
			WriteCustomMathDefines(math);
			WriteFunctionDefines(math);
			
			if (!d_options.NoSeparateMathHeader)
			{
				math.WriteLine("#endif /* __{0}__ */", guard);
				math.Close();
			}
			
			WriteFunctions(writer);

			writer.Close();
		}
	}
}

