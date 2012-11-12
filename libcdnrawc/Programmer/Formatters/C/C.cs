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
				values.Add(item.DataIndex.ToString());
				comments.Add(comment);
				
				maxname = System.Math.Max(maxname, enumname.Length);
				maxval = System.Math.Max(maxval, item.DataIndex.ToString().Length);

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

		private bool NeedsSpaceForEvents()
		{
			if (Knowledge.Instance.EventsCount == 0)
			{
				return false;
			}

			foreach (var ev in Knowledge.Instance.Events)
			{
				if (ev.Approximation != Double.MaxValue)
				{
					return true;
				}
			}

			return false;
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

			writer.WriteLine("typedef struct");
			writer.WriteLine("{");
			writer.WriteLine("\tValueType data[{0}];", d_program.StateTable.Size);
			writer.WriteLine("\t{0} event_states[{1}];", EventStateType, Knowledge.Instance.EventContainersCount);
			writer.WriteLine("\t{0} events_active[{1}];", EventType, Knowledge.Instance.EventsCount);
			writer.WriteLine("\tuint32_t events_active_size;");
			writer.WriteLine("}} CdnRawcNetwork{0};", CPrefix);
			writer.WriteLine();

			writer.WriteLine("#define CDN_RAWC_NETWORK_{0}_SIZE sizeof(CdnRawcNetwork{1})", CPrefixUp, CPrefix);
			writer.WriteLine("#define CDN_RAWC_NETWORK_{0}_SPACE_FOR_EVENTS {1}",
			                 CPrefixUp,
			                 NeedsSpaceForEvents() ? 1 : 0);

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
		
		private string GenerateArgsList(Programmer.Function function)
		{
			List<string> ret = new List<string>(function.NumArguments + 2);

			ret.Add(String.Format("ValueType *{0}", d_program.StateTable.Name));

			if (!function.Expression.Dimension.IsOne)
			{
				ret.Add("ValueType *ret");
			}

			int i = 0;

			foreach (var arg in function.OrderedArguments)
			{
				var child = function.Expression.FromPath(arg.Path);

				if (child.Dimension.IsOne)
				{
					ret.Add(String.Format("ValueType x{0}", i));
				}
				else
				{
					ret.Add(String.Format("ValueType *x{0}", i));
				}

				++i;
			}
			
			return String.Join(", ", ret.ToArray());
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
			
			foreach (var instr in d_program.CollectInstructions<Cdn.InstructionRand>())
			{
				string def = "CDN_MATH_RAND";
				
				if (generated.Add(def))
				{
					writer.WriteLine("#define {0}_REQUIRED", def);
				}

				break;
			}

			bool hasabs = generated.Contains("CDN_MATH_ABS");
			bool hasmax = generated.Contains("CDN_MATH_MAX");

			if (!hasabs || !hasmax)
			{
				foreach (var st in Knowledge.Instance.EventNodeStates)
				{
					if (!hasabs &&
					    st.Node.CompareType == MathFunctionType.Equal &&
					    st.Event.Approximation != Double.MaxValue)
					{
						generated.Add("CDN_MATH_ABS");
						hasabs = true;

						writer.WriteLine("#define CDN_MATH_ABS_REQUIRED");

						if (hasmax)
						{
							break;
						}
					}

					if (!hasmax &&
					    (st.Node.CompareType == Cdn.MathFunctionType.And ||
					     st.Node.CompareType == Cdn.MathFunctionType.Or))
					{
						generated.Add("CDN_MATH_MAX");
						hasmax = true;

						writer.WriteLine("#define CDN_MATH_MAX_REQUIRED");

						if (hasabs)
						{
							break;
						}
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
		
		private string FunctionToC(Function function, out Context context)
		{
			context = new Context(d_program, d_options, function.Expression, GenerateMapping("x{0}", function.Arguments));

			var ism = !function.Expression.Dimension.IsOne;

			if (ism)
			{
				context.PushRet("ret");
			}

			var ret = InstructionTranslator.QuickTranslate(context);

			if (ism)
			{
				context.PopRet();
			}

			return ret;
		}

		private void WriteFunction(TextWriter writer, Programmer.Function function)
		{
			var expr = function.Expression;
			var isone = expr.Dimension.IsOne;

			writer.WriteLine("#ifdef {0}_IS_DEFINED", function.Name.ToUpper());

			writer.Write("static ValueType ");

			if (!isone)
			{
				writer.Write("*");
			}

			writer.WriteLine("{0} ({1})",
			                 function.Name,
			                 GenerateArgsList(function));
			writer.WriteLine("{");

			if (function.Inline)
			{
				writer.WriteLine("\t/* Hey! This function is strictly inlined, so don't panic! */");
			}

			Context context;
			var retval = FunctionToC(function, out context);

			foreach (var tmp in context.TemporaryStorage)
			{
				writer.WriteLine("\tValueType {0}[{1}] = {{0,}};", tmp.Name, tmp.Size);
			}

			if (context.TemporaryStorage.Count != 0)
			{
				writer.WriteLine();
			}

			writer.WriteLine("\treturn {0};", Context.Reindent(retval, "\t").Substring(1));
			writer.WriteLine("}");
			writer.WriteLine("#endif /* {0}_IS_DEFINED */", function.Name.ToUpper());
			writer.WriteLine();
		}

		private void WriteFunctionDecl(TextWriter writer, Programmer.Function function)
		{
			writer.WriteLine("#ifdef {0}_IS_DEFINED", function.Name.ToUpper());

			writer.Write("static ");

			if (function.Inline)
			{
				writer.Write("inline ");
			}

			writer.Write("ValueType ");

			if (!function.Expression.Dimension.IsOne)
			{
				writer.Write("*");
			}

			writer.Write("{0} ({1})",
			             function.Name,
			             GenerateArgsList(function));

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
		
		private void WriteFunctions(TextWriter writer)
		{
			// Write declarations
			foreach (var function in d_program.Functions)
			{
				WriteFunctionDecl(writer, function);
			}

			// API Function declarations
			foreach (var api in d_program.APIFunctions)
			{
				if (api.Private && api.Body.Count == 0)
				{
					continue;
				}

				WriteAPIDecl(writer, api, false);
				writer.WriteLine(";");
			}

			writer.WriteLine("static void {0}_events_update_distance (void *data);", CPrefixDown);
			writer.WriteLine("static void {0}_events_post_update (void *data);", CPrefixDown);
			writer.WriteLine("static uint32_t {0}_get_events_active_size (void *data);", CPrefixDown);
			writer.WriteLine("static uint32_t {0}_get_events_active (void *data, uint32_t i);", CPrefixDown);
			writer.WriteLine("static CdnRawcEventValue *{0}_get_events_value (void *data, uint32_t i);", CPrefixDown);

			writer.WriteLine();
						
			foreach (var function in d_program.Functions)
			{
				WriteFunction(writer, function);
			}
		}

		private void WriteComputationNode(TextWriter writer, Computation.INode node)
		{
			WriteComputationNode(writer, node, "\t");
		}
		
		private void WriteComputationNode(TextWriter writer, Computation.INode node, string indent)
		{
			Context context = new Context(d_program, d_options);
			var ret = Context.Reindent(ComputationNodeTranslator.Translate(node, context), indent);
			
			if (context.TemporaryStorage.Count != 0)
			{
				writer.WriteLine("{0}{{", indent);
				
				foreach (Context.Temporary tmp in context.TemporaryStorage)
				{
					writer.WriteLine("{0}ValueType {1}[{2}] = {{0,}};", "\t" + indent, tmp.Name, tmp.Size);
				}
				
				writer.WriteLine();
				writer.WriteLine(Context.Reindent(ret, "\t"));
				
				writer.WriteLine("{0}}}", indent);
			}
			else
			{
				writer.WriteLine(ret);
			}
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

		private void WriteAPIDecl(TextWriter writer, APIFunction api, bool hasbody)
		{
			if (api.Private && api.Body.Count == 0)
			{
				return;
			}

			writer.Write("static ");
			writer.Write(api.ReturnType);

			if (hasbody)
			{
				writer.WriteLine();
			}
			else
			{
				writer.Write(" ");
			}

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

			writer.Write(")");
		}

		private void WriteNetworkVariable(TextWriter writer)
		{
			WriteNetworkVariable(writer, null);
		}

		private void WriteNetworkVariable(TextWriter writer, string decl)
		{
			WriteNetworkVariable(writer, decl, true);
		}

		private void WriteNetworkVariable(TextWriter writer, string decl, bool needsss)
		{
			writer.WriteLine("\tCdnRawcNetwork{0} *network = data;", CPrefix);

			if (needsss)
			{
				writer.WriteLine("\tValueType *{0} GNUC_UNUSED;", d_program.StateTable.Name);
			}

			if (decl != null)
			{
				writer.WriteLine(decl);
			}

			writer.WriteLine();

			if (needsss)
			{
				writer.WriteLine("\t{0} = network->data;", d_program.StateTable.Name);
				writer.WriteLine();
			}
		}
		
		private void WriteAPISource(TextWriter writer, APIFunction api)
		{
			if (api.Private && api.Body.Count == 0)
			{
				return;
			}

			WriteAPIDecl(writer, api, true);
			writer.WriteLine();

			writer.WriteLine("{");

			if (api.Body.Count > 0)
			{
				WriteNetworkVariable(writer);
				WriteComputationNodes(writer, api.Body);
			}

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
					Index = (uint)item.DataIndex,
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

		private void WriteNetwork(TextWriter writer)
		{
			var pref = CPrefixDown;

			writer.WriteLine("CdnRawcNetwork *");
			writer.WriteLine("cdn_rawc_{0}_network ()", pref);
			writer.WriteLine("{");
			
			WriteNetworkMeta(writer);

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
				"events_post_update",
				"events_fire",
				"get_data",
				"get_states",
				"get_derivatives",
				"get_nth",
				"get_events_active",
				"get_events_active_size",
				"get_events_value",
			};

			foreach (string name in funcs)
			{
				writer.WriteLine("\t\t.{0} = {1}_{0},", name, pref);
			}

			writer.WriteLine();

			var range = d_program.StateRange(Knowledge.Instance.Integrated);

			writer.WriteLine("\t\t.states = {{.start = {0}, .end = {1}, .stride = 1}},",
			                 range[0],
			                 range[1]);

			range = d_program.StateRange(Knowledge.Instance.DerivativeStates);

			writer.WriteLine("\t\t.derivatives = {{.start = {0}, .end = {1}, .stride = 1}},",
			                 range[0],
			                 range[1]);

			range = d_program.StateRange(Knowledge.Instance.EventNodeStates);

			writer.WriteLine("\t\t.event_values = {{.start = {0}, .end = {1}, .stride = 3}},",
			                 range[0],
			                 range[0] + (range[1] - range[0]) * 3);

			writer.WriteLine();
			writer.WriteLine("\t\t.size = CDN_RAWC_NETWORK_{0}_SIZE,", CPrefixUp);
			writer.WriteLine("\t\t.data_size = sizeof (ValueType) * {0},", d_program.StateTable.Size);
			writer.WriteLine("\t\t.event_refinement = {0},", NeedsSpaceForEvents() ? 1 : 0);
			writer.WriteLine("\t\t.type_size = sizeof (ValueType),");
			writer.WriteLine("\t\t.minimum_timestep = {0},", Knowledge.Instance.Network.Integrator.MinimumTimestep);

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
			writer.WriteLine("\t\t},");

			writer.WriteLine("\t};");

			writer.WriteLine();
			writer.WriteLine("\treturn &network;");

			writer.WriteLine("}");
			writer.WriteLine();
		}

		private string EventNodeStateVariable(Cdn.EventLogicalNode node, EventNodeState.StateType type)
		{
			var st = d_program.StateTable[EventNodeState.Key(node, type)];

			return String.Format("{0}[{1}]", d_program.StateTable.Name, st.AliasOrIndex);
		}

		private string EventConditionHolds(Cdn.Event ev, Cdn.EventLogicalNode node, EventNodeState.StateType type)
		{
			var st = EventNodeStateVariable(node, type);

			switch (node.CompareType)
			{
			case Cdn.MathFunctionType.Less:
			case Cdn.MathFunctionType.Greater:
				return String.Format("{0} > 0",
				                     st);
			case Cdn.MathFunctionType.LessOrEqual:
			case Cdn.MathFunctionType.GreaterOrEqual:
			case Cdn.MathFunctionType.And:
			case Cdn.MathFunctionType.Or:
				return String.Format("{0} >= 0",
				                     st);
			case Cdn.MathFunctionType.Equal:
				if (ev.Approximation != Double.MaxValue)
				{
					var approx = NumberTranslator.Translate(ev.Approximation, new Context(d_program, d_options));

					return String.Format("CDN_MATH_ABS ({0}) <= {1}",
					                     st,
					                     approx);
				}
				else
				{
					return "1";
				}
			default:
				return "0";
			}
		}

		private void WriteEventsUpdateDistance(TextWriter writer)
		{
			writer.WriteLine("static void");
			writer.WriteLine("{0}_events_update_distance (void *data)",
			                 CPrefixDown);

			writer.WriteLine("{");

			// Write updates of the logical nodes
			var states = new List<EventNodeState>(Knowledge.Instance.EventNodeStates);

			if (states.Count == 0)
			{
				writer.WriteLine("}");
				writer.WriteLine();
				return;
			}

			WriteNetworkVariable(writer);

			bool first = true;

			// Compute in reverse order to get the right dependencies
			for (int i = states.Count - 1; i >= 0; --i)
			{
				var st = states[i];

				if (st.Type != EventNodeState.StateType.Current)
				{
					continue;
				}

				if (first)
				{
					first = false;
				}
				else
				{
					writer.WriteLine();
				}

				switch (st.Node.CompareType)
				{
				case Cdn.MathFunctionType.And:
				case Cdn.MathFunctionType.Or:
				{
					var dist = EventNodeStateVariable(st.Node, EventNodeState.StateType.Distance);
					var cur = EventNodeStateVariable(st.Node, EventNodeState.StateType.Current);
					var ldist = EventNodeStateVariable(st.Node.Left, EventNodeState.StateType.Distance);
					var rdist = EventNodeStateVariable(st.Node.Right, EventNodeState.StateType.Distance);

					if (st.Node.CompareType == Cdn.MathFunctionType.And)
					{
						var lcond = EventConditionHolds(st.Event, st.Node.Left, EventNodeState.StateType.Current);
						var rcond = EventConditionHolds(st.Event, st.Node.Right, EventNodeState.StateType.Current);

						writer.WriteLine("\tif ({0} >= 0 && {1} >= 0)", ldist, rdist);
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = CDN_MATH_MAX ({1}, {2});", dist, ldist, rdist);
						writer.WriteLine("\t}");
						writer.WriteLine("\telse if ({0} >= 0 && {1})", ldist, rcond);
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = {1};", dist, ldist);
						writer.WriteLine("\t}");
						writer.WriteLine("\telse if ({0} >= 0 && {1})", rdist, lcond);
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = {1};", dist, rdist);
						writer.WriteLine("\t}");
						writer.WriteLine("\telse");
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = -1;", dist);
						writer.WriteLine("\t}");
						writer.WriteLine();
					}
					else
					{
						writer.WriteLine("\t{0} = CDN_MATH_MAX ({1}, {2});", dist, ldist, rdist);
					}

					writer.WriteLine("\t{0} = ({1} >= 0 ? 0 : -1);", cur, dist);
				}
				break;
				default:
				{
					var prevCond = EventConditionHolds(st.Event, st.Node, EventNodeState.StateType.Previous);
					var curCond = EventConditionHolds(st.Event, st.Node, EventNodeState.StateType.Current);

					var dist = EventNodeStateVariable(st.Node, EventNodeState.StateType.Distance);
					var prev = EventNodeStateVariable(st.Node, EventNodeState.StateType.Previous);
					var cur = EventNodeStateVariable(st.Node, EventNodeState.StateType.Current);

					// Compute distance for actual values
					writer.WriteLine("\tif (!({0}) && {1})", prevCond, curCond);
					writer.WriteLine("\t{");

					if (st.Event.Approximation == Double.MaxValue)
					{
						writer.WriteLine("\t\t{0} = 1;", dist);
					}
					else
					{
						var approx = NumberTranslator.Translate(st.Event.Approximation, new Context(d_program, d_options));

						writer.WriteLine("\t\tif ({0} <= {1})", cur, approx);
						writer.WriteLine("\t\t{");
						writer.WriteLine("\t\t\t{0} = 1;", dist);
						writer.WriteLine("\t\t}");
						writer.WriteLine("\t\telse");
						writer.WriteLine("\t\t{");
						writer.Write("\t\t\t{0} = {1} / ({1} - {2})", dist, prev, cur);
						
						if (st.Node.CompareType == Cdn.MathFunctionType.Less ||
						    st.Node.CompareType == Cdn.MathFunctionType.Greater)
						{
							writer.WriteLine(" + 1e-10;");
						}
						else
						{
							writer.WriteLine(";");
						}

						writer.WriteLine("\t\t}");
					}

					writer.WriteLine("\t}");
					writer.WriteLine("\telse");
					writer.WriteLine("\t{");
					writer.WriteLine("\t\t{0} = -1;", dist);
					writer.WriteLine("\t}");

					break;
				}
				}
			}

			var range = d_program.StateRange(Knowledge.Instance.EventNodeStates);

			writer.WriteLine();
			writer.WriteLine("\t{");
			writer.WriteLine("\tuint32_t i;");
			writer.WriteLine();
			writer.WriteLine("\tnetwork->events_active_size = 0;");
			writer.WriteLine();
			writer.WriteLine("\tfor (i = 0; i < {0}; ++i)", Knowledge.Instance.EventsCount);
			writer.WriteLine("\t{");
			writer.WriteLine("\t\tif ({0}_event_active (data, i) && {1}[{2} + i * 3] >= 0)",
			                 CPrefixDown,
			                 d_program.StateTable.Name,
			                 range[0] + 2);

			writer.WriteLine("\t\t{");
			writer.WriteLine("\t\t\tuint32_t j;");
			writer.WriteLine("\t\t\tCdnRawcEventValue *vi = {0}_get_events_value (data, i);", CPrefixDown);
			writer.WriteLine();
			writer.WriteLine("\t\t\tfor (j = network->events_active_size; j > 0; --j)");
			writer.WriteLine("\t\t\t{");
			writer.WriteLine("\t\t\t\tCdnRawcEventValue *vj = {0}_get_events_value (data, network->events_active[j - 1]);", CPrefixDown);
			writer.WriteLine();
			writer.WriteLine("\t\t\t\tif (vj->distance <= vi->distance)");
			writer.WriteLine("\t\t\t\t{");
			writer.WriteLine("\t\t\t\t\tbreak;");
			writer.WriteLine("\t\t\t\t}");
			writer.WriteLine("\t\t\t\telse");
			writer.WriteLine("\t\t\t\t{");
			writer.WriteLine("\t\t\t\t\tnetwork->events_active[j] = network->events_active[j - 1];");
			writer.WriteLine("\t\t\t\t}");
			writer.WriteLine("\t\t\t}");
			writer.WriteLine();
			writer.WriteLine("\t\t\tnetwork->events_active[j] = i;");
			writer.WriteLine("\t\t\t++network->events_active_size;");
			writer.WriteLine("\t\t}");
			writer.WriteLine("\t}");
			writer.WriteLine("\t}");
			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteEventsPostUpdate(TextWriter writer)
		{
			writer.WriteLine("static void");
			writer.WriteLine("{0}_events_post_update (void *data)",
			                 CPrefixDown);

			writer.WriteLine("{");

			var enu = Knowledge.Instance.EventNodeStates.GetEnumerator();

			if (enu.MoveNext())
			{
				WriteNetworkVariable(writer);

				var st = d_program.StateTable[enu.Current];
				var idx = st.AliasOrIndex;

				writer.WriteLine("\tint i;");
				writer.WriteLine();

				writer.WriteLine("\tfor (i = {0}; i < {1}; i += 3)", idx, st.DataIndex + Knowledge.Instance.EventNodeStatesCount);
				writer.WriteLine("\t{");
				writer.WriteLine("\t\t{0}[i] = {0}[i + 1];",
				                 d_program.StateTable.Name);
				writer.WriteLine("\t}");
			}

			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteEventsSource(TextWriter writer)
		{
			WriteEventActive(writer);
			WriteEventFire(writer);
			WriteEventsUpdateDistance(writer);
			WriteEventsPostUpdate(writer);
		}

		private void WriteEventActive(TextWriter writer)
		{
			if (Knowledge.Instance.EventNodeStatesCount == 0)
			{
				return;
			}

			writer.WriteLine("static uint8_t");
			writer.WriteLine("{0}_event_active (void *data, uint32_t i)",
			                 CPrefixDown);

			writer.WriteLine("{");

			bool hasit = false;

			foreach (var ev in Knowledge.Instance.Events)
			{
				if (ev.Phases.Length > 0)
				{
					hasit = true;
					break;
				}
			}

			if (!hasit)
			{
				writer.WriteLine("\treturn 1;");
			}
			else
			{
				WriteNetworkVariable(writer, String.Format("\t{0} const *evstates;", EventStateType));

				writer.WriteLine("\tevstates = network->event_states;");

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
			}

			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteEventFire(TextWriter writer)
		{
			writer.WriteLine("static void");
			writer.WriteLine("{0}_events_fire (void *data)",
			                 CPrefixDown);

			writer.WriteLine("{");

			bool hasit = false;

			foreach (var ev in Knowledge.Instance.Events)
			{
				var state = ev.GotoState;
				var prg = d_program.EventProgram(ev);

				if (!String.IsNullOrEmpty(state) || prg != null)
				{
					hasit = true;
					break;
				}
			}

			if (!hasit)
			{
				writer.WriteLine("}");
				writer.WriteLine();
				return;
			}

			WriteNetworkVariable(writer, "\tuint32_t event;");

			writer.WriteLine("\tfor (event = 0; event < network->events_active_size; ++event)");
			writer.WriteLine("\t{");
			writer.WriteLine("\t\tuint32_t i = network->events_active[event];");

			writer.WriteLine();
			writer.WriteLine("\t\tswitch (i)");
			writer.WriteLine("\t\t{");

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

				writer.WriteLine("\t\tcase {0}:", i - 1);
				writer.WriteLine("\t\t\tif ({0}_event_active (data, {1}))", CPrefixDown, i - 1);
				writer.WriteLine("\t\t\t{");

				if (!String.IsNullOrEmpty(state))
				{
					var parent = Knowledge.Instance.FindStateNode(ev);
					var cont = Knowledge.Instance.EventStatesMap[parent];
					var idx = cont.Index;
					var st = Knowledge.Instance.GetEventState(parent, state);

					writer.WriteLine("\t\t\t\tnetwork->evstates[{0}] = {1};",
					                 idx,
					                 st.Index);
				}

				if (prg != null)
				{
					WriteComputationNode(writer, prg, "\t\t\t\t");
				}

				writer.WriteLine("\t\t\t}");

				writer.WriteLine("\t\t\tbreak;");
			}

			writer.WriteLine("\t\tdefault:");
			writer.WriteLine("\t\t\tbreak;");

			writer.WriteLine("\t\t}");
			writer.WriteLine("\t}");
			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteDataAccessors(TextWriter writer)
		{
			writer.WriteLine("static ValueType *");
			writer.WriteLine("{0}_get_data (void *data)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			writer.WriteLine("{");
			WriteNetworkVariable(writer);
			writer.WriteLine("\treturn ss;");
			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static ValueType *");
			writer.WriteLine("{0}_get_states (void *data)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			var range = d_program.StateRange(Knowledge.Instance.Integrated);

			writer.WriteLine("{");
			WriteNetworkVariable(writer);
			writer.WriteLine("\treturn ss + {0};", range[0]);
			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static ValueType *");
			writer.WriteLine("{0}_get_derivatives (void *data)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			range = d_program.StateRange(Knowledge.Instance.DerivativeStates);

			writer.WriteLine("{");
			WriteNetworkVariable(writer);
			writer.WriteLine("\treturn ss + {0};", range[0]);
			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static void *");
			writer.WriteLine("{0}_get_nth (void *data, uint32_t nth)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			writer.WriteLine("{");
			WriteNetworkVariable(writer);
			writer.WriteLine("\treturn network + nth;");
			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static uint32_t");
			writer.WriteLine("{0}_get_events_active_size (void *data)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			writer.WriteLine("{");
			WriteNetworkVariable(writer, null, false);
			writer.WriteLine("\treturn network->events_active_size;");
			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static uint32_t");
			writer.WriteLine("{0}_get_events_active (void *data, uint32_t i)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			writer.WriteLine("{");
			WriteNetworkVariable(writer, null, false);
			writer.WriteLine("\treturn (uint32_t)network->events_active[i];");
			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static CdnRawcEventValue *");
			writer.WriteLine("{0}_get_events_value (void *data, uint32_t i)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			range = d_program.StateRange(Knowledge.Instance.EventNodeStates);

			writer.WriteLine("{");
			WriteNetworkVariable(writer);
			writer.WriteLine("\treturn (CdnRawcEventValue *)({0} + {1} + i * 3);",
			                 d_program.StateTable.Name,
			                 range[0]);
			writer.WriteLine("}");
			writer.WriteLine();
		}

		private string EventStateType
		{
			get { return MinimumCType((ulong)Knowledge.Instance.EventStates.Count); }
		}

		private string EventType
		{
			get { return MinimumCType((ulong)Knowledge.Instance.EventsCount); }
		}
		
		private void WriteSource()
		{
			d_options.CPrefix = CPrefix;
			d_options.CPrefixDown = CPrefixDown;
			d_options.CPrefixUp = CPrefixUp;
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
			
			StringWriter source = new StringWriter();

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

					source.WriteLine("#include \"{0}\"", path);
				}
			}

			source.WriteLine("#include <cdn-rawc/cdn-rawc-math.h>");
			source.WriteLine();

			WriteFunctionDefines(source);
			
			WriteDataTables(source);

			WriteFunctions(source);
			WriteAPISource(source);

			WriteEventsSource(source);
			WriteDataAccessors(source);

			WriteNetwork(source);
			
			foreach (var def in Context.MathDefines)
			{
				writer.WriteLine("#define {0}_REQUIRED", def);
			}
			
			writer.WriteLine();
			writer.Write(source.ToString());

			writer.Close();
		}	
	}
}

