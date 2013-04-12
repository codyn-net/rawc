using System;
using System.IO;

namespace Cdn.RawC.Programmer.Formatters
{
	public interface IFormatter
	{
		string[] Write(Program program);
		
		string[] Compile(bool verbose);
		string CompileForValidation(bool verbose);
	}
}

