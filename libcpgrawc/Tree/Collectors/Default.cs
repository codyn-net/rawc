using System;
using System.Collections.Generic;
using System.Text;
using Cpg.RawC.Plugins.Attributes;
using Cpg.RawC.Plugins;

namespace Cpg.RawC.Tree.Collectors
{
	[Plugin(Description="Default Algorithm", Author="Jesse van den Kieboom")]
	public class Default : ICollector
	{
		public Result Collect(Node[] forest)
		{
			Result ret = new Result();

			return ret;
		}
	}
}