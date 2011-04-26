using System;

namespace Cpg.RawC.Plugins
{
	public interface IOptions
	{
		CommandLine.OptionGroup Options
		{
			get;
		}
	}
}

