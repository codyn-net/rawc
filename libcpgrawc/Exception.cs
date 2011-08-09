using System;

namespace Cpg.RawC
{
	public class Exception : System.Exception
	{
		public Exception(string format, params string[] args) : base(String.Format(format, args))
		{
		}
	}
}

