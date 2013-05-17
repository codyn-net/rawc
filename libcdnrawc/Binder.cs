using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class Binder
	{
		public Binder()
		{
		}

		public struct Binding
		{
			public Cdn.Variable Input;
			public Cdn.Variable Output;
		}

		public void Generate(string input, string output)
		{
			Cdn.Network ninp;
			Cdn.Network nout;

			ninp = LoadNetwork(input);
			nout = LoadNetwork(output);

			var tin = nout.Integrator.Variable("t");
			var dtin = nout.Integrator.Variable("dt");

			tin.Flags = VariableFlags.Out;
			dtin.Flags = VariableFlags.Out;

			var tout = nout.Integrator.Variable("t");
			var dtout = nout.Integrator.Variable("dt");

			tout.Flags = VariableFlags.In;
			dtout.Flags = VariableFlags.In;

			// Create bindings for each input/output pair of variables in both
			// networks
			var inputsInOut = ninp.FindVariables("recurse(children) | if(has-flag(in), has-flag(out)) | not(parent | functions)");
			List<Binding> bindings = new List<Binding>();

			foreach (var v in inputsInOut)
			{
				var vout = FindSame(nout, v);

				if (vout != null)
				{
					if (!v.Dimension.Equal(vout.Dimension))
					{
						Log.WriteLine("The dimensions of `{0}' in the input are not equal to the output dimensions ({1}-by-{2} and {3}-by-{4})",
						              v.FullNameForDisplay,
						              v.Dimension.Rows,
						              v.Dimension.Columns,
						              vout.Dimension.Rows,
						              vout.Dimension.Columns);
					}
					else
					{
						bindings.Add(new Binding { Input = v, Output = vout });
					}
				}
			}

			var files = Options.Instance.Formatter.Bind(ninp, nout, bindings);

			string s;

			if (files.Length <= 1)
			{
				s = String.Join(", ", files);
			}
			else
			{
				s = String.Format("{0} and {1}", String.Join(", ", files, 0, files.Length - 1), files[files.Length - 1]);
			}

			Log.WriteLine("Generated {0} from binding `{1}' to `{2}'...", s, input, output);
		}

		private Cdn.Variable FindSame(Cdn.Network nout, Cdn.Variable v)
		{
			var parent = v.Object;

			Stack<Cdn.Object> parents = new Stack<Object>();

			while (parent != null && parent.Parent != null)
			{
				parents.Push(parent);
				parent = parent.Parent;
			}

			Cdn.Object outParent = nout;

			while (outParent != null && parents.Count > 0)
			{
				parent = parents.Pop();

				var node = outParent as Cdn.Node;

				if (node != null)
				{
					outParent = node.GetChild(parent.Id);
				}
				else
				{
					outParent = null;
				}
			}

			if (outParent != null)
			{
				return outParent.Variable(v.Name);
			}
			else
			{
				return null;
			}
		}

		private Cdn.Network LoadNetwork(string filename)
		{
			Cdn.Network n;

			try
			{
				n = new Cdn.Network(filename);
			}
			catch (GLib.GException e)
			{
				throw new Exception("Failed to load network: {0}", e.Message);
			}

			CompileError error = new CompileError();

			if (!n.Compile(null, error))
			{
				throw new Exception("Failed to compile network: {0}", error.FormattedString);
			}

			return n;
		}
	}
}

