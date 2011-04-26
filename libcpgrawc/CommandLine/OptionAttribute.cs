using System;

namespace Cpg.RawC.CommandLine
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class OptionAttribute : Attribute
	{
		public string LongName;
		public string ShortName;
		public string Description;
		public bool OptionalArgument;
		public string DefaultArgument;
		public string ArgumentName;

		public OptionAttribute(string longname) : this(longname, null)
		{
		}
		
		public OptionAttribute(char shortname) : this(null, shortname)
		{
		}
		
		public OptionAttribute(string longname, char shortname) : this(longname, shortname.ToString())
		{
		}
		
		private OptionAttribute(string longname, string shortname)
		{
			LongName = longname;
			ShortName = shortname;

			OptionalArgument = false;
			DefaultArgument = "";
			ArgumentName = "VALUE";
		}
	}
}

