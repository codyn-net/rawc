using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cdn.RawC
{
	public class Options : CommandLine.Options
	{
		[CommandLine.Option("help", 'h', Description="Show this help message and exit")]
		private bool d_showHelp;
		[CommandLine.Option("version", 'v', Description="Show version")]
		private bool d_showVersion;
		[CommandLine.Option("output", 'o', ArgumentName="DIR", Description="Output directory")]
		private string d_output;
		[CommandLine.Option("collector", 'c', OptionalArgument=true, ArgumentName="NAME", Description="The collector algorithm to use")]
		private string d_collector;
		[CommandLine.Option("filter", 'f', OptionalArgument=true, ArgumentName="NAME", Description="The filter algorithm to use")]
		private string d_filter;
		[CommandLine.Option("basename", 'b', ArgumentName="NAME", Description="The basename of the generated files")]
		private string d_basename;
		[CommandLine.Option("compile", Description="Directly compile network (if possible) without generating code")]
		private bool d_compile;
		[CommandLine.Option("print-compile-source", Description="Print source code of test program")]
		private bool d_printCompileSource;
		[CommandLine.Option("validate", Description="Validate generated network")]
		private bool d_validate;
		[CommandLine.Option("validate-precision", Description="Allowed precision for validation")]
		private double d_validatePrecision = 10e-6;
		[CommandLine.Option("verbose", 'V', Description="Display verbose information")]
		private bool d_verbose;
		[CommandLine.Option("quiet", 'q', Description="Do not output anything on standard out")]
		private bool d_quiet;
		[CommandLine.Option("minimum-embedding-size", Description="Minimum number of instructions which are candidates for embeddings")]
		private int d_minimumEmbeddingSize = 3;
		[CommandLine.Option("list-options", Description="List available options (used for completion)")]
		private bool d_listOptions;
		[CommandLine.Option("no-embeddings", Description="Disable detection of embeddings")]
		private bool d_noEmbeddings;
		[CommandLine.Option("minimum-loop-size", Description="Minimum number of items in a loop")]
		public uint MinimumLoopSize = 3;
		[CommandLine.Option("delay-time-step", Description="Fixed time step that will be used for delays")]
		public double DelayTimeStep = -1;
		[CommandLine.Option("no-metadata", Description="Disable generation of metadata")]
		public bool NoMetadata = false;
		[CommandLine.Option("dependency-graph", ArgumentName="FILE", Description="Output dependency graph in DOT format to specified file")]
		public string DependencyGraph;
		[CommandLine.Option("bind", Description="Generate binding from first network to second network")]
		public bool Bind = false;
		[CommandLine.Option("no-sparsity", Description="Disable optimization of sparse expressions")]
		public bool NoSparsity = false;
		[CommandLine.Option("sparsity-benchmark", Description="Generate sparsity benchmarking code")]
		public bool SparsityBenchmark = false;

		private double[] d_validateRange;
		private Programmer.Formatters.IFormatter d_formatter;
		private List<string> d_files;
		private bool d_showFormatters;
		private static Options s_instance;

		public static bool EnableProfile;

		public static Options Initialize(string[] args)
		{
			s_instance = new Options();
			s_instance.Parse(ref args);

			EnableProfile = Environment.GetEnvironmentVariable("ENABLE_PROFILE") == "1";
			return s_instance;
		}

		public Options() : base("cdnrawc", "raw codyn network generator", "[FILE...]")
		{
			d_formatter = new Programmer.Formatters.C.C();
			AddOptionsForPlugin(d_formatter);

			d_validateRange = new double[] {0, 0.001, 5};
		}

		public override void Parse(ref string[] args)
		{
			try
			{
				base.Parse(ref args);

				d_files = new List<string>(args);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Could not parse options: {0}\n\nTrace:\n{1}", e.GetBaseException().Message, e.GetBaseException().StackTrace);
				Environment.Exit(1);
			}

			if (d_showHelp)
			{
				ShowHelp();
				Environment.Exit(1);
			}

			if (d_showVersion)
			{
				System.Version version = Assembly.GetExecutingAssembly().GetName().Version;

				Console.WriteLine("cdnrawc - Version {0}.{1}.{2}", version.Major, version.Minor, version.Revision);
				Environment.Exit(0);
			}

			if (d_listOptions)
			{
				Console.WriteLine(ShowOptions());
				Environment.Exit(0);
			}

			if (d_showFormatters)
			{
				return;
			}
		}

		public static Options Instance
		{
			get
			{
				return s_instance;
			}
		}

		public string[] Files
		{
			get
			{
				return d_files.ToArray();
			}
		}

		public bool Compile
		{
			get { return d_compile; }
		}

		public string Output
		{
			get
			{
				return d_output;
			}
		}

		public string Collector
		{
			get
			{
				return d_collector;
			}
		}

		public bool Quiet
		{
			get
			{
				return d_quiet;
			}
		}

		public bool Verbose
		{
			get
			{
				return d_verbose;
			}
		}

		public string Filter
		{
			get
			{
				return d_filter;
			}
		}

		public string Basename
		{
			get
			{
				return d_basename;
			}
		}

		public double ValidatePrecision
		{
			get
			{
				return d_validatePrecision;
			}
		}

		public bool PrintCompileSource
		{
			get
			{
				return d_printCompileSource;
			}
		}

		[CommandLine.Option("validate-range", Description="Range to validate network on (from:step:to)")]
		private string ValidateRangeOption
		{
			set
			{
				string[] parts = value.Split(':');

				if (parts.Length == 1)
				{
					d_validateRange[1] = Double.Parse(parts[0]);
				}
				else if (parts.Length == 2)
				{
					d_validateRange[1] = Double.Parse(parts[0]);
					d_validateRange[2] = Double.Parse(parts[1]);
				}
				else if (parts.Length == 3)
				{
					d_validateRange[0] = Double.Parse(parts[0]);
					d_validateRange[1] = Double.Parse(parts[1]);
					d_validateRange[2] = Double.Parse(parts[2]);
				}
				else
				{
					throw new Exception(String.Format("Invalid range: {0}", value));
				}
			}
		}

		public double[] ValidateRange
		{
			get
			{
				return d_validateRange;
			}
		}

		public bool Validate
		{
			get
			{
				return d_validate;
			}
		}

		private void AddOptionsForPlugin(object plugin)
		{
			Plugins.IOptions opts = plugin as Plugins.IOptions;

			if (opts != null)
			{
				Add(opts.Options);
			}
		}

		private void RemoveOptionsForPlugin(object plugin)
		{
			Plugins.IOptions opts = plugin as Plugins.IOptions;

			if (opts != null)
			{
				Remove(opts.Options);
			}
		}

		private void AddFormatterOptions(string format)
		{
			if (format == "")
			{
				d_showFormatters = true;
				return;
			}

			Plugins.Plugins plugins = Plugins.Plugins.Instance;
			Type type = plugins.Find(typeof(Programmer.Formatters.IFormatter), format);

			if (type != null)
			{
				if (d_formatter != null)
				{
					RemoveOptionsForPlugin(d_formatter);
				}

				d_formatter = plugins.Instantiate<Programmer.Formatters.IFormatter>(type);
				AddOptionsForPlugin(d_formatter);
			}
			else
			{
				throw new CommandLine.OptionException("The formatter `{0}' does not exist...", format);
			}
		}

		[CommandLine.Option("format", OptionalArgument=true, ArgumentName="NAME", Description="The format of the output")]
		private string Format
		{
			set
			{
				AddFormatterOptions(value);
			}
		}

		public Programmer.Formatters.IFormatter Formatter
		{
			get
			{
				return d_formatter;
			}
		}

		public bool ShowFormatters
		{
			get
			{
				return d_showFormatters;
			}
		}

		[CommandLine.Option("load", 'l', ArgumentName="FILENAME", Description="Load additional assembly with plugins")]
		private string Load
		{
			set
			{
				Plugins.Plugins.Instance.LoadAssembly(value);
			}
		}

		public int MinimumEmbeddingSize
		{
			get
			{
				return d_minimumEmbeddingSize >= 2 ? d_minimumEmbeddingSize : 2;
			}
		}

		public bool ListOptions
		{
			get
			{
				return d_listOptions;
			}
		}

		public bool NoEmbeddings
		{
			get
			{
				return d_noEmbeddings;
			}
		}
	}
}

