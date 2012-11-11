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
			
			ret.Add(new Cdn.Monitor(d_network, d_network.Integrator.Variable("t")));
			
			Knowledge.Initialize(d_network);
			
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Integrated))
			{
				ret.Add(new Cdn.Monitor(d_network, v));
			}
			
			foreach (var v in Knowledge.Instance.FlaggedVariables(VariableFlags.Out))
			{
				ret.Add(new Cdn.Monitor(d_network, v));
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

		private void ReadAndCompare(DynamicNetwork dynnet, List<uint> indices, int row, double t)
		{
			List<string> failures = new List<string>();

			for (int i = 0; i < indices.Count; ++i)
			{
				double rawcval = dynnet.Value(indices[i]);
				double cdnval = d_data[i][row];

				if (System.Math.Abs(cdnval - rawcval) > Options.Instance.ValidatePrecision ||
				    double.IsNaN(cdnval) != double.IsNaN(rawcval))
				{
					failures.Add(String.Format("{0} (got {1} but expected {2})",
						         d_monitors[i].Variable.FullName,
						         rawcval,
						         cdnval));
				}
			}

			if (failures.Count > 0)
			{
				throw new Exception(String.Format("Discrepancy detected at t = {0}:\n  {1}",
					                              t,
					                              String.Join("\n  ", failures.ToArray())));

			}
		}

		public void Validate(Programmer.Program program)
		{
			Log.WriteLine("Validating generated network...");

			Options opts = Options.Instance;

			string shlib = opts.Formatter.CompileForValidation(opts.Verbose);

			double ts;

			if (opts.DelayTimeStep <= 0)
			{
				ts = opts.ValidateRange[1];
			}
			else
			{
				ts = opts.DelayTimeStep;
			}

			// Create dynamic binding to the shared lib API for rawc
			var dynnet = new DynamicNetwork(shlib, program.Options.Basename);
			double t = opts.ValidateRange[0];

			dynnet.Reset(t);

			var indices = d_monitors.ConvertAll<uint>(a => (uint)program.StateTable[a.Variable].DataIndex);

			var dtstate = program.StateTable[Knowledge.Instance.TimeStep];

			for (int i = 0; i < d_data.Count; ++i)
			{
				ReadAndCompare(dynnet, indices, i, t);

				dynnet.Step(t, ts);
				t += dynnet.Value((uint)dtstate.DataIndex);
			}

			Log.WriteLine("Network {0} successfully validated...", d_network.Filename);
		}

		private class DynamicNetwork
		{
			private Type d_type;
			private Type d_valuetype;
			private string d_name;
		
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
				var typesize = tb.DefinePInvokeMethod("cdn_rawc_" + d_name  + "_get_type_size",
				                                      shlib,
				                                      MethodAttributes.Public |
				                                      MethodAttributes.Static |
				                                      MethodAttributes.PinvokeImpl,
				                                      CallingConventions.Standard,
				                                      typeof(byte),
				                                      new Type[] {},
				                                      CallingConvention.StdCall,
				                                      CharSet.Auto);

				// Implementation flags for preserving signature
				typesize.SetImplementationFlags(typesize.GetMethodImplementationFlags() |
				                                MethodImplAttributes.PreserveSig);
			
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

				var s = (byte)tp.InvokeMember("cdn_rawc_" + d_name + "_get_type_size",
				                              BindingFlags.InvokeMethod,
				                              null,
				                              Activator.CreateInstance(tp),
				                              new object[] {});
			
				if (s == sizeof(float))
				{
					d_valuetype = typeof(float);
				}
				else
				{
					d_valuetype = typeof(double);
				}
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

				// Define dynamic PInvoke method
				var netinit = tb.DefinePInvokeMethod("cdn_rawc_" + d_name  + "_reset",
				                                     shlib,
				                                     MethodAttributes.Public |
				                                     MethodAttributes.Static |
				                                     MethodAttributes.PinvokeImpl,
				                                     CallingConventions.Standard,
				                                     null,
				                                     new Type[] {d_valuetype},
				                                     CallingConvention.StdCall,
				                                     CharSet.Auto);
	
				// Implementation flags for preserving signature
				netinit.SetImplementationFlags(netinit.GetMethodImplementationFlags() |
				                               MethodImplAttributes.PreserveSig);
	
				var netstep = tb.DefinePInvokeMethod("cdn_rawc_" + d_name  + "_step",
				                                     shlib,
				                                     MethodAttributes.Public |
				                                     MethodAttributes.Static |
				                                     MethodAttributes.PinvokeImpl,
				                                     CallingConventions.Standard,
				                                     null,
				                                     new Type[] {d_valuetype, d_valuetype},
				                                     CallingConvention.StdCall,
				                                     CharSet.Auto);
	
				// Implementation flags for preserving signature
				netstep.SetImplementationFlags(netstep.GetMethodImplementationFlags() |
				                               MethodImplAttributes.PreserveSig);
	
				var netdataget = tb.DefinePInvokeMethod("cdn_rawc_" + d_name  + "_get",
	                                     shlib,
	                                     MethodAttributes.Public |
	                                     MethodAttributes.Static |
	                                     MethodAttributes.PinvokeImpl,
	                                     CallingConventions.Standard,
	                                     d_valuetype,
	                                     new Type[] {typeof(UInt32)},
	                                     CallingConvention.StdCall,
	                                     CharSet.Auto);
	
				// Implementation flags for preserving signature
				netdataget.SetImplementationFlags(netdataget.GetMethodImplementationFlags() |
				                               MethodImplAttributes.PreserveSig);

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
			}

			public double Value(uint index)
			{
				var f = d_type.InvokeMember("cdn_rawc_" + d_name + "_get",
				                            BindingFlags.InvokeMethod,
				                            null,
				                            Activator.CreateInstance(d_type),
				                            new object[] {index});

				return (double)Convert.ChangeType(f, typeof(double));
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

