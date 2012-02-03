using System;

namespace Cdn.RawC.CommandLine
{
	public class OptionException : System.Exception
	{
		public OptionException(string message, params object[] args) : base(String.Format(message, args))
		{
		}
	}
}

