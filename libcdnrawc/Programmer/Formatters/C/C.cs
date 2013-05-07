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
	public class C : CLike.CLike, IFormatter, Plugins.IOptions
	{
		private Options d_options;
		private Programmer.Program d_program;

		private string d_sourceFilename;
		private string d_headerFilename;

		private string d_runSourceFilename;
		private string d_runHeaderFilename;

		public C()
		{
			d_options = new Options("C Formatter");
		}

		private bool IsStandalone
		{
			get { return d_options.Standalone != null || d_options.ValueType != "double"; }
		}

		public string[] Write(Program program)
		{
			Profile.Do("c init", () => {
				Initialize(program, d_options);
			});

			d_program = program;

			List<string > written = new List<string>();

			Profile.Do("c header", () => {
				WriteHeader();
			});

			Profile.Do("c source", () => {
				WriteSource();
			});

			Profile.Do("c run", () => {
				Profile.Do("header", () => {
					WriteRunHeader();
				});

				Profile.Do("source", () => {
					WriteRunSource();
				});
			});

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
				var t = Profile.Begin("c makefile");
				var filename = WriteMakefile();
				t.End();
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

			var cid = Knowledge.Instance.Network.Integrator.ClassId;

			var srep = new Dictionary<string, string> {
				{"name", CPrefixDown},
				{"NAME", CPrefixUp},
				{"Name", CPrefix},
				{"SOURCES", sources},
				{"HEADERS", headers},
				{"integrator_include", cid.ToLower()},
				{"integrator_type", cid.Replace("-", "_").ToLower()},
				{"INTEGRATOR", cid.ToUpper().Replace("-", "_")},
				{"basename", d_program.Options.Basename},
				{"BASENAME", d_program.Options.Basename.ToUpper()},
				{"valuetype", ValueType},
				{"cflags", d_options.CFlags},
				{"libs", d_options.Libs},
				{"enable_blas", d_options.NoBlas ? "0" : "1"},
				{"enable_lapack", d_options.NoLapack ? "0" : "1"},
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

				statemap.AppendFormat("\t{{{0}_{1}, \"{2}\"}}", CPrefixUp, d_enumMap[i].CName, name);
			}

			prog = prog.Replace("${statemap}", statemap.ToString());

			writer.WriteLine(prog);
			return writer.ToString();
		}

		public IEnumerator<double[]> RunForValidation(string[] sources, double t, double dt)
		{
			// No need since we support compilation for validation
			return null;
		}

		public string CompileForValidation(string[] sources, bool verbose)
		{
			return Compile(verbose, true)[0];
		}

		public string[] Compile(bool verbose)
		{
			return Compile(verbose, false);
		}

		[System.Runtime.InteropServices.DllImport("libc")]
		private static extern int uname(IntPtr buf);

		private bool IsOnOSX()
		{
			IntPtr buf = IntPtr.Zero;

			try
			{
				buf = System.Runtime.InteropServices.Marshal.AllocHGlobal(8192);

				if (uname(buf) == 0)
				{
					string os = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(buf);
					return (os == "Darwin");
				}
			}
			catch {}
			finally
			{
				if (buf != IntPtr.Zero)
				{
					System.Runtime.InteropServices.Marshal.FreeHGlobal(buf);
				}
			}

			return false;
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
				string dyext;

				args.Append(" shared");

				switch (Environment.OSVersion.Platform)
				{
				case PlatformID.MacOSX:
					dyext = ".dylib";
					break;
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					dyext = ".dll";
					break;
				default:
					if (IsOnOSX())
					{
						dyext = ".dylib";
					}
					else
					{
						dyext = ".so";
					}
					break;
				}

				ret.Add(Path.Combine(ddir, "lib" + CPrefixDown + dyext));
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

		private string ValueType
		{
			get
			{
				return d_options.ValueType;
			}
		}

		protected override string EnumAlias(string name)
		{
			return CPrefixUp + "_" + name;
		}

		private void WriteAccessorEnum(TextWriter writer)
		{
			if (d_program.StateTable.Count == 0)
			{
				return;
			}

			writer.WriteLine("typedef enum");
			writer.WriteLine("{");

			List<string > names = new List<string>();
			List<string > values = new List<string>();
			List<string > comments = new List<string>();

			int maxname = 0;
			int maxval = 0;

			foreach (var e in d_enumMap)
			{
				names.Add(String.Format("{0}_{1}", CPrefixUp, e.CName));
				values.Add(e.Value);
				comments.Add(e.Comment);

				maxname = System.Math.Max(maxname, e.CName.Length);
				maxval = System.Math.Max(maxval, e.Value.Length);
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

			// End protect for including this from C++
			writer.WriteLine("CDN_RAWC_END_DECLS");
			writer.WriteLine();

			// End include guard
			writer.WriteLine("#endif /* __CDN_RAWC_{0}_H__ */", CPrefixUp);

			writer.Close();
		}

		public bool IsDouble
		{
			get { return d_options.ValueType == "double"; }
		}

		private string GenerateArgsList(Programmer.Function function)
		{
			List<string> ret = new List<string>(function.NumArguments + 2);

			ret.Add(String.Format("ValueType *{0}", d_program.StateTable.Name));

			if (!function.Expression.Dimension.IsOne)
			{
				ret.Add("ValueType *ret");
			}

			if (function.IsCustom)
			{
				int i = 0;

				foreach (var arg in function.CustomArguments)
				{
					if (arg.Dimension.IsOne)
					{
						ret.Add(String.Format("ValueType x{0}", i));
					}
					else
					{
						ret.Add(String.Format("ValueType *x{0}", i));
					}

					++i;
				}
			}
			else
			{
				for (int i = 0; i < function.OrderedArguments.Count; ++i)
				{
					var arg = function.OrderedArguments[i];
					var child = function.Expression.FromPath(arg.Path);

					if (child.Dimension.IsOne)
					{
						ret.Add(String.Format("ValueType x{0}", i));
					}
					else
					{
						ret.Add(String.Format("ValueType *x{0}", i));
					}
				}
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
				if (!function.CanBeOverridden)
				{
					continue;
				}

				string impl = Context.ToAsciiOnly(function.Name);
				string def = impl.ToUpper();

				WriteDefine(writer, def, "", impl, String.Format("#define {0}_IS_DEFINED", def));
			}
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
			var impl = Context.ToAsciiOnly(function.Name);

			if (function.CanBeOverridden)
			{
				writer.WriteLine("#ifdef {0}_IS_DEFINED", impl.ToUpper());
			}

			writer.Write("static ValueType");

			if (!isone)
			{
				writer.Write(" *");
			}

			writer.WriteLine();

			writer.WriteLine("{0} ({1})",
			                 impl,
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

			if (function.CanBeOverridden)
			{
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", impl.ToUpper());
			}

			writer.WriteLine();
		}

		private void WriteFunctionDecl(TextWriter writer, Programmer.Function function)
		{
			var impl = Context.ToAsciiOnly(function.Name);

			if (function.CanBeOverridden)
			{
				writer.WriteLine("#ifdef {0}_IS_DEFINED", impl.ToUpper());
			}

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
			             impl,
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

			if (function.CanBeOverridden)
			{
				writer.WriteLine("#endif /* {0}_IS_DEFINED */", impl.ToUpper());
				writer.WriteLine();
			}
		}

		private string Eye(int i)
		{
			StringBuilder b = new StringBuilder();

			b.Append("{");

			for (var c = 0; c < i; ++c)
			{
				for (var r = 0; r < i; ++r)
				{
					if (c != 0 || r != 0)
					{
						b.Append(", ");
					}

					if (c == r)
					{
						b.Append("1");
					}
					else
					{
						b.Append("0");
					}
				}
			}

			b.Append("}");
			return b.ToString();
		}

		private void WriteMathDimFunctions(TextWriter writer)
		{
			foreach (var def in Context.UsedMathFunctions)
			{
				Context.Workspace ws;

				if (!Context.Workspaces.TryGetValue(def, out ws))
				{
					continue;
				}

				var nm = Enum.GetName(typeof(Cdn.MathFunctionType), ws.Type);

				// Write function for this linsolve order
				var os = String.Join("_", Array.ConvertAll(ws.Order, a => a.ToString()));
				var n = String.Format("cdn_math_{0}_builtin_v_{1}", nm.ToLower(), os);

				writer.WriteLine("#ifndef {0}", def);
				writer.WriteLine("#define {0} {1}", def, n);
				writer.WriteLine("static ValueType *");
				writer.WriteLine("{0} (ValueType *ret,", n);

				switch (ws.Type)
				{
				case Cdn.MathFunctionType.Linsolve:
					writer.WriteLine("     ValueType *A,");
					writer.WriteLine("     ValueType *b,");
					writer.WriteLine("     uint32_t   RA,");
					writer.WriteLine("     uint32_t   CB)");
					writer.WriteLine("{");
					writer.WriteLine("\tint64_t ipiv[{0}];", ws.WorkSize[0]);
					writer.WriteLine();
					writer.WriteLine("\treturn CDN_MATH_LINSOLVE_V(ret, A, b, RA, CB, ipiv);");
					break;
				case Cdn.MathFunctionType.Inverse:
					writer.WriteLine("     ValueType *A)");
					writer.WriteLine("{");
					writer.WriteLine("\tint64_t ipiv[{0}];", ws.Dimension.Rows);
					writer.WriteLine("\tValueType work[{0}];", ws.WorkSize[0]);
					writer.WriteLine();
					writer.WriteLine("\treturn CDN_MATH_INVERSE_V(ret, A, {0}, ipiv, work, {1});", ws.Dimension.Rows, ws.WorkSize[0]);
					break;
				case Cdn.MathFunctionType.PseudoInverse:
				{
					int maxdim = System.Math.Max(ws.Dimension.Rows, ws.Dimension.Columns);
					int mindim = System.Math.Min(ws.Dimension.Rows, ws.Dimension.Columns);

					writer.WriteLine("     ValueType *A)");
					writer.WriteLine("{");
					writer.WriteLine("\tValueType Acp[{0}];", ws.Dimension.Size());
					writer.WriteLine("\tValueType b[{0}] = {1};", maxdim * maxdim, Eye(maxdim));
					writer.WriteLine("\tValueType s[{0}];", mindim);
					writer.WriteLine("\tValueType work[{0}];", ws.WorkSize[0]);
					writer.WriteLine("\tint64_t   iwork[{0}];", ws.WorkSize[1]);
					writer.WriteLine();
					writer.WriteLine("\tmemcpy (Acp, A, sizeof(ValueType) * {0});", ws.Dimension.Size());
					writer.WriteLine("\treturn CDN_MATH_PSEUDOINVERSE_V(ret, Acp, {0}, {1}, b, {2}, s, work, {3}, iwork);",
                                     ws.Dimension.Rows,
                                     ws.Dimension.Columns,
					                 maxdim,
					                 ws.WorkSize[0]);
				}
					break;
				case Cdn.MathFunctionType.Qr:
				{
					int mindim = System.Math.Min(ws.Dimension.Rows, ws.Dimension.Columns);

					writer.WriteLine("     ValueType *A)");
					writer.WriteLine("{");
					writer.WriteLine("\tValueType Acp[{0}] = {{0,}};", ws.Dimension.Size() + ws.Dimension.Rows * ws.Dimension.Rows);
					writer.WriteLine("\tValueType tau[{0}];", mindim);
					writer.WriteLine("\tValueType work[{0}];", ws.WorkSize[0]);
					writer.WriteLine();
					writer.WriteLine("\tmemcpy (Acp, A, sizeof(ValueType) * {0});", ws.Dimension.Size());
					writer.WriteLine("\treturn CDN_MATH_QR_V(ret, Acp, {0}, {1}, tau, work, {2});",
                                     ws.Dimension.Rows,
                                     ws.Dimension.Columns,
					                 ws.WorkSize[0]);
				}
					break;
				case Cdn.MathFunctionType.Slinsolve:
					writer.WriteLine("     ValueType *A,");
					writer.WriteLine("     ValueType *b,");
					writer.WriteLine("     uint32_t   CB,");
					writer.WriteLine("     ValueType *L)");
					writer.WriteLine("{");
					writer.WriteLine("\tValueType Acp[{0}];", ws.Dimension.Size());
					writer.WriteLine();
					writer.WriteLine("\tmemcpy (Acp, A, sizeof(ValueType) * {0});", ws.Dimension.Size());
					writer.WriteLine("\treturn CDN_MATH_SLINSOLVE_V(ret, Acp, {0}, b, CB, L);", ws.Dimension.Rows);

					break;
				}

				writer.WriteLine("}");
				writer.WriteLine("#endif");
				writer.WriteLine();
			}
		}

		private void WriteFunctions(TextWriter writer)
		{
			WriteMathDimFunctions(writer);

			// Write declarations
			foreach (var function in d_program.Functions)
			{
				WriteFunctionDecl(writer, function);
			}

			writer.WriteLine();

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

			writer.WriteLine();

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
			writer.WriteLine(Context.Reindent(CLike.ComputationNodeTranslator<ComputationNodeTranslator>.Translate(node, context), indent));
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

			writer.Write("{0}_{1} (void *data", CPrefixDown, api.Name);

			for (int i = 0; i < api.Arguments.Length; i += 2)
			{
				writer.Write(", ");

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
				string extrav = null;

				if (api.NeedsEventStates)
				{
					extrav = String.Format("\t{0} *{1};", EventStateType, d_program.EventStatesTable.Name);
				}

				WriteNetworkVariable(writer, extrav);

				if (api.NeedsEventStates)
				{
					writer.WriteLine("\t{0} = network->event_states;\n", d_program.EventStatesTable.Name);
				}

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
			                 Context.TypeToCType(table.TypeName),
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
				string val = table.NeedsInitialization ? translator.Translate(table[i].Key) : "0";
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
			public object Object;
			public uint Parent;
			public bool IsNode;

			public uint Index;
			public uint Next;
		}

		private class NodeMeta
		{
			public Cdn.Object Object;
			public string Name;
			public uint Parent;
			public uint FirstChild;
			public uint FirstTemplate;
		}

		private class StateMeta
		{
			public Cdn.Variable Variable;
			public string Name;
			public uint Parent;
			public uint Index;
		}

		private class TemplateMeta
		{
			public Cdn.Object Object;
			public string Name;
			public uint Parent;
			public uint Next;
		}

		private class Meta
		{
			public List<NodeMeta> Nodes;
			public List<StateMeta> States;
			public List<ChildMeta> Children;
			public List<TemplateMeta> Templates;
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

		private void ExtractMetaTemplates(Meta meta, Cdn.Object obj, NodeMeta node, uint parent)
		{
			TemplateMeta prev = null;

			foreach (var tmpl in obj.AppliedTemplates)
			{
				var tm = new TemplateMeta {
					Object = tmpl,
					Name = tmpl.FullId,
					Parent = parent,
					Next = 0,
				};

				if (node.FirstTemplate == 0)
				{
					node.FirstTemplate = (uint)meta.Templates.Count;
				}

				if (prev != null)
				{
					prev.Next = (uint)meta.Templates.Count;
				}

				prev = tm;
				meta.Templates.Add(tm);
			}
		}

		private uint ExtractMeta(Meta meta, Cdn.Object obj, uint parent)
		{
			NodeMeta nm = new NodeMeta {
				Object = obj,
				Name = obj.Id,
				Parent = parent,
				FirstChild = 0,
				FirstTemplate = 0,
			};

			parent = (uint)meta.Nodes.Count;

			meta.Nodes.Add(nm);

			ExtractMetaTemplates(meta, obj, nm, parent);

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

				if (d_program.StateTable.TryGetValue(v, out item))
				{
					ChildMeta cm = new ChildMeta {
						Object = v,
						Parent = parent,
						IsNode = false,
						Index = (uint)meta.States.Count,
						Next = 0
					};

					StateMeta sm = new StateMeta {
						Index = (uint)item.DataIndex,
						Name = v.Name,
						Variable = v,
						Parent = parent,
					};

					meta.States.Add(sm);
					prev = AddChild(meta, nm, cm, prev);
				}
			}

			Cdn.Node node = obj as Cdn.Node;

			if (node != null)
			{
				foreach (var child in node.Children)
				{
					uint cid = ExtractMeta(meta, child, parent);

					ChildMeta cm = new ChildMeta {
						Object = child,
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
				Templates = new List<TemplateMeta>()
			};

			// Empty root nodes
			meta.Nodes.Add(new NodeMeta {
				Name = null,
				Parent = 0,
				FirstChild = 0,
				FirstTemplate = 0,
			});

			meta.Children.Add(new ChildMeta {
				Parent = 0,
				IsNode = false,
				Index = 0,
				Next = 0
			});

			meta.Templates.Add(new TemplateMeta {
				Name = null,
				Parent = 0,
				Next = 0
			});

			if (Cdn.RawC.Options.Instance.NoMetadata)
			{
				return meta;
			}

			ExtractMeta(meta, Knowledge.Instance.Network, 0);

			return meta;
		}

		private void WriteNetworkMeta(TextWriter writer)
		{
			var meta = ExtractMeta();
			var vb = Cdn.RawC.Options.Instance.Verbose;

			writer.WriteLine("\tstatic CdnRawcStateMeta meta_states[] = {");

			var pl = (meta.States.Count - 1).ToString().Length;

			for (int i = 0; i < meta.States.Count; ++i)
			{
				var state = meta.States[i];

				if (vb)
				{
					writer.WriteLine("\n\t\t/* [{0}] {1} */",
					                 i.ToString().PadLeft(pl),
					                 state.Variable != null ? state.Variable.FullNameForDisplay : "(empty)");
				}

				writer.WriteLine("\t\t{{ {0}, {1}, {2} }},",
				                 state.Name == null ? "NULL" : "\"" + state.Name.Replace("\"", "\\\"") + "\"",
				                 state.Parent,
				                 state.Index);
			}

			writer.WriteLine("\t};");
			writer.WriteLine();
			writer.WriteLine("\tstatic CdnRawcNodeMeta meta_nodes[] = {");

			pl = (meta.Nodes.Count - 1).ToString().Length;

			for (int i = 0; i < meta.Nodes.Count; ++i)
			{
				var node = meta.Nodes[i];

				if (vb)
				{
					writer.WriteLine("\n\t\t/* [{0}] {1} */",
					                 i.ToString().PadLeft(pl),
					                 node.Object != null ? node.Object.FullIdForDisplay : "(empty)");
				}

				writer.WriteLine("\t\t{{ {0}, {1}, {2}, {3} }},",
				                 node.Name == null ? "NULL" : "\"" + node.Name.Replace("\"", "\\\"") + "\"",
				                 node.Parent,
				                 node.FirstChild,
				                 node.FirstTemplate);
			}

			writer.WriteLine("\t};");
			writer.WriteLine();
			writer.WriteLine("\tstatic CdnRawcChildMeta meta_children[] = {");

			pl = (meta.Children.Count - 1).ToString().Length;

			for (int i = 0; i < meta.Children.Count; ++i)
			{
				var child = meta.Children[i];

				if (vb)
				{
					string s;

					if (child.Object != null)
					{
						if (child.IsNode)
						{
							s = ((Cdn.Object)child.Object).FullIdForDisplay;
						}
						else
						{
							s = ((Cdn.Variable)child.Object).FullNameForDisplay;
						}
					}
					else
					{
						s = "(empty)";
					}

					writer.WriteLine("\n\t\t/* [{0}] {1} */",
					                 i.ToString().PadLeft(pl),
					                 s);
				}

				writer.WriteLine("\t\t{{ {0}, {1}, {2}, {3} }},",
				                 child.Parent,
				                 child.IsNode ? 1 : 0,
				                 child.Index,
				                 child.Next);
			}

			writer.WriteLine("\t};");
			writer.WriteLine();
			writer.WriteLine("\tstatic CdnRawcTemplateMeta meta_templates[] = {");

			pl = (meta.Templates.Count - 1).ToString().Length;

			for (int i = 0; i < meta.Templates.Count; ++i)
			{
				var tmpl = meta.Templates[i];

				if (vb)
				{
					writer.WriteLine("\n\t\t/* [{0}] {1} */",
					                 i.ToString().PadLeft(pl),
					                 tmpl.Object != null ? tmpl.Object.FullIdForDisplay : "(empty)");
				}

				writer.WriteLine("\t\t{{ {0}, {1}, {2} }},",
				                 tmpl.Name == null ? "NULL" : "\"" + tmpl.Name.Replace("\"", "\\\"") + "\"",
				                 tmpl.Parent,
				                 tmpl.Next);
			}

			writer.WriteLine("\t};");
			writer.WriteLine();
		}

		private void WriteNetworkDimensions(TextWriter writer)
		{
			writer.WriteLine("\tstatic CdnRawcDimension dimensions[] = {");

			foreach (var item in d_program.StateTable)
			{
				var dim = item.Dimension;

				if (!dim.IsOne)
				{
					writer.WriteLine("\t\t{{ {0}, {1} }},", dim.Rows, dim.Columns);
				}
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
			WriteNetworkDimensions(writer);

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
				"get_dimension",
			};

			foreach (string name in funcs)
			{
				writer.WriteLine("\t\t.{0} = {1}_{0},", name, pref);
			}

			writer.WriteLine();

			var range = d_program.StateRange(Knowledge.Instance.Integrated, new int[] {0, 0});

			writer.WriteLine("\t\t.states = {{.start = {0}, .end = {1}, .stride = 1}},",
			                 range[0],
			                 range[1]);

			range = d_program.StateRange(Knowledge.Instance.DerivativeStates, new int[] {0, 0});

			writer.WriteLine("\t\t.derivatives = {{.start = {0}, .end = {1}, .stride = 1}},",
			                 range[0],
			                 range[1]);

			range = d_program.StateRange(Knowledge.Instance.EventNodeStates, new int[] {0, 0});

			writer.WriteLine("\t\t.event_values = {{.start = {0}, .end = {1}, .stride = 3}},",
			                 range[0],
			                 range[0] + (range[1] - range[0]) * 3);

			writer.WriteLine();
			writer.WriteLine("\t\t.dimensions = dimensions,");
			writer.WriteLine();
			writer.WriteLine("\t\t.size = CDN_RAWC_NETWORK_{0}_SIZE,", CPrefixUp);
			writer.WriteLine("\t\t.data_size = sizeof (ValueType) * {0},", d_program.StateTable.Size);
			writer.WriteLine("\t\t.data_count = {0},", d_program.StateTable.Size);
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
			writer.WriteLine();
			writer.WriteLine("\t\t\t.templates = meta_templates,");
			writer.WriteLine("\t\t\t.templates_size = sizeof (meta_templates) / sizeof (CdnRawcTemplateMeta),");
			writer.WriteLine("\t\t},");

			writer.WriteLine("\t};");

			writer.WriteLine();
			writer.WriteLine("\treturn &network;");

			writer.WriteLine("}");
			writer.WriteLine();
		}

		protected override Cdn.RawC.Programmer.Formatters.CLike.Context CreateContext()
		{
			return new Context(d_program, d_options);
		}

		protected override void WriteEventsUpdateDistance(TextWriter writer)
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

			base.WriteEventsUpdateDistance(writer);

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
				WriteNetworkVariable(writer, String.Format("\t{0} const *{1};", EventStateType, d_program.EventStatesTable.Name));

				writer.WriteLine("\t{0} = network->event_states;", d_program.EventStatesTable.Name);

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
						Knowledge.EventState evstate;

						if (Knowledge.Instance.TryGetEventState(parent, ph, out evstate))
						{
							conditions.Add(String.Format("{0}[{1}] == {2}", d_program.EventStatesTable.Name, idx, evstate.Index));
						}
					}

					if (conditions.Count > 0)
					{
						writer.WriteLine("\t\treturn ({0});", String.Join(" || ", conditions));
					}
					else
					{
						writer.WriteLine("\t\treturn 0;");
					}
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
					Knowledge.EventState st;

					if (Knowledge.Instance.TryGetEventState(parent, state, out st))
					{
						writer.WriteLine("\t\t\t\tnetwork->event_states[{0}] = {1};",
					                     idx,
					                     st.Index);
					}
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

			if (range == null)
			{
				writer.WriteLine("\treturn NULL;");
			}
			else
			{
				WriteNetworkVariable(writer);
				writer.WriteLine("\treturn ss + {0};", range[0]);
			}

			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static ValueType *");
			writer.WriteLine("{0}_get_derivatives (void *data)",
			                 CPrefixDown,
			                 d_program.StateTable.Name);

			range = d_program.StateRange(Knowledge.Instance.DerivativeStates);

			writer.WriteLine("{");

			if (range == null)
			{
				writer.WriteLine("\treturn NULL;");
			}
			else
			{
				WriteNetworkVariable(writer);
				writer.WriteLine("\treturn ss + {0};", range[0]);
			}

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

			if (range == null)
			{
				writer.WriteLine("\treturn NULL;");
			}
			else
			{
				WriteNetworkVariable(writer);
				writer.WriteLine("\treturn (CdnRawcEventValue *)({0} + {1} + i * 3);",
				                 d_program.StateTable.Name,
				                 range[0]);
			}

			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("static CdnRawcDimension const *");
			writer.WriteLine("{0}_get_dimension (CdnRawcDimension const *dimensions, uint32_t i)", CPrefixDown);
			writer.WriteLine("{");

			writer.WriteLine("\tswitch (i)");
			writer.WriteLine("\t{");

			int i = 0;

			foreach (var item in d_program.StateTable)
			{
				var dim = item.Dimension;

				if (!dim.IsOne)
				{
					writer.WriteLine("\tcase {0}:", item.AliasOrIndex);
					writer.WriteLine("\t\treturn dimensions + {0};", i);
					++i;
				}
			}

			writer.WriteLine("\tdefault:");
			writer.WriteLine("\t{");
			writer.WriteLine("\t\tstatic CdnRawcDimension dimone = {1, 1};");
			writer.WriteLine("\t\treturn &dimone;");
			writer.WriteLine("\t}");
			writer.WriteLine("\t}");

			writer.WriteLine("}");
			writer.WriteLine();
		}

		private string EventStateType
		{
			get { return Context.TypeToCType(d_program.EventStatesTable.TypeName); }
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

			foreach (var inc in new string[] {"stdint", "stddef", "float", "string"})
			{
				writer.WriteLine("#include <{0}.h>", inc);
			}

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

			WriteFunctionDefines(source);

			WriteDataTables(source);

			WriteFunctions(source);
			WriteAPISource(source);

			WriteEventsSource(source);
			WriteDataAccessors(source);

			WriteNetwork(source);

			var seen = new HashSet<MathFunctionType>();

			foreach (var def in Context.UsedMathFunctions)
			{
				Context.Workspace ws;

				if (Context.Workspaces.TryGetValue(def, out ws))
				{
					if (seen.Add(ws.Type))
					{
						var nm = Enum.GetName(typeof(Cdn.MathFunctionType), ws.Type).ToUpper();
						writer.WriteLine("#define CDN_MATH_{0}_V_REQUIRED", nm);
					}
				}
				else
				{
					writer.WriteLine("#define {0}_REQUIRED", def);
				}
			}

			writer.WriteLine();

			writer.WriteLine("#include <cdn-rawc/cdn-rawc-math.h>");
			writer.WriteLine();

			WriteMathDimFunctions(writer);

			writer.WriteLine();
			writer.Write(source.ToString());

			writer.Close();
		}
	}
}

