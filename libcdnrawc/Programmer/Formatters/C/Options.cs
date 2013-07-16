using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class Options : CLike.Options
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
		[CommandLine.Option("no-makefile", Description="Don't generate makefile to compile the network")]
		public bool NoMakefile;
		[CommandLine.Option("static", Description="Compile a static library")]
		public bool CompileStatic;
		[CommandLine.Option("shared", Description="Compile a shared library")]
		public bool CompileShared;
		[CommandLine.Option("standalone", ArgumentName="TYPE", OptionalArgument=true, DefaultArgument="minimal", Description="Create a standalone network (minimal, full)")]
		public string Standalone;
		[CommandLine.Option("no-blas", Description="Disable use of blas")]
		public bool NoBlas;
		[CommandLine.Option("no-lapack", Description="Disable use of lapack")]
		public bool NoLapack;
		[CommandLine.Option("no-run", Description="Disable generation of run sources")]
		public bool NoRun;

		public string EventStateType;

		public Options(string name) : base(name)
		{
		}
	}
}

