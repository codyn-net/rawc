using System;

namespace Cdn.RawC
{
	public class Exception : System.Exception
	{
		public Exception(string format, params object[] args) : base(String.Format(format, args))
		{
		}
	}
}

