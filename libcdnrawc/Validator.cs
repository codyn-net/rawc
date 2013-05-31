using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cdn.RawC
{
	public class Validator
	{
		private Cdn.Network d_network;
		private List<double[]> d_data;
		private List<Cdn.Monitor> d_monitors;

		public Validator(Cdn.Network network)
		{
			d_network = network;

			InitRand();

			if (!Options.Instance.PrintCompileSource)
			{
				RunCodyn();
			}
		}

		private void InitRand()
		{
			// Set seeds for all the rand instructions in the network
			var r = new System.Random();

			d_network.ForeachExpression((expr) => {
				foreach (var instr in expr.Instructions)
				{
					Cdn.InstructionRand rand = instr as Cdn.InstructionRand;

					if (rand != null)
					{
						var seed = (uint)(r.NextDouble() * (uint.MaxValue - 1) + 1);
						rand.Seed = seed;
					}
				}
			});
		}

		private void RunCodyn()
		{
			// Run the network now
			var ret = new List<Cdn.Monitor>();

			var t = d_network.Integrator.Variable("t");
			ret.Add(new Cdn.Monitor(d_network, t));

			foreach (var v in d_network.Integrator.State.AllVariables())
			{
				if (v != t && (v.Integrated || (v.Flags & Cdn.VariableFlags.InOut) != 0) && (v.Flags & Cdn.VariableFlags.FunctionArgument) == 0)
				{
					ret.Add(new Cdn.Monitor(d_network, v));
				}
			}

			double ts;

			if (Options.Instance.DelayTimeStep <= 0)
			{
				ts = Options.Instance.ValidateRange[1];
			}
			else
			{
				ts = Options.Instance.DelayTimeStep;
			}

			d_network.Run(Options.Instance.ValidateRange[0],
			              ts,
			              Options.Instance.ValidateRange[2]);

			// Extract the validation data
			d_data = new List<double[]>();

			for (int i = 0; i < ret.Count; ++i)
			{
				d_data.Add(ret[i].GetData());
			}

			d_monitors = ret;
		}

		private void ReadAndCompare(List<uint> indices, List<Cdn.Dimension> dimensions, int row, double[] data, double t)
		{
			List<string> failures = new List<string>();

			for (int i = 0; i < indices.Count; ++i)
			{
				var dim = dimensions[i];
				var size = dim.Size();

				for (int j = 0; j < size; ++j)
				{
					double rawcval = data[indices[i] + (uint)j];
					double cdnval = d_data[i][row * size + j];

					if (System.Math.Abs(cdnval - rawcval) > Options.Instance.ValidatePrecision ||
				        double.IsNaN(cdnval) != double.IsNaN(rawcval))
					{
						failures.Add(String.Format("{0}[{1}] (got {2} but expected {3})",
							         d_monitors[i].Variable.FullNameForDisplay,
							         j,
						             rawcval.ToString("R"),
						             cdnval.ToString("R")));
					}
				}
			}

			if (failures.Count > 0)
			{
				throw new Exception(String.Format("Discrepancy detected at t = {0}:\n  {1}",
					                              t,
					                              String.Join("\n  ", failures.ToArray())));

			}
		}

		public void Validate(Programmer.Program program, string[] sources)
		{
			Log.WriteLine("Validating generated network...");

			Options opts = Options.Instance;
			double dt;

			if (opts.DelayTimeStep <= 0)
			{
				dt = opts.ValidateRange[1];
			}
			else
			{
				dt = opts.DelayTimeStep;
			}

			double t = opts.ValidateRange[0];

			var indices = d_monitors.ConvertAll<uint>(a => (uint)program.StateTable[a.Variable].DataIndex);
			var dimensions = d_monitors.ConvertAll<Cdn.Dimension>(a => a.Variable.Dimension);

			var dtstate = program.StateTable[Knowledge.Instance.TimeStep];
			var len = d_data[0].Length - 1;

			string shlib = opts.Formatter.CompileForValidation(sources, opts.Verbose);
			IEnumerator<double[]> enu;

			if (shlib != null)
			{
				enu = ValidateSharedLib(program, shlib, t, dt);
			}
			else
			{
				enu = ValidateProgram(program, sources, t, dt);
			}

			enu.MoveNext();

			for (int i = 0; i < len; ++i)
			{
				var data = enu.Current;
				ReadAndCompare(indices, dimensions, i, data, t);
				enu.MoveNext();

				t += data[dtstate.DataIndex];
			}

			Log.WriteLine("Network {0} successfully validated...", d_network.Filename);
		}

		private IEnumerator<double[]> ValidateProgram(Programmer.Program program, string[] sources, double t, double dt)
		{
			return Options.Instance.Formatter.RunForValidation(sources, t, dt);
		}

		private IEnumerator<double[]> ValidateSharedLib(Programmer.Program program, string shlib, double t, double dt)
		{
			// Create dynamic binding to the shared lib API for rawc
			var dynnet = new DynamicNetwork(shlib, program.Options.Basename);
			var dtstate = program.StateTable[Knowledge.Instance.TimeStep];

			dynnet.Reset(t);

			yield return dynnet.Values();

			while (true)
			{
				dynnet.Step(t, dt);

				var vals = dynnet.Values();
				yield return vals;

				t += vals[dtstate.DataIndex];
			}
		}

		private class DynamicNetwork
		{
			private Type d_type;
			private Type d_valuetype;
			private string d_name;
			private uint d_shsize;
			private IntPtr d_shnetwork;
			private IntPtr d_dataptr;
			private double[] d_data;
			private Array d_realdata;

			private string ToAsciiOnly(string name)
			{
				StringBuilder builder = new StringBuilder();

				foreach (char c in name)
				{
					if (!char.IsLetterOrDigit(c))
					{
						builder.Append("_");
					}
					else
					{
						builder.Append(c);
					}
				}

				return builder.ToString();
			}

			private void DetermineValueType(string shlib, ModuleBuilder mb, string name)
			{
				TypeBuilder tb = mb.DefineType("DynamicRawcType" + Guid.NewGuid().ToString("N"));

				// Define dynamic PInvoke method
				DefineMethod(tb, shlib, "cdn_rawc_network_get_type_size", typeof(byte), typeof(IntPtr));
				DefineMethod(tb, shlib, "cdn_rawc_" + name  + "_network", typeof(IntPtr));

				Type tp;

				try
				{
					tp = tb.CreateType();
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Could not create dynamic type proxy: {0}", e.Message);
					d_valuetype = typeof(double);
					return;
				}

				var net = (IntPtr)tp.InvokeMember("cdn_rawc_" + name + "_network",
				                                  BindingFlags.InvokeMethod,
				                                  null,
				                                  Activator.CreateInstance(tp),
				                                  new object[] {});

				var s = (byte)tp.InvokeMember("cdn_rawc_network_get_type_size",
				                              BindingFlags.InvokeMethod,
				                              null,
				                              Activator.CreateInstance(tp),
				                              new object[] {net});

				if (s == sizeof(float))
				{
					d_valuetype = typeof(float);
				}
				else
				{
					d_valuetype = typeof(double);
				}
			}

			private void DefineMethod(TypeBuilder tb, string shlib, string name, Type rettype, params Type[] args)
			{
				var ret = tb.DefinePInvokeMethod(name,
				                                 shlib,
				                                 MethodAttributes.Public |
				                                 MethodAttributes.Static |
				                                 MethodAttributes.PinvokeImpl,
				                                 CallingConventions.Standard,
				                                 rettype,
				                                 args,
				                                 CallingConvention.StdCall,
				                                 CharSet.Auto);

				ret.SetImplementationFlags(ret.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);
			}

			public DynamicNetwork(string shlib, string basename)
			{
				AssemblyName name = new AssemblyName("DynamicRawcAssembly" +
				                                      Guid.NewGuid().ToString("N"));

				// Assembly builder
				AssemblyBuilder ab =
					AppDomain.CurrentDomain.DefineDynamicAssembly(name,
				                                                  AssemblyBuilderAccess.Run);

				// Module builder
				ModuleBuilder mb = ab.DefineDynamicModule("DynamicRawc");

				d_name = ToAsciiOnly(basename);
				DetermineValueType(shlib, mb, d_name);

				// Type builder
				TypeBuilder tb = mb.DefineType("DynamicRawc" + Guid.NewGuid().ToString("N"));

				DefineMethod(tb, shlib, "cdn_rawc_" + d_name + "_reset", null, d_valuetype);
				DefineMethod(tb, shlib, "cdn_rawc_" + d_name + "_step", null, d_valuetype, d_valuetype);
				DefineMethod(tb, shlib, "cdn_rawc_" + d_name + "_get", d_valuetype, typeof(UInt32));
				DefineMethod(tb, shlib, "cdn_rawc_" + d_name + "_network", typeof(IntPtr));
				DefineMethod(tb, shlib, "cdn_rawc_" + d_name + "_data", typeof(IntPtr));
				DefineMethod(tb, shlib, "cdn_rawc_network_get_data_count", typeof(UInt32), typeof(IntPtr));

				// Create the dynamic type
				try
				{
					d_type = tb.CreateType();
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Could not create dynamic type proxy: {0}", e.Message);
					return;
				}

				d_shnetwork = (IntPtr)d_type.InvokeMember("cdn_rawc_" + d_name + "_network",
				                                          BindingFlags.InvokeMethod,
				                                          null,
				                                          Activator.CreateInstance(d_type),
				                                          null);

				d_shsize = (UInt32)d_type.InvokeMember("cdn_rawc_network_get_data_count",
				                                       BindingFlags.InvokeMethod,
				                                       null,
				                                       Activator.CreateInstance(d_type),
				                                       new object[] {d_shnetwork});

				d_dataptr = (IntPtr)d_type.InvokeMember("cdn_rawc_" + d_name + "_data",
				                                        BindingFlags.InvokeMethod,
				                                        null,
				                                        Activator.CreateInstance(d_type),
				                                        null);

				d_data = new double[d_shsize];

				if (d_valuetype != typeof(double))
				{
					d_realdata = Array.CreateInstance(d_valuetype, d_shsize);
				}
			}

			public double[] Values()
			{
				if (d_realdata != null)
				{
					if (d_realdata.GetType().GetElementType() == typeof(float))
					{
						Marshal.Copy(d_dataptr, (float[])d_realdata, 0, (int)d_shsize);

						float[] fl = (float[])d_realdata;

						for (int i = 0; i < d_shsize; ++i)
						{
							d_data[i] = (double)fl[i];
						}
					}
				}
				else
				{
                    Marshal.Copy(d_dataptr, d_data, 0, (int)d_shsize);
                }

                return d_data;
			}

			public void Reset(double t)
			{
				d_type.InvokeMember("cdn_rawc_" + d_name + "_reset",
				                    BindingFlags.InvokeMethod,
				                    null,
				                    Activator.CreateInstance(d_type),
				                    new object[] {Convert.ChangeType(t, d_valuetype)});
			}

			public void Step(double t, double dt)
			{
				d_type.InvokeMember("cdn_rawc_" + d_name + "_step",
				                    BindingFlags.InvokeMethod,
				                    null,
				                    Activator.CreateInstance(d_type),
				                    new object[] {Convert.ChangeType(t, d_valuetype),
					                              Convert.ChangeType(dt, d_valuetype)});
			}
		}
	}
}

