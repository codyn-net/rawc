using System;
using System.IO;

namespace Cdn.RawC
{
	public class Log
	{
		private static TextWriter s_base;
		
		static Log()
		{
			s_base = Console.Error;
		}
		
		public static TextWriter Base
		{
			get
			{
				return s_base;
			}
			set
			{
				s_base = value;
			}
		}
		
		public static void Close()
		{
			s_base = null;
		}
		
		public static void Write(string val)
		{
			if (s_base == null)
			{
				return;
			}
			
			s_base.Write(val);
		}
		
		public static void WriteLine(string format, params object[] args)
		{
			if (s_base == null)
			{
				return;
			}
			
			s_base.WriteLine(format, args);
		}
	}
}

