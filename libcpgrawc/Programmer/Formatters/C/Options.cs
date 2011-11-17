using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer.Formatters.C
{
	public class Options : CommandLine.OptionGroup
	{
		[CommandLine.Option("value-type", Description="Value type to use (double, float, etc)")]
		public string ValueType = "double";
		[CommandLine.Option("custom-header", ArgumentName="FILENAME", Description="Custom header to include")]
		public List<string> CustomHeaders;
		[CommandLine.Option("separate-math-header", Description="Whether or not to use a separate header for math defines")]
		public bool SeparateMathHeader;
		[CommandLine.Option("generate-cpp-wrapper", Description="Specify to generate a simple C++ wrapper")]
		public bool GenerateCppWrapper;
		[CommandLine.Option("cflags", Description="Specify compiler flags for compilation (used with --compile and --validate)")]
		public string CFlags;
		[CommandLine.Option("libs", Description="Specify linker flags for compilation (used with --compile and --validate)")]
		public string Libs;
		[CommandLine.Option("symbolic-names", Description="Use symbolic names for state indices in the source code")]
		public bool SymbolicNames;
		
		public Options(string name) : base(name)
		{
		}		
	}
}

