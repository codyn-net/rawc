using System;
using System.Collections.Generic;

namespace Cpg.RawC
{
	public class OptionParser
	{
		private bool d_showHelp;
		private List<string> d_files;
		private string d_output;
		
		private static OptionParser s_instance;

		public static OptionParser Initialize(string[] args)
		{
			s_instance = new OptionParser(args);
			return s_instance;
		}
		
		private OptionParser(string[] args)
		{
			Mono.Options.OptionSet opts = new Mono.Options.OptionSet() {
				{"h|help", "Show this help message and exit", v => d_showHelp = v != null},
				{"o|output", "Output directory", v => d_output = v}
			};
			
			try
			{
				d_files = opts.Parse(args);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Could not parse options: {0}", e.Message);
				Environment.Exit(1);
			}
			
			if (d_showHelp)
			{
				ShowHelp(opts);
			}
			
			if (d_files.Count == 0)
			{
				Console.Error.WriteLine("Please specify at least one network file");
				Environment.Exit(1);
			}
		}
		
		public static OptionParser Instance
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
		
		public string Output
		{
			get
			{
				return d_output;
			}
		}
		
		private void ShowHelp(Mono.Options.OptionSet opts)
		{
			Console.WriteLine("Usage: cpgrawc [options] file");
			Console.WriteLine();

			opts.WriteOptionDescriptions(Console.Out);
			
			Environment.Exit(1);
		}
	}
}

