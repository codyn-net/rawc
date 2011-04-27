using System;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer.Formatters.C
{
	public class Options : CommandLine.OptionGroup
	{
		[CommandLine.Option("value-type", Description="Value type to use (double, float, etc)")]
		public string ValueType = "double";
		
		[CommandLine.Option("custom-headers", ArgumentName="FILENAME", Description="Custom header to include")]
		public List<string> CustomHeaders;
		
		[CommandLine.Option("no-separate-math-header", Description="Whether or not to use a separate header for math defines")]
		public bool NoSeparateMathHeader;
		
		[CommandLine.Option("generate-cpp-wrapper", Description="Specify to generate a simple C++ wrapper")]
		public bool GenerateCppWrapper;
		
		[CommandLine.Option("cflags", Description="Specify compiler flags")]
		public string CFlags;
		
		[CommandLine.Option("libs", Description="Specify linker flags")]
		public string Libs;

		public Options(string name) : base(name)
		{
			CustomHeaders = new List<string>();
		}		
	}
}

