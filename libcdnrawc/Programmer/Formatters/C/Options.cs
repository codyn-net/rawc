using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class Options : CommandLine.OptionGroup
	{
		[CommandLine.Option("value-type", Description="Value type to use (double, float, etc)")]
		public string ValueType = "double";
		[CommandLine.Option("custom-header", ArgumentName="FILENAME", Description="Custom header to include")]
		public List<string> CustomHeaders;
		[CommandLine.Option("separate-math-header", Description="Whether or not to use a separate header for math defines")]
		public bool SeparateMathHeader;
		[CommandLine.Option("cflags", Description="Specify compiler flags for compilation")]
		public string CFlags;
		[CommandLine.Option("libs", Description="Specify linker flags for compilation")]
		public string Libs;
		[CommandLine.Option("symbolic-names", Description="Use symbolic names for state indices in the source code")]
		public bool SymbolicNames;
		[CommandLine.Option("no-makefile", Description="Don't generate makefile to compile the network")]
		public bool NoMakefile;
		[CommandLine.Option("static", Description="Compile a static library")]
		public bool CompileStatic;
		[CommandLine.Option("shared", Description="Compile a shared library")]
		public bool CompileShared;
		[CommandLine.Option("standalone", ArgumentName="TYPE", OptionalArgument=true, DefaultArgument="minimal", Description="Create a standalone network (minimal, full)")]
		public string Standalone;

		public string CPrefixDown;
		
		public Options(string name) : base(name)
		{
		}
	}
}

