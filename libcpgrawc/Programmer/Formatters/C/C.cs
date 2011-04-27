using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Cpg.RawC.Programmer.Formatters.C
{
	[Plugins.Attributes.Plugin(Name="C",
	                           Description="Write compact C file",
	                           Author="Jesse van den Kieboom")]
	public class C : IFormatter, Plugins.IOptions
	{
		private Options d_options;
		private Programmer.Program d_program;
		private string d_cprefix;
		private string d_cprefixup;
		private string d_sourceFilename;
		
		private class EnumItem
		{
			public Cpg.Property Property;
			public string ShortName;
			public string CName;
			
			public EnumItem(Cpg.Property property, string shortname, string cname)
			{
				Property = property;
				ShortName = shortname;
				CName = cname;
			}
		}

		private List<EnumItem> d_enumMap;

		public C()
		{
			d_options = new Options("C Formatter");
			d_enumMap = new List<EnumItem>();
		}
		
		public void Write(Program program)
		{
			d_program = program;

			WriteHeader();
			WriteSource();
			
			if (d_options.GenerateCppWrapper)
			{
				WriteCppWrapper();
			}
		}
		
		public string CompileSource()
		{
			Stream program = Assembly.GetExecutingAssembly().GetManifestResourceStream("Cpg.RawC.Programmer.Formatters.C.TestProgram.resources");
			StreamReader reader = new StreamReader(program);
			
			StringWriter writer = new StringWriter();
			
			writer.WriteLine("#include \"{0}.h\"", CPrefixDown);
			writer.WriteLine();
			
			string prog = reader.ReadToEnd();
			prog = prog.Replace("${name}", CPrefixDown);
			prog = prog.Replace("${NAME}", CPrefixUp);
			
			// Generate state map
			StringBuilder statemap = new StringBuilder();
			
			for (int i = 0; i < d_enumMap.Count; ++i)
			{
				if (i != 0)
				{
					statemap.AppendLine(",");
				}
				
				EnumItem item = d_enumMap[i];
				string name;
				
				if (item.Property.Object is Cpg.Integrator || item.Property.Object is Cpg.Network)
				{
					name = item.Property.Name;
				}
				else
				{
					name = item.Property.FullName;
				}
				
				statemap.AppendFormat("\t{{{0}, \"{1}\"}}", d_enumMap[i].CName, name);
			}
			
			prog = prog.Replace("${statemap}", statemap.ToString());

			writer.WriteLine(prog);
			return writer.ToString();
		}
		
		public void Compile(string filename, bool verbose)
		{
			if (String.IsNullOrEmpty(d_sourceFilename))
			{
				throw new Exception("The program is not compiled yet!");
			}
			
			// Compile source file
			Process process = new Process();
			process.StartInfo.FileName = "gcc";
			process.StartInfo.UseShellExecute = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.Arguments = String.Format("{0} -Wall -I. -c -o {1}.o {2}", d_options.CFlags, CPrefixDown, d_sourceFilename);
			
			if (verbose)
			{
				Console.Error.WriteLine("Compiling: gcc {0}", process.StartInfo.Arguments);
			}
			
			process.Start();
			process.WaitForExit();
			
			if (process.ExitCode != 0)
			{
				Environment.Exit(process.ExitCode);
			}
			
			// Then compile test program
			string source = CompileSource();
			
			string tempfile = Path.GetTempFileName();
			StreamWriter writer = new StreamWriter(tempfile + ".c");
			
			writer.WriteLine(source);
			writer.Close();
			
			process.StartInfo.Arguments = String.Format("{0} -I. -Wall -c -o {1}.o {2}.c", d_options.CFlags, tempfile, tempfile);
			
			if (verbose)
			{
				Console.Error.WriteLine("Compiling: gcc {0}", process.StartInfo.Arguments);
			}

			process.Start();
			process.WaitForExit();
			
			if (process.ExitCode != 0)
			{
				Environment.Exit(process.ExitCode);
			}
			
			process.StartInfo.Arguments = String.Format("-Wall {0} -o {1} {2}.o {3}.o -lm", d_options.Libs, filename, tempfile, CPrefixDown);
			
			if (verbose)
			{
				Console.Error.WriteLine("Linking: gcc {0}", process.StartInfo.Arguments);
			}

			process.Start();
			process.WaitForExit();
			
			if (process.ExitCode != 0)
			{
				Environment.Exit(process.ExitCode);
			}
			
			File.Delete(tempfile + ".c");
			File.Delete(tempfile + ".o");
			File.Delete(CPrefixDown + ".o");
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
		
		private string ValueType
		{
			get
			{
				return d_options.ValueType;
			}
		}
		
		private void WriteAccessorEnum(TextWriter writer)
		{
			if (d_program.StateTable.Count == 0)
			{
				return;
			}

			writer.WriteLine("typedef enum");
			writer.WriteLine("{");
			
			Dictionary<string, bool> unique = new Dictionary<string, bool>();
			
			List<string> names = new List<string>();
			List<string> values = new List<string>();
			List<string> comments = new List<string>();
			
			int maxname = 0;
			int maxval = 0;
			
			foreach (DataTable.DataItem item in d_program.StateTable)
			{
				Cpg.Property prop = item.Key as Cpg.Property;
				
				if (prop == null)
				{
					continue;
				}
				
				string fullname;
				
				if (prop.Object == d_program.Options.Network || prop.Object == d_program.Options.Network.Integrator)
				{
					fullname = prop.Name;
				}
				else
				{
					fullname = prop.FullName;
				}
				
				string orig = ToAsciiOnly(fullname).ToUpper();

				string enumname = String.Format("{0}_STATE_{1}", CPrefixUp, orig);
				string shortname = orig;

				int id = 0;
				
				while (unique.ContainsKey(enumname))
				{
					enumname = String.Format("{0}_STATE_{1}_{2}", CPrefixUp, orig, ++id);
					shortname = String.Format("{0}_{1}", orig, id);
				}
				
				names.Add(enumname);
				values.Add(item.Index.ToString());
				comments.Add(fullname);
				
				maxname = System.Math.Max(maxname, enumname.Length);
				maxval = System.Math.Max(maxval, item.Index.ToString().Length);

				item.Alias = enumname;
				
				d_enumMap.Add(new EnumItem(prop, shortname, enumname));
			}
			
			for (int i = 0; i < names.Count; ++i)
			{
				if (i != 0)
				{
					writer.WriteLine(", /* {0} */", comments[i - 1]);
				}

				writer.Write("\t{0} = {1}", names[i].PadRight(maxname), values[i].PadLeft(maxval));
				
				if (i == names.Count - 1)
				{
					writer.WriteLine("  /* {0} */", comments[i]);
				}
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
 			
 			writer.WriteLine("{0} {1}_get (int idx);", ValueType, CPrefixDown);
 			writer.WriteLine("void {0}_set (int idx, {1} val);", CPrefixDown, ValueType);
 			
 			writer.WriteLine();
 			
 			writer.WriteLine("void {0}_initialize (void);", CPrefixDown);
 			writer.WriteLine("void {0}_step ({1} timestep);", CPrefixDown, ValueType);
 			
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
		
		public bool IsDouble
		{
			get
			{
				return d_options.ValueType == "double";
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
					return NestedImplementation("CPG_MATH_MAX", arguments, "(x0 > x1 ? x0 : x1)");
				case MathFunctionType.Min:
					return NestedImplementation("CPG_MATH_MIN", arguments, "(x0 < x1 ? x0 : x1)");
				case MathFunctionType.Sqsum:
					return NestedImplementation("CPG_MATH_SQSUM", arguments, "x0 * x0 + x1 * x1");
				case MathFunctionType.Invsqrt:
					return IsDouble ? "1 / sqrt(x0)" : "1 / sqrtf(x0)";
				default:
				break;
					
			}
			
			throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
		}
		
		private void WriteCustomMathDefine(TextWriter writer, Cpg.MathFunctionType type, int arguments, Dictionary<string, bool> generated)
		{
			string def = Context.MathFunctionDefine(type, arguments);
			
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
			string rdef2 = Context.MathFunctionDefine(Cpg.MathFunctionType.Rand, 2);
			string rdef1 = Context.MathFunctionDefine(Cpg.MathFunctionType.Rand, 1);
			string rdef0 = Context.MathFunctionDefine(Cpg.MathFunctionType.Rand, 0);

			WriteDefine(writer, rdef2, "(a, b)", "(({0})(a + (random () / (double)RAND_MAX) * (b - a)))", null, ValueType);
			WriteDefine(writer, rdef1, "(a)", rdef2 + "(0, a)");
			WriteDefine(writer, rdef0, "", rdef1 + "(1)");
			
			Dictionary<string, bool> generated = new Dictionary<string, bool>();
			
			generated[rdef2] = true;
			generated[rdef1] = true;
			generated[rdef0] = true;

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
		
		private Dictionary<Tree.NodePath, string> GenerateMapping(string format, IEnumerable<Tree.Embedding.Argument> args)
		{
			Dictionary<Tree.NodePath, string> mapping = new Dictionary<Tree.NodePath, string>();

			foreach (Tree.Embedding.Argument arg in args)
			{
				mapping[arg.Path] = String.Format(format, arg.Index);
			}
			
			return mapping;
		}
		
		private string FunctionToC(Function function)
		{
			Context context = new Context(d_program, d_options, function.Expression, GenerateMapping("x{0}", function.Arguments));
			
			return InstructionTranslator.QuickTranslate(context);
		}
		
		private void WriteFunctions(TextWriter writer)
		{
			// Write declarations
			foreach (Programmer.Function function in d_program.Functions)
			{
				writer.WriteLine("#ifdef {0}_IS_DEFINED", function.Name.ToUpper());
				writer.WriteLine("static {0} {1} ({2}) GNUC_PURE;", ValueType, function.Name, GenerateArgsList("x", function.NumArguments, 0, ValueType));
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", function.Name.ToUpper());
				writer.WriteLine();
			}
			
			foreach (Programmer.Function function in d_program.Functions)
			{
				writer.WriteLine("#ifdef {0}_IS_DEFINED", function.Name.ToUpper());
				writer.WriteLine("static {0} {1} ({2})", ValueType, function.Name, GenerateArgsList("x", function.NumArguments, 0, ValueType));
				writer.WriteLine("{");
				writer.WriteLine("\treturn {0};", FunctionToC(function));
				writer.WriteLine("}");
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", function.Name.ToUpper());
				writer.WriteLine();
			}
		}
		
		private string Reindent(string s, string indent)
		{
			string[] lines = s.Split('\n');
			return indent + String.Join("\n" + indent, lines).Replace("\n" + indent + "\n", "\n\n");
		}
		
		private void WriteComputationNode(TextWriter writer, Computation.INode node)
		{
			Context context = new Context(d_program, d_options);
			writer.WriteLine(Reindent(ComputationNodeTranslator.Translate(node, context), "\t"));
		}
		
		private void WriteComputationNodes(TextWriter writer, IEnumerable<Computation.INode> nodes)
		{
			foreach (Computation.INode node in nodes)
			{
				WriteComputationNode(writer, node);
			}
		}
		
		private void WriteInitialization(TextWriter writer)
		{
			writer.WriteLine("void");
			writer.WriteLine("{0}_initialize (void)", CPrefixDown);
			writer.WriteLine("{");
			
			WriteComputationNodes(writer, d_program.InitializationNodes);
			
			writer.WriteLine("}");
			writer.WriteLine();
		}
		
		private void WriteAccessors(TextWriter writer)
		{
			writer.WriteLine("{0}", ValueType);
			writer.WriteLine("{0}_get (int idx)", CPrefixDown);
			writer.WriteLine("{");
			
			writer.WriteLine("\treturn {0}[idx];", d_program.StateTable.Name);
			
			writer.WriteLine("}");
			writer.WriteLine();
			
			writer.WriteLine("void");
			writer.WriteLine("{0}_set (int idx, {1} val)", CPrefixDown, ValueType);
			writer.WriteLine("{");
			
			writer.WriteLine("\t{0}[idx] = val;", d_program.StateTable.Name);
			
			writer.WriteLine("}");
			writer.WriteLine();
		}
		
		private void WriteStep(TextWriter writer)
		{
			writer.WriteLine("void");
			writer.WriteLine("{0}_step ({1} timestep)", CPrefixDown, ValueType);
			writer.WriteLine("{");
			
			WriteComputationNodes(writer, d_program.SourceNodes);
			
			writer.WriteLine("}");
			writer.WriteLine();
		}
		
		private void WriteDataTable(TextWriter writer, DataTable table)
		{
			writer.WriteLine("static {0} {1}[] =", ValueType, table.Name);
			writer.WriteLine("{");
			
			int cols = System.Math.Min(10, table.Count);
			int rows = (int)System.Math.Ceiling(table.Count / (double)cols);
			int[,] colsize = new int[cols, 2];

			string[,] vals = new string[rows, cols];
			InitialValueTranslator translator = new InitialValueTranslator();
			
			for (int i = 0; i < table.Count; ++i)
			{
				int row = i / cols;
				int col = i % cols;
				
				string val = translator.Translate(table[i].Key);
				vals[row, col] = val;
				
				int pos = val.IndexOf('.');

				colsize[col, 0] = System.Math.Max(colsize[col, 0], pos == -1 ? val.Length : pos);
				colsize[col, 1] = System.Math.Max(colsize[col, 1], pos == -1 ? 0 : (val.Length - pos));
			}
			
			for (int i = 0; i < table.Count; ++i)
			{
				int row = i / cols;
				int col = i % cols;
				
				if (col == 0)
				{
					writer.Write("\t");
				}
				
				string val = vals[row, col];
				string[] parts = val.Split('.');
				
				if (parts.Length == 1)
				{
					writer.Write("{0}{1}", val.PadLeft(colsize[col, 0] + 1), "".PadRight(colsize[col, 1]));
				}
				else
				{
					writer.Write("{0}.{1}", parts[0].PadLeft(colsize[col, 0] + 1), parts[1].PadRight(colsize[col, 1] - 1));
				}
				
				if (i != table.Count - 1)
				{
					writer.Write(",");
					
					if (col == cols - 1)
					{
						writer.WriteLine();
					}
				}
				else
				{
					writer.WriteLine();
				}
			}

			writer.WriteLine("};");
			writer.WriteLine();
		}
		
		private void WriteDataTables(TextWriter writer)
		{
			foreach (DataTable table in d_program.DataTables)
			{
				WriteDataTable(writer, table);
			}
		}
		
		private void WriteSource()
		{
			d_sourceFilename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".c");
			TextWriter writer = new StreamWriter(d_sourceFilename);
			
			writer.WriteLine("#include \"{0}.h\"", d_program.Options.Basename);
			writer.WriteLine("#include <math.h>");
			writer.WriteLine("#include <stdlib.h>");
			writer.WriteLine("#include <string.h>");
			
			writer.WriteLine();
			
			writer.WriteLine("#if __GNUC__ >= 2 && __GNUC_MINOR__ > 96");
			writer.WriteLine("#define GNUC_PURE __attribute__ (pure)");
			writer.WriteLine("#else");
			writer.WriteLine("#define GNUC_PURE");
			writer.WriteLine("#endif");
			
			writer.WriteLine();
			
			foreach (string header in d_options.CustomHeaders)
			{
				writer.WriteLine("#include \"{0}\"", header);
			}
			
			TextWriter math;
			string guard = null;
			
			if (!d_options.NoSeparateMathHeader)
			{
				string mathbase = d_program.Options.Basename + "_math.h";
				string filename = Path.Combine(d_program.Options.Output, mathbase);
				
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
			
			WriteDataTables(writer);
			WriteFunctions(writer);
			WriteInitialization(writer);
			WriteAccessors(writer);
			WriteStep(writer);

			writer.Close();
		}
		
		private void WriteCppWrapper()
		{
			string filename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".hh");
			TextWriter writer = new StreamWriter(filename);
			
			writer.WriteLine("#ifndef __{0}_HH__", CPrefixUp);
			writer.WriteLine("#define __{0}_HH__", CPrefixUp);
			
			writer.WriteLine();
			writer.WriteLine("#include \"{0}.h\"", CPrefixDown);
			writer.WriteLine();
			
			writer.WriteLine("namespace cpg");
			writer.WriteLine("{");
			writer.WriteLine("namespace {0}", CPrefixDown);
			writer.WriteLine("{");
			
			if (d_enumMap.Count > 0)
			{
				writer.WriteLine("\tstruct State");
				writer.WriteLine("\t{");
				writer.WriteLine("\t\tenum Values");
				writer.WriteLine("\t\t{");
				
				for (int i = 0; i < d_enumMap.Count; ++i)
				{
					if (i != 0)
					{
						writer.WriteLine(",");
					}

					writer.Write("\t\t\t{0} = {1}", d_enumMap[i].ShortName, d_enumMap[i].CName);
				}
				
				writer.WriteLine();

				writer.WriteLine("\t\t};");
				writer.WriteLine("\t};");
				writer.WriteLine();
			}
			
			writer.WriteLine("\tclass Network");
			writer.WriteLine("\t{");
			writer.WriteLine("\t\tpublic:");
			writer.WriteLine("\t\t\tstatic void initialize()");
			writer.WriteLine("\t\t\t{");
			writer.WriteLine("\t\t\t\t{0}_initialize ();", CPrefixDown);
			writer.WriteLine("\t\t\t}");
			writer.WriteLine();

			writer.WriteLine("\t\t\tstatic void step({0} timestep)", ValueType);
			writer.WriteLine("\t\t\t{");
			writer.WriteLine("\t\t\t\t{0}_step (timestep);", CPrefixDown);
			writer.WriteLine("\t\t\t}");
			writer.WriteLine();

			writer.WriteLine("\t\t\tstatic void set(State::Values idx, {0} val);", ValueType);
			writer.WriteLine("\t\t\t{");
			writer.WriteLine("\t\t\t\t{0}_set (static_cast<int>(idx), val);", CPrefixDown);
			writer.WriteLine("\t\t\t}");
			writer.WriteLine();

			writer.WriteLine("\t\t\tstatic {0} get(State::Values idx);", ValueType);
			writer.WriteLine("\t\t\t{");
			writer.WriteLine("\t\t\t\treturn {0}_get (static_cast<int>(idx));", CPrefixDown);
			writer.WriteLine("\t\t\t}");
			writer.WriteLine();
			
			writer.WriteLine("\t};");
			
			writer.WriteLine("}");
			writer.WriteLine("}");

			writer.WriteLine();
			
			writer.WriteLine("#endif /* __{0}_HH__ */", CPrefixUp);
			
			writer.Close();
		}
	}
}
