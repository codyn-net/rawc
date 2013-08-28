using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.CLike
{
	public class Options : CommandLine.OptionGroup
	{
		[CommandLine.Option("symbolic-names", Description="Use symbolic names for state indices in the source code")]
		public bool SymbolicNames;

		public string CPrefix;
		public string CPrefixDown;
		public string CPrefixUp;

		public Options(string name) : base(name)
		{
		}
	}
}

