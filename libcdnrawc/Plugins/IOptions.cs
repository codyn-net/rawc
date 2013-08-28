using System;

namespace Cdn.RawC.Plugins
{
	public interface IOptions
	{
		CommandLine.OptionGroup Options
		{
			get;
		}
	}
}

