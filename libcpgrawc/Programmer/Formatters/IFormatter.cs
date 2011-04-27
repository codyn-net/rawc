using System;
using System.IO;

namespace Cpg.RawC.Programmer.Formatters
{
	public interface IFormatter
	{
		void Write(Program program);
		
		void Compile(string filename, bool verbose);
	}
}

