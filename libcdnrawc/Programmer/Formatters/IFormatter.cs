using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters
{
	public interface IFormatter
	{
		string[] Write(Program program);
		
		string[] Compile(bool verbose);

		string CompileForValidation(string[] sources, bool verbose);
		IEnumerator<double[]> RunForValidation(string[] sources, double t, double dt);
	}
}

