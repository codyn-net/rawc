using System;
using System.IO;

namespace Cdn.RawC.Programmer.Formatters
{
	public interface IFormatter
	{
		string[] Write(Program program);
		
		void Compile(string filename, bool verbose);

		string CompileSource();
		string MexSource();
	}
}

