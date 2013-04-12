using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace Cdn.RawC.Programmer.Formatters.JavaScript
{
	[Plugins.Attributes.Plugin(Name="JavaScript",
	                           Description="Write compact JavaScript file",
	                           Author="Jesse van den Kieboom")]
	public class JavaScript : CLike.CLike, IFormatter, Plugins.IOptions
	{
		private Options d_options;
		private Programmer.Program d_program;

		private TextWriter d_writer;
		
		public JavaScript()
		{
			d_options = new Options("JavaScript Formatter");
		}

		public string[] Compile(bool verbose)
		{
			return null;
		}

		public string CompileForValidation(bool verbose)
		{
			return null;
		}

		public string[] Write(Program program)
		{
			d_program = program;

			Initialize(program, d_options);

			string filename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".js");

			d_writer = new StreamWriter(filename);
			
			WriteSource();

			d_writer.Flush();
			d_writer.Close();

			return new string[] {filename};
		}

		public CommandLine.OptionGroup Options
		{
			get { return d_options; }
		}

		private void WriteSource()
		{
			WriteEnums();

			WriteConstructor();

			WriteFunctions();
			WriteAPI();
		}

		private void WriteConstructor()
		{
			d_writer.WriteLine("Cdn.{0} = function()\n{{", CPrefix);
			WriteDataTables();
			d_writer.WriteLine("}");
		}

		private string ZeroArrayOfSize(int size)
		{
			StringBuilder ret = new StringBuilder();
			ret.Append('[');

			for (int i = 0; i < size; ++i)
			{
				if (i != 0)
				{
					ret.Append(", ");
				}

				ret.Append('0');
			}

			ret.Append(']');
			return ret.ToString();
		}

		private void WriteDataTables()
		{
			foreach (DataTable table in d_program.DataTables)
			{
				WriteDataTable(table);
			}
		}

		private void WriteDataTable(DataTable table)
		{
			if (table.Count == 0)
			{
				return;
			}
				
			d_writer.WriteLine("\tthis.{1} = [", CPrefix, table.Name);
			
			var translator = new CLike.InitialValueTranslator();
			
			for (int i = 0; i < table.Count; ++i)
			{
				string val = table.NeedsInitialization ? translator.Translate(table[i].Key) : "0";				

				if (table.Columns > 0)
				{
					if (i % table.Columns == 0)
					{
						if (i != 0)
						{
							d_writer.Write("], ");
						}

						d_writer.Write("[");
					}
				}

				d_writer.Write("{0}, ", val);
			}

			d_writer.WriteLine("];\n");
		}

		private string GenerateArgsList(string prefix, int num)
		{
			return GenerateArgsList(prefix, num, 0);
		}

		private string GenerateArgsList(string prefix, int num, int numstart)
		{
			List<string> ret = new List<string>(num);
			
			for (int i = 0; i < num; ++i)
			{
				ret.Add(String.Format("{0}{1}", prefix, numstart + i));
			}
			
			return String.Join(", ", ret.ToArray());
		}

		private string FunctionToJS(Programmer.Function function)
		{
			CLike.Context context = new CLike.Context(d_program, d_options, function.Expression, GenerateMapping("x{0}", function.Arguments));
			
			return InstructionTranslator.QuickTranslate(context);
		}

		private void WriteFunction(Programmer.Function function)
		{
			d_writer.WriteLine("Cdn.{0}.prototype.{1} = function ({2}) {{", CPrefix, function.Name, GenerateArgsList("x", function.NumArguments));
			
			d_writer.WriteLine("\treturn {0};", FunctionToJS(function));
			d_writer.WriteLine("}");
			d_writer.WriteLine();
		}

		private void WriteFunctions()
		{
			foreach (Programmer.Function function in d_program.Functions)
			{
				WriteFunction(function);
			}
		}

		private void WriteAPI()
		{
			foreach (var api in d_program.APIFunctions)
			{
				WriteAPIFunction(api);
			}
		}

		private void WriteComputationNode(Computation.INode node)
		{
			WriteComputationNode(node, "\t");
		}
		
		private void WriteComputationNode(Computation.INode node, string indent)
		{
			Context context = new Context(d_program, d_options);
			d_writer.WriteLine(Context.Reindent(ComputationNodeTranslator.Translate(node, context), indent));
		}
		
		private void WriteComputationNodes(IEnumerable<Computation.INode> nodes)
		{
			bool empty = false;
			bool written = false;
			
			foreach (Computation.INode node in nodes)
			{
				if (node is Computation.Empty)
				{
					empty = true;
					continue;
				}
				else if (empty && written)
				{
					WriteComputationNode(new Computation.Empty());
					empty = false;
				}
				
				WriteComputationNode(node);
				written = true;
			}
		}

		private void WriteAPIFunction(APIFunction api)
		{
			if (api.Private && api.Body.Count == 0)
			{
				return;
			}

			d_writer.Write("Cdn.{0}.prototype.{1} = function(", CPrefix, api.Name);
			
			for (int i = 0; i < api.Arguments.Length; i += 2)
			{
				if (i != 0)
				{
					d_writer.Write(", ");
				}
				
				var name = api.Arguments[i + 1];
				
				d_writer.Write(name);
			}
			
			d_writer.WriteLine(")\n{");

			if (api.Body.Count != 0)
			{
				WriteComputationNodes(api.Body);
			}
			
			d_writer.WriteLine("}");
			d_writer.WriteLine();
		}

		private void WriteEnums()
		{
			d_writer.WriteLine("Cdn.{0}.States = {{", CPrefix);

			foreach (var e in d_enumMap)
			{
				d_writer.WriteLine("{0}: {1}, /* {2} */",
				                   e.CName,
				                   e.Value,
				                   e.Comment);
			}

			d_writer.WriteLine("};\n");
		}
	}
}