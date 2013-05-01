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

		public string[] Write(Program program)
		{
			d_program = program;

			Initialize(program, d_options);

			string filename = Path.Combine(d_program.Options.Output, d_program.Options.Basename + ".js");

			d_writer = new StreamWriter(filename);

			d_writer.WriteLine("(function(Cdn) {");

			WriteSource();

			d_writer.WriteLine("})(typeof window !== 'undefined' ? (window.Cdn = window.Cdn || {}) : (global.Cdn = global.Cdn || {}))");

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
			d_writer.WriteLine(ReadResource("Cdn.js"));
			d_writer.WriteLine(ReadResource("Cdn.Utils.js"));
			d_writer.WriteLine(ReadResource("Cdn.Math.js"));
			d_writer.WriteLine(ReadResource("Cdn.Integrators.js"));
			d_writer.WriteLine(ReadResource("Cdn.Integrators.Euler.js"));
			d_writer.WriteLine(ReadResource("Cdn.Integrators.RungeKutta.js"));

			WriteConstructor();
			WriteClearData();

			WriteClassData();

			WriteFunctions();
			WriteAPI();
			WriteEventsSource();
			WriteDataAccessors();
		}

		private void WriteConstructor()
		{
			d_writer.WriteLine("Cdn.Networks.{0} = function()\n{{", CPrefix);
			d_writer.WriteLine("\tthis.{0} = {{}};", Context.DataName);
			d_writer.WriteLine("\tthis._clear_data();");
			d_writer.WriteLine("\tthis._integrator = new Cdn.Integrators['{0}']();",
			                   Knowledge.Instance.Network.Integrator.Name);

			d_writer.WriteLine("\tthis.reset(0);");
			d_writer.WriteLine("}\n");

			d_writer.WriteLine("Cdn.Networks.{0}.prototype.step = function(t, dt)\n{{", CPrefix);
			d_writer.WriteLine("\tthis._integrator.step(this, t, dt);", Context.DataName);
			d_writer.WriteLine("}\n");
		}

		private void WriteStateTableArray()
		{
			d_writer.WriteLine("[");

			foreach (var item in d_program.StateTable)
			{
				if (item.Dimension.IsOne)
				{
					d_writer.WriteLine("\t\t0.0,");
				}
				else
				{
					d_writer.WriteLine("\t\t{0},", Context.ZeroArrayOfSize(item.Dimension.Size()));
				}
			}

			d_writer.Write("\t]");
		}

		private void WriteClearData()
		{
			d_writer.WriteLine("Cdn.Networks.{0}.prototype._clear_data = function()\n{{", CPrefix);
			d_writer.Write("\tthis.{0}.{1} = ",
			               Context.DataName,
			               d_program.StateTable.Name);
			WriteStateTableArray();
			d_writer.WriteLine(";\n");

			d_writer.WriteLine("\tthis.{0}.event_states = {1};",
			                   Context.DataName,
			                   Context.ZeroArrayOfSize(Knowledge.Instance.EventContainersCount, false));

			d_writer.WriteLine("\tthis.{0}.events_active = {1};",
			                   Context.DataName,
			                   Context.ZeroArrayOfSize(Knowledge.Instance.EventsCount, false));

			d_writer.WriteLine("\tthis.{0}.events_active_size = 0;",
			                   Context.DataName);

			foreach (DataTable table in d_program.DataTables)
			{
				if (!table.IsConstant)
				{
					WriteDataTable(table, "this", "\t");
				}
			}

			d_writer.WriteLine("}\n");
		}

		private void WriteClassData()
		{
			var cls = String.Format("Cdn.Networks.{0}", CPrefix);

			d_writer.WriteLine("{0}.name = \"{1}\";", cls, CPrefix);

			d_writer.WriteLine("{0}.event_refinement = {1};",
			                   cls,
			                   NeedsSpaceForEvents() ? "true" : "false");

			foreach (DataTable table in d_program.DataTables)
			{
				if (table.IsConstant)
				{
					WriteDataTable(table, cls, "");
				}
			}

			WriteEnums();
			WriteDimensions();

			var range = d_program.StateRange(Knowledge.Instance.Integrated);

			if (range == null)
			{
				d_writer.WriteLine("{0}.states = {{start: 0, end: 0}};",
				                   cls);
			}
			else
			{
				d_writer.WriteLine("{0}.states = {{start: {1}, end: {2}}};",
				                   cls,
				                   range[0],
				                   range[1]);
			}

			var drange = d_program.StateRange(Knowledge.Instance.DerivativeStates);

			if (drange == null)
			{
				d_writer.WriteLine("{0}.derivatives = {{start: 0, end: 0}};",
				                   cls);
			}
			else
			{
				d_writer.WriteLine("{0}.derivatives = {{start: {1}, end: {2}}};",
				                   cls,
				                   drange[0],
				                   drange[1]);
			}
		}

		private void WriteDimensions()
		{
			d_writer.WriteLine("Cdn.Networks.{0}.dimensions = [", CPrefix);

			foreach (var item in d_program.StateTable)
			{
				var dim = item.Dimension;
				d_writer.WriteLine("\t{{rows: {0}, columns: {1}, size: {2}}},", dim.Rows, dim.Columns, dim.Size());
			}

			d_writer.WriteLine("];");
			d_writer.WriteLine();
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

		private void WriteDataTable(DataTable table, string on, string indent)
		{
			if (table.Count == 0)
			{
				return;
			}

			d_writer.Write("{0}{1}.{2} = [", indent, on, table.Name);

			if (!table.NeedsInitialization)
			{
				d_writer.Write(ZeroArrayOfSize(table.Count));
				d_writer.WriteLine("];");
				return;
			}

			var translator = new InitialValueTranslator();

			for (int i = 0; i < table.Count; ++i)
			{
				string val = translator.Translate(table[i].Key);

				if (table.Columns > 0)
				{
					if (i % table.Columns == 0)
					{
						if (i != 0)
						{
							d_writer.Write("],");
						}

						d_writer.Write("\n");
						d_writer.Write(indent);
						d_writer.Write("\t[");
					}
					else if (i != 0)
					{
						d_writer.Write(", ");
					}
				}
				else if (i == 0)
				{
					d_writer.WriteLine();
					d_writer.Write(indent);
					d_writer.Write("\t");
				}
				else
				{
					d_writer.Write(", ");
				}

				d_writer.Write("{0}", val);
			}

			if (table.Columns > 0)
			{
				d_writer.WriteLine("]");
			}
			else
			{
				d_writer.WriteLine();
			}

			d_writer.Write(indent);
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
			CLike.Context context = new Context(d_program,
			                                    d_options,
			                                    function.Expression,
			                                    GenerateMapping("x{0}", function.Arguments));

			return InstructionTranslator.QuickTranslate(context);
		}

		private string GenerateArgsList(Programmer.Function function)
		{
			List<string> ret = new List<string>(function.NumArguments + 2);

			if (function.IsCustom)
			{
				int i = 0;

				foreach (var arg in function.CustomArguments)
				{
					ret.Add(String.Format("x{0}", i));
					++i;
				}
			}
			else
			{
				for (int i = 0; i < function.OrderedArguments.Count; ++i)
				{
					ret.Add(String.Format("x{0}", i));
				}
			}

			return String.Join(", ", ret.ToArray());
		}

		private void WriteFunction(Programmer.Function function)
		{
			d_writer.WriteLine("Cdn.Networks.{0}.prototype.{1} = function ({2}) {{", CPrefix, function.Name, GenerateArgsList(function));

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

		private void WriteEventsSource()
		{
			WriteEventActive();
			WriteEventFire();
			WriteEventsUpdateDistance(d_writer);
			WriteEventsPostUpdate();
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

			d_writer.Write("Cdn.Networks.{0}.prototype.{1} = function(", CPrefix, api.Name);

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

		protected override string EnumAlias(string name)
		{
			return "Cdn." + CPrefix + ".States." + name;
		}

		private void WriteEnums()
		{
			d_writer.WriteLine("Cdn.Networks.{0}.States = {{", CPrefix);

			foreach (var e in d_enumMap)
			{
				d_writer.WriteLine("\t{0}: {1}, /* {2} */",
				                   e.CName,
				                   e.Value,
				                   e.Comment);
			}

			d_writer.WriteLine("};\n");
		}

		protected override Cdn.RawC.Programmer.Formatters.CLike.Context CreateContext()
		{
			return new Context(d_program, d_options);
		}

		protected override void WriteEventsUpdateDistance(TextWriter writer)
		{
			writer.WriteLine("Cdn.Networks.{0}.prototype.events_update_distance = function()",
			                 CPrefix);

			writer.WriteLine("{");

			// Write updates of the logical nodes
			var states = new List<EventNodeState>(Knowledge.Instance.EventNodeStates);

			if (states.Count == 0)
			{
				writer.WriteLine("}");
				writer.WriteLine();
				return;
			}

			base.WriteEventsUpdateDistance(writer);

			var range = d_program.StateRange(Knowledge.Instance.EventNodeStates);

			writer.WriteLine("\tthis.{0}.events_active_size = 0;", Context.DataName);
			writer.WriteLine();
			writer.WriteLine("\tfor (var i = 0; i < {0}; ++i)", Knowledge.Instance.EventsCount);
			writer.WriteLine("\t{");
			writer.WriteLine("\t\tif (this.event_active(i) && this.data.{0}[{1} + i * 3] >= 0)",
			                 d_program.StateTable.Name,
			                 range[0] + 2);
			writer.WriteLine("\t\t{");
			writer.WriteLine("\t\t\tvar vi = this.get_events_value(i);");
			writer.WriteLine();
			writer.WriteLine("\t\t\tfor (var j = this.{0}.events_active_size; j > 0; --j)", Context.DataName);
			writer.WriteLine("\t\t\t{");
			writer.WriteLine("\t\t\t\tvar vj = this.get_events_value(this.{0}.events_active[j - 1]);", Context.DataName);
			writer.WriteLine();
			writer.WriteLine("\t\t\t\tif (vj.distance() <= vi.distance())");
			writer.WriteLine("\t\t\t\t{");
			writer.WriteLine("\t\t\t\t\tbreak;");
			writer.WriteLine("\t\t\t\t}");
			writer.WriteLine("\t\t\t\telse");
			writer.WriteLine("\t\t\t\t{");
			writer.WriteLine("\t\t\t\t\tthis.{0}.events_active[j] = this.{0}.events_active[j - 1];", Context.DataName);
			writer.WriteLine("\t\t\t\t}");
			writer.WriteLine("\t\t\t}");
			writer.WriteLine();
			writer.WriteLine("\t\t\tthis.{0}.events_active[j] = i;", Context.DataName);
			writer.WriteLine("\t\t\t++this.{0}.events_active_size;", Context.DataName);
			writer.WriteLine("\t\t}");
			writer.WriteLine("\t}");
			writer.WriteLine("\t}");
			writer.WriteLine("}");
			writer.WriteLine();
		}

		private void WriteEventsPostUpdate()
		{
			d_writer.WriteLine("Cdn.Networks.{0}.prototype.events_post_update = function()",
			                 CPrefix);
			d_writer.WriteLine("{");

			var enu = Knowledge.Instance.EventNodeStates.GetEnumerator();

			if (enu.MoveNext())
			{
				var st = d_program.StateTable[enu.Current];
				var idx = st.AliasOrIndex;

				d_writer.WriteLine("\tfor (var i = {0}; i < {1}; i += 3)",
				                 idx,
				                 st.DataIndex + Knowledge.Instance.EventNodeStatesCount);

				d_writer.WriteLine("\t{");
				d_writer.WriteLine("\t\tthis.{0}.{1}[i] = this.{0}.{1}[i + 1];",
				                   Context.DataName,
				                   d_program.StateTable.Name);
				d_writer.WriteLine("\t}");
			}

			d_writer.WriteLine("}");
			d_writer.WriteLine();
		}

		private void WriteEventActive()
		{
			if (Knowledge.Instance.EventNodeStatesCount == 0)
			{
				return;
			}

			d_writer.WriteLine("Cdn.Networks.{0}.event_active = function(i)", CPrefix);
			d_writer.WriteLine("{");

			bool hasit = false;

			foreach (var ev in Knowledge.Instance.Events)
			{
				if (ev.Phases.Length > 0)
				{
					hasit = true;
					break;
				}
			}

			if (!hasit)
			{
				d_writer.WriteLine("\treturn 1;");
			}
			else
			{
				d_writer.WriteLine("\tswitch (i)");
				d_writer.WriteLine("\t{");

				int i = 0;

				foreach (var ev in Knowledge.Instance.Events)
				{
					var phases = ev.Phases;
					++i;

					if (phases.Length == 0)
					{
						continue;
					}

					d_writer.WriteLine("\tcase {0}:", i - 1);

					var parent = Knowledge.Instance.FindStateNode(ev);
					var cont = Knowledge.Instance.EventStatesMap[parent];
					var idx = cont.Index;

					List<string> conditions = new List<string>();

					foreach (var ph in phases)
					{
						var st = Knowledge.Instance.GetEventState(parent, ph);

						conditions.Add(String.Format("{0}[{1}] == {2}", d_program.EventStatesTable.Name, idx, st.Index));
					}

					d_writer.WriteLine("\t\treturn ({0});", String.Join(" || ", conditions));
				}

				d_writer.WriteLine("\tdefault:");
				d_writer.WriteLine("\t\treturn 1;");

				d_writer.WriteLine("\t}");
			}

			d_writer.WriteLine("}");
			d_writer.WriteLine();
		}

		private void WriteEventFire()
		{
			d_writer.WriteLine("Cdn.Networks.{0}.events_fire = function()", CPrefix);
			d_writer.WriteLine("{");

			bool hasit = false;

			foreach (var ev in Knowledge.Instance.Events)
			{
				var state = ev.GotoState;
				var prg = d_program.EventProgram(ev);

				if (!String.IsNullOrEmpty(state) || prg != null)
				{
					hasit = true;
					break;
				}
			}

			if (!hasit)
			{
				d_writer.WriteLine("}");
				d_writer.WriteLine();
				return;
			}

			d_writer.WriteLine("\tfor (var event = 0; event < this.{0}.events_active_size; ++event)",
			                   Context.DataName);
			d_writer.WriteLine("\t{");
			d_writer.WriteLine("\t\ti = this.{0}.events_active[event];", Context.DataName);

			d_writer.WriteLine();
			d_writer.WriteLine("\t\tswitch (i)");
			d_writer.WriteLine("\t\t{");

			int i = 0;

			foreach (var ev in Knowledge.Instance.Events)
			{
				var state = ev.GotoState;
				++i;

				var prg = d_program.EventProgram(ev);

				if (String.IsNullOrEmpty(state) && prg == null)
				{
					continue;
				}

				d_writer.WriteLine("\t\tcase {0}:", i - 1);
				d_writer.WriteLine("\t\t\tif (this.{0}.event_active ({1}))", Context.DataName, i - 1);
				d_writer.WriteLine("\t\t\t{");

				if (!String.IsNullOrEmpty(state))
				{
					var parent = Knowledge.Instance.FindStateNode(ev);
					var cont = Knowledge.Instance.EventStatesMap[parent];
					var idx = cont.Index;
					var st = Knowledge.Instance.GetEventState(parent, state);

					d_writer.WriteLine("\t\t\t\tthis.{0}.event_states[{1}] = {2};",
					                   Context.DataName,
					                   idx,
					                   st.Index);
				}

				if (prg != null)
				{
					WriteComputationNode(prg, "\t\t\t\t");
				}

				d_writer.WriteLine("\t\t\t}");

				d_writer.WriteLine("\t\t\tbreak;");
			}

			d_writer.WriteLine("\t\tdefault:");
			d_writer.WriteLine("\t\t\tbreak;");

			d_writer.WriteLine("\t\t}");
			d_writer.WriteLine("\t}");
			d_writer.WriteLine("}");
			d_writer.WriteLine();
		}

		private void WriteDataAccessors()
		{
			d_writer.WriteLine("Cdn.Networks.{0}.prototype.data = function(d)", CPrefix);
			d_writer.WriteLine("{");
			d_writer.WriteLine("\tif (typeof d === 'undefined')");
			d_writer.WriteLine("\t{");
			d_writer.WriteLine("\t\treturn this.{0}.{1};",
			                   Context.DataName,
			                   d_program.StateTable.Name);
			d_writer.WriteLine("\t}");
			d_writer.WriteLine("\telse");
			d_writer.WriteLine("\t{");
			d_writer.WriteLine("\t\tthis.{0}.{1} = d;",
			                   Context.DataName,
			                   d_program.StateTable.Name);
			d_writer.WriteLine("\t}");
			d_writer.WriteLine("}");
			d_writer.WriteLine();

			d_writer.WriteLine("Cdn.Networks.{0}.prototype.integrator = function(d)", CPrefix);
			d_writer.WriteLine("{");
			d_writer.WriteLine("\tif (typeof d === 'undefined')");
			d_writer.WriteLine("\t{");
			d_writer.WriteLine("\t\treturn this._integrator;",
			                   Context.DataName,
			                   d_program.StateTable.Name);
			d_writer.WriteLine("\t}");
			d_writer.WriteLine("\telse");
			d_writer.WriteLine("\t{");
			d_writer.WriteLine("\t\tthis._integrator = d;",
			                   Context.DataName,
			                   d_program.StateTable.Name);
			d_writer.WriteLine("\t}");
			d_writer.WriteLine("}");
			d_writer.WriteLine();

			d_writer.WriteLine("Cdn.Networks.{0}.prototype.states = function()", CPrefix);

			var range = d_program.StateRange(Knowledge.Instance.Integrated);

			d_writer.WriteLine("{");

			if (range == null)
			{
				d_writer.WriteLine("\treturn [];");
			}
			else
			{
				d_writer.WriteLine("\treturn this.{0}.{1}.slice({2}, {3});",
				                   Context.DataName,
				                   d_program.StateTable.Name,
				                   range[0],
				                   range[1]);
			}

			d_writer.WriteLine("}");
			d_writer.WriteLine();

			d_writer.WriteLine("Cdn.Networks.{0}.prototype.derivatives = function()", CPrefix);

			range = d_program.StateRange(Knowledge.Instance.DerivativeStates);

			d_writer.WriteLine("{");

			if (range == null)
			{
				d_writer.WriteLine("\treturn [];");
			}
			else
			{
				d_writer.WriteLine("\treturn this.{0}.{1}.slice({2}, {3});",
				                   Context.DataName,
				                   d_program.StateTable.Name,
				                   range[0],
				                   range[1]);
			}

			d_writer.WriteLine("}");
			d_writer.WriteLine();

			d_writer.WriteLine("Cdn.Networks.{0}.prototype.events_active = function(i)", CPrefix);
			d_writer.WriteLine("{");
			d_writer.WriteLine("\treturn this.{0}.events_active[i];", Context.DataName);
			d_writer.WriteLine("}");
			d_writer.WriteLine();

			d_writer.WriteLine("Cdn.Networks.{0}.prototype.events_active_size = function()", CPrefix);
			d_writer.WriteLine("{");
			d_writer.WriteLine("\treturn this.{0}.events_active_size;", Context.DataName);
			d_writer.WriteLine("}");
			d_writer.WriteLine();

			d_writer.WriteLine("Cdn.Networks.{0}.prototype.events_value = function(i)", CPrefix);

			range = d_program.StateRange(Knowledge.Instance.EventNodeStates);

			d_writer.WriteLine("{");

			if (range == null)
			{
				d_writer.WriteLine("\treturn null;");
			}
			else
			{
				d_writer.WriteLine("\treturn new Cdn.EventValue(this.{0}.{1}, {2} + i * 3);",
				                   Context.DataName,
				                   d_program.StateTable.Name,
				                   range[0]);
			}

			d_writer.WriteLine("}");
			d_writer.WriteLine();
		}

		public string[] Compile(bool verbose)
		{
			return null;
		}

		public string CompileForValidation(string[] sources, bool verbose)
		{
			return null;
		}

		public IEnumerator<double[]> RunForValidation(string[] sources, double t, double dt)
		{
			return null;
		}
	}
}