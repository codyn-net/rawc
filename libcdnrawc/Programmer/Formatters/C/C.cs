using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Cdn.RawC.Programmer.Formatters.C
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
		private string d_headerFilename;

		private string d_runSourceFilename;
		private string d_runHeaderFilename;

		private class EnumItem
		{
			public Cdn.Variable Variable;
			public string ShortName;
			public string CName;
			
			public EnumItem(Cdn.Variable property, string shortname, string cname)
			{
				Variable = property;
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

		private bool IsStandalone
		{
			get { return d_options.Standalone != null || d_options.ValueType != "double"; }
		}
		
		public string[] Write(Program program)
		{
			d_program = program;
			
			List<string > written = new List<string>();

			WriteHeader();
			WriteSource();

			WriteRunHeader();
			WriteRunSource();
			
			written.Add(d_headerFilename);
			written.Add(d_sourceFilename);

			written.Add(d_runHeaderFilename);
			written.Add(d_runSourceFilename);

			if (IsStandalone)
			{
				// Copy rawc sources also
				var sources = Path.Combine(Config.Data, "cdn-rawc-1.0/src/cdn-rawc");
				var destdir = Path.Combine(d_program.Options.Output, "cdn-rawc");

				Directory.CreateDirectory(destdir);

				foreach (var f in Directory.EnumerateFiles(sources))
				{
					var dest = Path.Combine(destdir, Path.GetFileName(f));
					File.Copy(f, dest, true);
					written.Add(dest);
				}

				var intdir = Path.Combine(destdir, "integrators");

				if (d_options.Standalone == "full")
				{
					Directory.CreateDirectory(intdir);

					foreach (var integrator in Directory.EnumerateFiles(Path.Combine(sources, "integrators")))
					{
						var dest = Path.Combine(intdir, Path.GetFileName(integrator));
						File.Copy(integrator, dest, true);
						written.Add(dest);
					}
				}
				else
				{
					Directory.CreateDirectory(intdir);

					var name = Knowledge.Instance.Network.Integrator.ClassId.Replace("_", "-");
					var dest = Path.Combine(intdir, "cdn-rawc-integrator-" + name);
					var source = Path.Combine(sources, "integrators", "cdn-rawc-integrator-" + name);

					File.Copy(source + ".c", dest + ".c", true);
					File.Copy(source + ".h", dest + ".h", true);

					written.Add(dest + ".c");
					written.Add(dest + ".h");
				}
			}
			
			if (!d_options.NoMakefile)
			{
				var filename = WriteMakefile();
				written.Add(filename);
			}

			return written.ToArray();
		}

		private string WriteMakefile()
		{
			var filename = Path.Combine(d_program.Options.Output, "Makefile");
			TextWriter writer = new StreamWriter(filename);

			if (IsStandalone)
			{
				writer.Write(Template("Cdn.RawC.Programmer.Formatters.C.Resources.Standalone.make"));
			}
			else
			{
				writer.Write(Template("Cdn.RawC.Programmer.Formatters.C.Resources.Library.make"));
			}

			writer.Close();
			return filename;
		}

		public string Template(string resource)
		{
			Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
			StreamReader reader = new StreamReader(res);
			string ret = reader.ReadToEnd();

			var sources = Path.GetFileName(d_sourceFilename) + " " + Path.GetFileName(d_runSourceFilename);
			var headers = Path.GetFileName(d_headerFilename) + " " + Path.GetFileName(d_runHeaderFilename);

			if (IsStandalone)
			{
				sources += " $(wildcard cdn-rawc/*.c) $(wildcard cdn-rawc/integrators/*.c)";
				headers += " $(wildcard cdn-rawc/*.h) $(wildcard cdn-rawc/integrators/*.h)";
			}

			var srep = new Dictionary<string, string> {
				{"name", CPrefixDown},
				{"NAME", CPrefixUp},
				{"Name", CPrefix},
				{"SOURCES", sources},
				{"HEADERS", headers},
				{"integrator", Knowledge.Instance.Network.Integrator.ClassId},
				{"INTEGRATOR", Knowledge.Instance.Network.Integrator.ClassId.ToUpper()},
				{"basename", d_program.Options.Basename},
				{"BASENAME", d_program.Options.Basename.ToUpper()},
				{"valuetype", ValueType},
				{"cflags", d_options.CFlags},
				{"libs", d_options.Libs},
			};

			var ors = String.Join("|", (new List<string>(srep.Keys)).ToArray());
			var r = new System.Text.RegularExpressions.Regex("[$][{](" + ors + ")[}]");

			ret = r.Replace(ret, (m) => {
				var s = srep[m.Groups[1].Value];
				return s == null ? "" : s;
			});

			r = new System.Text.RegularExpressions.Regex(@"[$][{]include:([^}]*)[}]");

			ret = r.Replace(ret, (m) => {
				return Template(m.Groups[1].Value);
			});

			return ret;
		}
		
		public string Source(string resource)
		{
			StringWriter writer = new StringWriter();
			
			writer.WriteLine("#include \"{0}.h\"", d_program.Options.Basename);
			writer.WriteLine();

			var prog = Template(resource);
			
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
				
				if (item.Variable.Object is Cdn.Integrator || item.Variable.Object is Cdn.Network)
				{
					name = item.Variable.Name;
				}
				else
				{
					name = item.Variable.FullName;
				}
				
				statemap.AppendFormat("\t{{{0}, \"{1}\"}}", d_enumMap[i].CName, name);
			}
			
			prog = prog.Replace("${statemap}", statemap.ToString());

			writer.WriteLine(prog);
			return writer.ToString();
		}

		public string MexSource()
		{
			return Source("Cdn.RawC.Programmer.Formatters.C.Resources.MexProgram.c");
		}

		public string CompileForValidation(bool verbose)
		{
			return Compile(verbose, true)[0];
		}

		public string[] Compile(bool verbose)
		{
			return Compile(verbose, false);
		}

		private string[] Compile(bool verbose, bool validating)
		{
			if (String.IsNullOrEmpty(d_sourceFilename))
			{
				throw new Exception("The program is not generated yet!");
			}

			string ddir = Path.GetDirectoryName(d_sourceFilename);
			
			// Compile source file
			Process process = new Process();

			process.StartInfo.FileName = "make";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			StringBuilder args = new StringBuilder();

			if (verbose)
			{
				args.Append(" V=1");
			}

			List<string> ret = new List<string>();

			bool all = !(d_options.CompileShared || d_options.CompileStatic);

			if (all || d_options.CompileShared || validating)
			{
				args.Append(" shared");
				ret.Add(Path.Combine(ddir, "lib" + CPrefixDown + ".so"));
			}

			if ((all || d_options.CompileStatic) && !validating)
			{
				args.Append(" static");
				ret.Add(Path.Combine(ddir, "lib" + CPrefixDown + ".a"));
			}

			process.StartInfo.Arguments = args.ToString();
			process.StartInfo.WorkingDirectory = ddir;

			if (!verbose && validating)
			{
				process.StartInfo.RedirectStandardOutput = true;
			}
			
			if (verbose)
			{
				Log.WriteLine("Compiling in {0}: make {1}", ddir, process.StartInfo.Arguments);
			}

			process.Start();
			process.WaitForExit();
			
			if (process.ExitCode != 0)
			{
				Environment.Exit(process.ExitCode);
			}

			return ret.ToArray();
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
			
			Dictionary<string, bool > unique = new Dictionary<string, bool>();
			
			List<string > names = new List<string>();
			List<string > values = new List<string>();
			List<string > comments = new List<string>();
			
			int maxname = 0;
			int maxval = 0;

			int firstrand = -1;
			
			foreach (DataTable.DataItem item in d_program.StateTable)
			{
				Cdn.Variable prop = null;
				bool isdiff = false;

				if ((item.Type & DataTable.DataItem.Flags.Derivative) != 0)
				{
					var state = item.Object as DerivativeState;

					if (state != null)
					{
						prop = state.Object as Cdn.Variable;
						isdiff = true;
					}
				}
				else
				{
					prop = item.Key as Cdn.Variable;
				}
				
				if (prop == null || prop.Flags == 0)
				{
					if (!(Cdn.RawC.Options.Instance.Verbose && d_options.SymbolicNames))
					{
						continue;
					}

					var rinstr = item.Key as InstructionRand;

					if (rinstr != null)
					{
						if (firstrand == -1)
						{
							firstrand = item.Index;
						}

						item.Alias = string.Format("{0} /* RAND_{1} */", item.Index, item.Index - firstrand);
					}
					else if ((item.Type & DataTable.DataItem.Flags.Constant) != 0)
					{
						item.Alias = string.Format("{0} /* {1} */", item.Index, item.Key);
					}

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
				string prefix;

				if (isdiff)
				{
					prefix = String.Format("{0}_DERIV", CPrefixUp);
				}
				else
				{
					prefix = String.Format("{0}_STATE", CPrefixUp);
				}

				string enumname = String.Format("{0}_{1}", prefix, orig);
				string shortname = orig;

				int id = 0;
				
				while (unique.ContainsKey(enumname))
				{
					enumname = String.Format("_{1}__{2}", prefix, orig, ++id);
					shortname = String.Format("{0}__{1}", orig, id);
				}
				
				var comment = fullname;
				
				if (isdiff)
				{
					comment += "'";
				}
				
				names.Add(enumname);
				values.Add(item.Index.ToString());
				comments.Add(comment);
				
				maxname = System.Math.Max(maxname, enumname.Length);
				maxval = System.Math.Max(maxval, item.Index.ToString().Length);

				if (d_options.SymbolicNames)
				{
					item.Alias = enumname;
				}
				
				unique[enumname] = true;

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

			if (names.Count == 0)
			{
				writer.WriteLine();
			}

			writer.WriteLine("}} CdnRawc{0}State;", CPrefix);
			writer.WriteLine();
		}

		private void WriteRunSource()
		{
			d_runSourceFilename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + "_run.c");
			TextWriter writer = new StreamWriter(d_runSourceFilename);

			writer.Write(Template("Cdn.RawC.Programmer.Formatters.C.Resources.RunSource.c"));

			writer.Close();
		}

		private void WriteRunHeader()
		{
			d_runHeaderFilename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + "_run.h");
			TextWriter writer = new StreamWriter(d_runHeaderFilename);

			writer.Write(Template("Cdn.RawC.Programmer.Formatters.C.Resources.RunHeader.h"));

			writer.Close();
		}
		
		private void WriteHeader()
		{
			d_headerFilename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".h");
			TextWriter writer = new StreamWriter(d_headerFilename);
			
			// Include guard
			writer.WriteLine("#ifndef __CDN_RAWC_{0}_H__", CPrefixUp);
			writer.WriteLine("#define __CDN_RAWC_{0}_H__", CPrefixUp);

			writer.WriteLine();
			writer.WriteLine("#define ValueType {0}", ValueType);

			writer.WriteLine();
			writer.WriteLine("#include <cdn-rawc/cdn-rawc.h>");
			writer.WriteLine();
			
			// Protect for including this from C++
			writer.WriteLine("CDN_RAWC_BEGIN_DECLS");

			writer.WriteLine();

			var evtcnt = Knowledge.Instance.EventContainersCount;

			if (evtcnt > 0)
			{
				string ctype = EventStateType;

				writer.WriteLine("#define CDN_RAWC_{0}_DATA_SIZE ({1} + ({2} * sizeof({3}) + sizeof(ValueType) - 1) / sizeof(ValueType))",
				                 CPrefixUp,
				                 d_program.StateTable.Count,
				                 Knowledge.Instance.EventStatesMap.Count,
				                 ctype);
			}
			else
			{
				writer.WriteLine("#define CDN_RAWC_{0}_DATA_SIZE {1}", CPrefixUp, d_program.StateTable.Count);
			}

			writer.WriteLine();
 			
			// Write interface
			WriteAccessorEnum(writer);

			writer.WriteLine("CdnRawcNetwork *cdn_rawc_{0}_network (void);", CPrefixDown);
			writer.WriteLine();

			writer.WriteLine("uint8_t cdn_rawc_{0}_get_type_size (void);", CPrefixDown);
			writer.WriteLine();
 			
			// End protect for including this from C++
			writer.WriteLine("CDN_RAWC_END_DECLS"); 			
			writer.WriteLine();
			
			// End include guard
			writer.WriteLine("#endif /* __CDN_RAWC_{0}_H__ */", CPrefixUp);

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
			if (num == 0 && type == null)
			{
				return "void";
			}

			List<string> ret = new List<string>(num);

			if (type != null)
			{
				ret.Add(String.Format("ValueType *{0}", d_program.StateTable.Name));
			}

			string extra = !String.IsNullOrEmpty(type) ? String.Format("{0} ", type) : "";
			
			for (int i = 0; i < num; ++i)
			{
				ret.Add(String.Format("{0}{1}{2}", extra, prefix, numstart + i));
			}
			
			return String.Join(", ", ret.ToArray());
		}
		
		private string NestedImplementation(string name, int arguments, string implementation)
		{
			if (arguments == 2)
			{
				return implementation;
			}
			else
			{
				return String.Format("{0}2 (x0, {0}{1} ({2}))", name, arguments - 1, GenerateArgsList("x", arguments - 1, 1));
			}
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

		private void WriteCustomMathRequired(TextWriter writer)
		{
			var generated = new HashSet<string>();

			foreach (Cdn.InstructionFunction inst in d_program.CollectInstructions<Cdn.InstructionFunction>())
			{
				if (inst.Id > (uint)MathFunctionType.NumOperators || inst.Id == (uint)MathFunctionType.Power || inst.Id == (uint)MathFunctionType.Modulo)
				{
					string def = Context.MathFunctionDefine(inst);

					if (generated.Add(def))
					{
						writer.WriteLine("#define {0}_REQUIRED", def);
					}
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
			Dictionary<Tree.NodePath, string > mapping = new Dictionary<Tree.NodePath, string>();

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

				writer.Write("static ");

				if (function.Inline)
				{
					writer.Write("inline ");
				}

				writer.Write("ValueType {0} ({1})",
				                 function.Name,
				                 GenerateArgsList("x", function.NumArguments, 0, "ValueType"));

				if (function.Inline)
				{
					writer.Write(" GNUC_INLINE");
				}

				if (function.Pure)
				{
					writer.Write(" GNUC_PURE");
				}

				writer.WriteLine(";");
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", function.Name.ToUpper());
				writer.WriteLine();
			}
			
			foreach (Programmer.Function function in d_program.Functions)
			{
				writer.WriteLine("#ifdef {0}_IS_DEFINED", function.Name.ToUpper());
				writer.WriteLine("static ValueType {0} ({1})",
				                 function.Name,
				                 GenerateArgsList("x", function.NumArguments, 0, "ValueType"));
				writer.WriteLine("{");

				if (function.Inline)
				{
					writer.WriteLine("\t/* Hey! This function is strictly inlined, so don't panic! */");
				}

				writer.WriteLine("\treturn {0};", FunctionToC(function));
				writer.WriteLine("}");
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", function.Name.ToUpper());
				writer.WriteLine();
			}
		}

		private void WriteComputationNode(TextWriter writer, Computation.INode node)
		{
			WriteComputationNode(writer, node, "\t");
		}
		
		private void WriteComputationNode(TextWriter writer, Computation.INode node, string indent)
		{
			Context context = new Context(d_program, d_options);
			writer.WriteLine(ComputationNodeTranslator.Reindent(ComputationNodeTranslator.Translate(node, context), indent));
		}
		
		private void WriteComputationNodes(TextWriter writer, IEnumerable<Computation.INode> nodes)
		{
			bool empty = false;
			bool written = false;

			foreach (Computation.INode node in nodes)
			{
				if (node is Computation.Empty)
				{
					empty = true;
					continue;
				}
				else if (empty && written)
				{
					WriteComputationNode(writer, new Computation.Empty());
					empty = false;
				}

				WriteComputationNode(writer, node);
				written = true;
			}
		}
		
		private void WriteAPISource(TextWriter writer, APIFunction api)
		{
			if (api.Private && api.Body.Count == 0)
			{
				return;
			}

			writer.Write("static ");
			writer.WriteLine(api.ReturnType);
			writer.Write("{0}_{1} (", CPrefixDown, api.Name);

			for (int i = 0; i < api.Arguments.Length; i += 2)
			{
				if (i != 0)
				{
					writer.Write(", ");
				}

				var type = api.Arguments[i];
				var name = api.Arguments[i + 1];

				if (type.EndsWith("*"))
				{
					writer.Write("{0} *{1}", type.Substring(0, type.Length - 1), name);
				}
				else
				{
					writer.Write("{0} {1}", type, name);
				}
			}

			writer.WriteLine(")");

			writer.WriteLine("{");
			WriteComputationNodes(writer, api.Body);
			writer.WriteLine("}");

			writer.WriteLine();
		}

		private void WriteAPISource(TextWriter writer)
		{
			foreach (var api in d_program.APIFunctions)
			{
				WriteAPISource(writer, api);
			}

			writer.WriteLine("uint8_t");
			writer.WriteLine("cdn_rawc_{0}_get_type_size ()", CPrefixDown);
			writer.WriteLine("{");
			writer.WriteLine("\treturn (uint8_t)sizeof (ValueType);");
			writer.WriteLine("}");
			writer.WriteLine();
		}
		
		private string MinimumTableType(DataTable table)
		{
			return MinimumCType(table.MaxSize);
		}
	
		private string MinimumCType(ulong maxnum)
		{
			if (maxnum < (ulong)byte.MaxValue)
			{
				return "uint8_t";
			}
			else if (maxnum < (ulong)UInt16.MaxValue)
			{
				return "uint16_t";
			}
			else if (maxnum < (ulong)UInt32.MaxValue)
			{
				return "uint32_t";
			}
			else
			{
				return "uint64_t";
			}
		}
		
		private void WriteDataTable(TextWriter writer, DataTable table)
		{
			if (table.Count == 0)
			{
				return;
			}

			writer.WriteLine("static{0} {1} {2}[]{3} =",
			                 table.IsConstant ? " const" : "",
			                 table.IntegerType ? MinimumTableType(table) : "ValueType",
			                 table.Name,
			                 table.Columns > 0 ? String.Format("[{0}]", table.Columns) : "");
			
			writer.WriteLine("{");
			
			int cols = table.Columns > 0 ? table.Columns : System.Math.Min(10, table.Count);
			int rows = (int)System.Math.Ceiling(table.Count / (double)cols);
			int[,] colsize = new int[cols, 2];

			string[,] vals = new string[rows, cols];
			InitialValueTranslator translator = new InitialValueTranslator();
			int maxs = 0;
			
			for (int i = 0; i < table.Count; ++i)
			{
				int row = i / cols;
				int col = i % cols;
				string val = table.NeedsInitialization ? translator.Translate(table[i].Key) : InitialValueTranslator.NotInitialized;
				vals[row, col] = val;

				if (val.Length > maxs)
				{
					maxs = val.Length;
				}
				
				int pos = val.IndexOf('.');

				colsize[col, 0] = System.Math.Max(colsize[col, 0], pos == -1 ? val.Length : pos);
				colsize[col, 1] = System.Math.Max(colsize[col, 1], pos == -1 ? 0 : (val.Length - pos));
			}

			int numdec = (int)System.Math.Floor(System.Math.Log10(table.Count)) + 1;

			for (int i = 0; i < table.Count; ++i)
			{
				int row = i / cols;
				int col = i % cols;

				if (Cdn.RawC.Options.Instance.Verbose)
				{
					if (col == 0 && table.Columns > 0)
					{
						writer.WriteLine("{");
					}

					string v = vals[row, col];

					writer.Write("\t{0}", v);
					
					if (i != table.Count - 1)
					{
						writer.Write(", ".PadRight(maxs - v.Length + 2));
					}
					else
					{
						writer.Write(" ".PadLeft(maxs - v.Length + 2));
					}

					writer.WriteLine("/* {0}) {1} [{2}] */", i.ToString().PadLeft(numdec), table[i].Description, table[i].Type);
					
					if (table.Columns > 0 && col == table.Columns - 1)
					{
						writer.Write("}");
						
						if (i != table.Count - 1)
						{
							writer.Write(",");
						}
						
						writer.WriteLine();
					}

					continue;
				}
			
				if (col == 0)
				{
					writer.Write("\t");
					
					if (table.Columns > 0)
					{
						writer.Write("{ ");
					}
				}
				
				string val = vals[row, col];
				string[] parts = val.Split('.');

				int coloff = (col == 0 ? 0 : 1);
				
				if (parts.Length == 1)
				{
					writer.Write("{0}{1}", val.PadLeft(colsize[col, 0] + coloff), "".PadRight(colsize[col, 1]));
				}
				else
				{
					writer.Write("{0}.{1}", parts[0].PadLeft(colsize[col, 0] + coloff), parts[1].PadRight(colsize[col, 1] - 1));
				}
				
				if (table.Columns > 0 && col == table.Columns - 1)
				{
					writer.Write(" }");
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
			if (Cdn.RawC.Options.Instance.Validate && Knowledge.Instance.CountRandStates > 0)
			{
				// Write seed table and state table for rands
				writer.WriteLine("typedef char RandState[8];");
				writer.WriteLine("static RandState rand_states[{0}] = {{{{0,}}}};", Knowledge.Instance.CountRandStates);
				writer.WriteLine();
			}

			foreach (DataTable table in d_program.DataTables)
			{
				WriteDataTable(writer, table);
			}
		}

		private class ChildMeta
		{
			public uint Parent;
			public bool IsNode;

			public uint Index;
			public uint Next;
		}

		private class NodeMeta
		{
			public string Name;
			public uint Parent;
			public uint FirstChild;
		}

		private class StateMeta
		{
			public string Name;
			public uint Parent;
		}

		private class Meta
		{
			public List<NodeMeta> Nodes;
			public List<StateMeta> States;
			public List<ChildMeta> Children;

			public Dictionary<Cdn.Object, uint> NodeMap;
		}

		private ChildMeta AddChild(Meta meta, NodeMeta parent, ChildMeta child, ChildMeta prev)
		{
			if (prev != null)
			{
				prev.Next = (uint)meta.Children.Count;
			}
			else
			{
				parent.FirstChild = (uint)meta.Children.Count;
			}

			meta.Children.Add(child);
			return child;
		}

		private uint ExtractMeta(Meta meta, Cdn.Object obj, uint parent)
		{
			NodeMeta nm = new NodeMeta {
				Name = obj.Id,
				Parent = parent,
				FirstChild = 0
			};

			parent = (uint)meta.Nodes.Count;
			meta.Nodes.Add(nm);

			meta.NodeMap[obj] = parent;

			ChildMeta prev = null;

			var vars = obj.Variables;

			if (obj == Knowledge.Instance.Network)
			{
				var v = vars;
				vars = new Cdn.Variable[v.Length + 2];
				v.CopyTo(vars, 2);

				vars[0] = (Cdn.Variable)Knowledge.Instance.Time.Object;
				vars[1] = (Cdn.Variable)Knowledge.Instance.TimeStep.Object;
			}

			foreach (var v in vars)
			{
				DataTable.DataItem item;

				try
				{
					item = d_program.StateTable[v];
				}
				catch
				{
					continue;
				}

				ChildMeta cm = new ChildMeta {
					Parent = parent,
					IsNode = false,
					Index = (uint)item.Index,
					Next = 0
				};

				prev = AddChild(meta, nm, cm, prev);
			}

			Cdn.Node node = obj as Cdn.Node;

			if (node != null)
			{
				foreach (var child in node.Children)
				{
					uint cid = ExtractMeta(meta, child, parent);

					ChildMeta cm = new ChildMeta {
						Parent = parent,
						IsNode = true,
						Index = cid,
						Next = 0
					};

					prev = AddChild(meta, nm, cm, prev);
				}
			}

			return parent;
		}

		private Meta ExtractMeta()
		{
			Meta meta = new Meta {
				Nodes = new List<NodeMeta>(),
				States = new List<StateMeta>(d_program.StateTable.Count + 1),
				Children = new List<ChildMeta>(),
				NodeMap = new Dictionary<Cdn.Object, uint>()
			};

			// Empty root nodes
			meta.Nodes.Add(new NodeMeta {
				Name = null,
				Parent = 0,
				FirstChild = 0
			});

			meta.Children.Add(new ChildMeta {
				Parent = 0,
				IsNode = false,
				Index = 0,
				Next = 0
			});

			if (Cdn.RawC.Options.Instance.NoMetadata)
			{
				return meta;
			}

			ExtractMeta(meta, Knowledge.Instance.Network, 0);

			foreach (var item in d_program.StateTable)
			{
				Cdn.Variable v = item.Key as Cdn.Variable;

				StateMeta sm = new StateMeta {
					Name = null,
					Parent = 0
				};
				
				if (v != null)
				{
					uint nid = 0;

					if (item.Object == Knowledge.Instance.Time ||
					    item.Object == Knowledge.Instance.TimeStep)
					{
						meta.NodeMap.TryGetValue(Knowledge.Instance.Network, out nid);
					}
					else
					{
						meta.NodeMap.TryGetValue(v.Object, out nid);
					}
					
					sm.Name = v.Name;
					sm.Parent = nid;
				}
				else
				{
					break;
				}

				meta.States.Add(sm);
			}
			
			return meta;
		}
		
		private void WriteNetworkMeta(TextWriter writer)
		{
			var meta = ExtractMeta();

			writer.WriteLine("\tstatic CdnRawcStateMeta meta_states[] = {");
			
			foreach (var state in meta.States)
			{
		 		writer.WriteLine("\t\t{{ {0}, {1} }},",
		 		                 state.Name == null ? "NULL" : "\"" + state.Name + "\"",
		 		                 state.Parent);
			}
			
			writer.WriteLine("\t};");
			writer.WriteLine();			
			writer.WriteLine("\tstatic CdnRawcNodeMeta meta_nodes[] = {");
			
			foreach (var node in meta.Nodes)
			{
				writer.WriteLine("\t\t{{ {0}, {1}, {2} }},",
				                  node.Name == null ? "NULL" : "\"" + node.Name + "\"",
				                  node.Parent,
				                  node.FirstChild);
			}
			
			writer.WriteLine("\t};");
			writer.WriteLine();			
			writer.WriteLine("\tstatic CdnRawcChildMeta meta_children[] = {");
			
			foreach (var child in meta.Children)
			{
				writer.WriteLine("\t\t{{ {0}, {1}, {2}, {3} }},",
				                 child.Parent,
				                 child.IsNode ? 1 : 0,
				                 child.Index,
				                 child.Next);
			}
			
			writer.WriteLine("\t};");
			writer.WriteLine();			
		}

		private string EventNodeType(EventNodeState ev)
		{
			switch (ev.Node.CompareType)
			{
			case Cdn.MathFunctionType.Less:
				return "CDN_RAWC_EVENT_STATE_TYPE_LESS";
			case Cdn.MathFunctionType.LessOrEqual:
				return "CDN_RAWC_EVENT_STATE_TYPE_LESS_OR_EQUAL";
			case Cdn.MathFunctionType.Greater:
				return "CDN_RAWC_EVENT_STATE_TYPE_GREATER";
			case Cdn.MathFunctionType.GreaterOrEqual:
				return "CDN_RAWC_EVENT_STATE_TYPE_GREATER_OR_EQUAL";
			case Cdn.MathFunctionType.Equal:
				return "CDN_RAWC_EVENT_STATE_TYPE_EQUAL";
			case Cdn.MathFunctionType.And:
				return "CDN_RAWC_EVENT_STATE_TYPE_AND";
			case Cdn.MathFunctionType.Or:
				return "CDN_RAWC_EVENT_STATE_TYPE_OR";
			default:
				return null;
			}
		}

		private void WriteNetworkEvents(TextWriter writer)
		{
			writer.WriteLine("\tstatic CdnRawcEvent events[] = {");

			Dictionary<Cdn.EventLogicalNode, int> nodemap = new Dictionary<EventLogicalNode, int>();
			int i = 0;

			foreach (var ev in Knowledge.Instance.EventNodeStates)
			{
				if (ev.Type != EventNodeState.StateType.Previous)
				{
					continue;
				}

				nodemap[ev.Node] = i;
				i++;
			}

			foreach (var ev in Knowledge.Instance.EventNodeStates)
			{
				if (ev.Type != EventNodeState.StateType.Previous)
				{
					continue;
				}

				writer.Write("\t\t{ ");

				string approx;

				if (ev.Event.Approximation == double.MaxValue)
				{
					if (ValueType == "float")
					{
						approx = "FLT_MAX";
					}
					else
					{
						approx = "DBL_MAX";
					}
				}
				else
				{
					approx = NumberTranslator.Translate(ev.Event.Approximation, new Context(d_program, d_options));
				}

				writer.Write("{0}, {1}, {2}, {3}",
				             EventNodeType(ev),
				             approx,
				             ev.Node.Left != null ? nodemap[ev.Node.Left] : 0,
				             ev.Node.Right != null ? nodemap[ev.Node.Right] : 0);

				writer.WriteLine(" },");
			}

			writer.WriteLine("\t};");
			writer.WriteLine();
		}

		private void WriteNetwork(TextWriter writer)
		{
			var pref = CPrefixDown;

			writer.WriteLine("CdnRawcNetwork *");
			writer.WriteLine("cdn_rawc_{0}_network ()", pref);
			writer.WriteLine("{");
			
			WriteNetworkMeta(writer);
			WriteNetworkEvents(writer);

			writer.WriteLine("\tstatic CdnRawcNetwork network = {");

			var funcs = new string[] {
				"prepare",
				"init",
				"reset",
				"pre",
				"prediff",
				"diff",
				"post",
				"events_update",
				"event_active",
				"event_fire",
				"event_get_value",
			};

			foreach (string name in funcs)
			{
				writer.WriteLine("\t\t.{0} = {1}_{0},", name, pref);
			}

			writer.WriteLine();

			var enu = Knowledge.Instance.Integrated.GetEnumerator();
			if (enu.MoveNext())
			{
				var integrated = d_program.StateTable[enu.Current];

				writer.WriteLine("\t\t.states = {{.start = {0}, .end = {1}}},",
				                 integrated.AliasOrIndex,
				                 integrated.Index + Knowledge.Instance.IntegratedCount);
			}
			else
			{
				writer.WriteLine("\t\t.states = {.start = 0, .end = 0},");
			}

			enu = Knowledge.Instance.DerivativeStates.GetEnumerator();

			if (enu.MoveNext())
			{
				var derivatives = d_program.StateTable[enu.Current];

				writer.WriteLine("\t\t.derivatives = {{.start = {0}, .end = {1}}},",
				                 derivatives.AliasOrIndex,
				                 derivatives.Index + Knowledge.Instance.DerivativeStatesCount);
			}
			else
			{
				writer.WriteLine("\t\t.derivatives = {.start = 0, .end = 0},");
			}

			enu = Knowledge.Instance.EventNodeStates.GetEnumerator();

			if (enu.MoveNext())
			{
				var eventvalues = d_program.StateTable[enu.Current];

				writer.WriteLine("\t\t.event_values = {{.start = {0}, .end = {1}}},",
				                 eventvalues.AliasOrIndex,
				                 eventvalues.Index + Knowledge.Instance.EventNodeStatesCount);
			}
			else
			{
				writer.WriteLine("\t\t.derivatives = {.start = 0, .end = 0},");
			}

			writer.WriteLine();
			writer.WriteLine("\t\t.data_size = CDN_RAWC_{0}_DATA_SIZE,", CPrefixUp);
			writer.WriteLine("\t\t.type_size = sizeof (ValueType),");

			writer.WriteLine();
			writer.WriteLine("\t\t.events_size = {0},", Knowledge.Instance.EventsCount);
			writer.WriteLine("\t\t.events_size_all = {0},", Knowledge.Instance.EventNodeStatesCount / 3);
			writer.WriteLine("\t\t.events = events,");

			var t = d_program.StateTable[Knowledge.Instance.Time];
			var dt = d_program.StateTable[Knowledge.Instance.TimeStep];

			writer.WriteLine();
			writer.WriteLine("\t\t.meta = {");
			writer.WriteLine("\t\t\t.t = {0},", t.AliasOrIndex);
			writer.WriteLine("\t\t\t.dt = {0},", dt.AliasOrIndex);
			writer.WriteLine();

			if (!Cdn.RawC.Options.Instance.NoMetadata)
			{
				writer.WriteLine(String.Format("\t\t\t.name = \"{0}\",", CPrefixDown));
			}
			else
			{
				writer.WriteLine("\t\t\t.name = 0,");
			}

			writer.WriteLine();

			writer.WriteLine("\t\t\t.states = meta_states,");
			writer.WriteLine("\t\t\t.states_size = sizeof (meta_states) / sizeof (CdnRawcStateMeta),");
			writer.WriteLine();
			writer.WriteLine("\t\t\t.nodes = meta_nodes,");
			writer.WriteLine("\t\t\t.nodes_size = sizeof (meta_nodes) / sizeof (CdnRawcNodeMeta),");
			writer.WriteLine();
			writer.WriteLine("\t\t\t.children = meta_children,");
			writer.WriteLine("\t\t\t.children_size = sizeof (meta_children) / sizeof (CdnRawcChildMeta),");
			writer.WriteLine();
			writer.WriteLine("\t\t},");

			writer.WriteLine("\t};");

			writer.WriteLine();
			writer.WriteLine("\treturn &network;");

			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteEventsSource(TextWriter writer)
		{
			WriteEventGetValue(writer);
			WriteEventActive(writer);
			WriteEventFire(writer);
		}

		private void WriteEventActive(TextWriter writer)
		{
			writer.WriteLine("static uint8_t");
			writer.WriteLine("{0}_event_active (ValueType *{1}, uint32_t i)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			writer.WriteLine("{");

			writer.WriteLine("\t{0} const *evstates = ({0} const *)({1} + {2});",
			                 EventStateType,
			                 d_program.StateTable.Name,
			                 d_program.StateTable.Count);

			writer.WriteLine();
			writer.WriteLine("\tswitch (i)");
			writer.WriteLine("\t{");

			int i = 0;

			foreach (var ev in Knowledge.Instance.Events)
			{
				var phases = ev.Phases;
				++i;

				if (phases.Length == 0)
				{
					continue;
				}

				writer.WriteLine("\tcase {0}:", i - 1);

				var parent = Knowledge.Instance.FindStateNode(ev);
				var cont = Knowledge.Instance.EventStatesMap[parent];
				var idx = cont.Index;

				List<string> conditions = new List<string>();

				foreach (var ph in phases)
				{
					var st = Knowledge.Instance.GetEventState(parent, ph);

					conditions.Add(String.Format("evstates[{0}] == {1}", idx, st.Index));
				}

				writer.WriteLine("\t\treturn ({0});", String.Join(" || ", conditions));
			}

			writer.WriteLine("\tdefault:");
			writer.WriteLine("\t\treturn 1;");

			writer.WriteLine("\t}");
			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteEventFire(TextWriter writer)
		{
			writer.WriteLine("static void");
			writer.WriteLine("{0}_event_fire (ValueType *{1}, uint32_t i)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			writer.WriteLine("{");

			writer.WriteLine("\t{0} *evstates = ({0} *)({1} + {2});",
			                 EventStateType,
			                 d_program.StateTable.Name,
			                 d_program.StateTable.Count);

			writer.WriteLine();
			writer.WriteLine("\tswitch (i)");
			writer.WriteLine("\t{");

			int i = 0;

			foreach (var ev in Knowledge.Instance.Events)
			{
				var state = ev.GotoState;
				++i;

				var prg = d_program.EventProgram(ev);

				if (String.IsNullOrEmpty(state) && prg == null)
				{
					continue;
				}

				writer.WriteLine("\tcase {0}:", i - 1);

				if (!String.IsNullOrEmpty(state))
				{
					var parent = Knowledge.Instance.FindStateNode(ev);
					var cont = Knowledge.Instance.EventStatesMap[parent];
					var idx = cont.Index;
					var st = Knowledge.Instance.GetEventState(parent, state);

					writer.WriteLine("\t\tevstates[{0}] = {1};",
					                 idx,
					                 st.Index);
				}

				if (prg != null)
				{
					WriteComputationNode(writer, prg, "\t\t");
				}

				writer.WriteLine("\t\tbreak;");
			}

			writer.WriteLine("\tdefault:");
			writer.WriteLine("\t\tbreak;");

			writer.WriteLine("\t}");
			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteEventGetValue(TextWriter writer)
		{
			writer.WriteLine("static CdnRawcEventValue *");
			writer.WriteLine("{0}_event_get_value (ValueType *data, uint32_t i)", CPrefixDown);
			writer.WriteLine("{");

			var enu = Knowledge.Instance.EventNodeStates.GetEnumerator();
			int start;

			if (enu.MoveNext())
			{
				start = d_program.StateTable[enu.Current].Index;
			}
			else
			{
				start = d_program.StateTable.Count;
			}

			writer.WriteLine("\treturn (CdnRawcEventValue *)(data + {0} + i);",
			                 start);

			writer.WriteLine("}");
			writer.WriteLine();
		}

		private string EventStateType
		{
			get { return MinimumCType((ulong)Knowledge.Instance.EventStates.Count); }
		}
		
		private void WriteSource()
		{
			d_options.CPrefixDown = CPrefixDown;
			d_options.EventStateType = EventStateType;

			d_sourceFilename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".c");
			TextWriter writer = new StreamWriter(d_sourceFilename);
			
			writer.WriteLine("#include \"{0}.h\"", d_program.Options.Basename);
			writer.WriteLine("#include <stdint.h>");
			writer.WriteLine("#include <float.h>");
			writer.WriteLine("#include <string.h>");
			writer.WriteLine("#include <cdn-rawc/cdn-rawc-macros.h>");
			
			writer.WriteLine();
			WriteCustomMathRequired(writer);
			writer.WriteLine();

			if (d_options.CustomHeaders != null)
			{
				foreach (string header in d_options.CustomHeaders)
				{
					string path = header;

					if (Cdn.RawC.Options.Instance.Validate && !Path.IsPathRooted(path))
					{
						// For validation, include a absolute path for custom
						// headers, relative to the original output path
						path = Path.GetFullPath(Path.Combine(d_program.Options.OriginalOutput, header));
					}

					writer.WriteLine("#include \"{0}\"", path);
				}
			}

			writer.WriteLine("#include <cdn-rawc/cdn-rawc-math.h>");
			writer.WriteLine();

			WriteFunctionDefines(writer);
			
			WriteDataTables(writer);
			WriteFunctions(writer);
			WriteAPISource(writer);

			WriteEventsSource(writer);

			WriteNetwork(writer);

			writer.Close();
		}	
	}
}

