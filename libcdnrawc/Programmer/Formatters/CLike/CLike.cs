using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using CL = Cdn.RawC.Programmer.Formatters.CLike;

namespace Cdn.RawC.Programmer.Formatters.CLike
{
	public abstract class CLike
	{
		protected class EnumItem
		{
			public Cdn.Variable Variable;
			public string ShortName;
			public string CName;
			public string Comment;
			public string Value;

			public EnumItem(Cdn.Variable property, string shortname, string cname, string comment, string v)
			{
				Variable = property;
				ShortName = shortname;
				CName = cname;
				Comment = comment;
				Value = v;
			}
		}

		protected List<EnumItem> d_enumMap;
		private string d_cprefixdown;
		private string d_cprefixup;
		private string d_cprefix;
		private Program d_program;

		protected void Initialize(Program program, Options options)
		{
			d_program = program;

			options.CPrefix = CPrefix;
			options.CPrefixDown = CPrefixDown;
			options.CPrefixUp = CPrefixUp;

			InitializeEnum(options);

			var ctx = CreateContext();

			if (ctx.SupportsFirstClassArrays)
			{
				// Here we are going to translate the main data table into
				// a table where each element is an array if the element is
				// multidim
				foreach (var item in d_program.StateTable)
				{
					item.DataIndex = item.Index;
				}
			}
		}

		private string PrettyFullName(Cdn.Variable v)
		{
			if (v.Object == d_program.Options.Network || v.Object == d_program.Options.Network.Integrator)
			{
				return v.Name;
			}
			else
			{
				return v.FullName;
			}
		}

		protected virtual string EnumAlias(string name)
		{
			return name;
		}

		private void InitializeEnum(Options options)
		{
			d_enumMap = new List<EnumItem>();

			if (d_program.StateTable.Count == 0)
			{
				return;
			}

			Dictionary<string, bool > unique = new Dictionary<string, bool>();

			int firstrand = -1;

			foreach (DataTable.DataItem item in d_program.StateTable)
			{
				Cdn.Variable prop = null;
				bool isdiff = false;

				if ((item.Type & DataTable.DataItem.Flags.Derivative) != 0)
				{
					var state = item.Object as DerivativeState;

					if (state != null)
					{
						prop = state.Object as Cdn.Variable;
						isdiff = true;
					}
				}
				else if (item.Object is EventNodeState)
				{
					var state = item.Object as EventNodeState;

					if (Cdn.RawC.Options.Instance.Verbose && options.SymbolicNames)
					{
						item.Alias = String.Format("{0} /* {1}.{2} */", item.DataIndex, state.Event.FullIdForDisplay, state.Type.ToString().ToLower());
					}
				}
				else
				{
					prop = item.Key as Cdn.Variable;
				}

				if (prop == null || prop.Flags == 0)
				{
					if (!(Cdn.RawC.Options.Instance.Verbose && options.SymbolicNames))
					{
						continue;
					}

					var rinstr = item.Key as InstructionRand;

					if (rinstr != null)
					{
						if (firstrand == -1)
						{
							firstrand = item.Index;
						}

						item.Alias = string.Format("{0} /* RAND_{1} */", item.DataIndex, item.Index - firstrand);
					}
					else if ((item.Type & DataTable.DataItem.Flags.Constant) != 0)
					{
						item.Alias = string.Format("{0} /* {1} */", item.DataIndex, item.Key);
					}
					else if (prop != null)
					{
						item.Alias = string.Format("{0} /* {1} */", item.DataIndex, PrettyFullName(prop));
					}

					continue;
				}

				string fullname = PrettyFullName(prop);

				string orig = Context.ToAsciiOnly(fullname).ToUpper();
				string prefix;

				if (isdiff)
				{
					prefix = "DERIV";
				}
				else
				{
					prefix = "STATE";
				}

				string enumname = String.Format("{0}_{1}", prefix, orig);
				string shortname = orig;

				int id = 0;

				while (unique.ContainsKey(enumname))
				{
					enumname = String.Format("_{1}__{2}", prefix, orig, ++id);
					shortname = String.Format("{0}__{1}", orig, id);
				}

				var comment = fullname;

				if (isdiff)
				{
					comment += "'";
				}

				if (options.SymbolicNames)
				{
					item.Alias = EnumAlias(enumname);
				}

				unique[enumname] = true;

				d_enumMap.Add(new EnumItem(prop, shortname, enumname, comment, item.DataIndex.ToString()));
			}
		}

		protected string CPrefix
		{
			get
			{
				if (d_cprefix == null)
				{
					string[] parts = Context.ToAsciiOnly(d_program.Options.Basename).ToLower().Split('_');

					parts = Array.FindAll(parts, a => !String.IsNullOrEmpty(a));
					parts = Array.ConvertAll(parts, a => char.ToUpper(a[0]) + a.Substring(1));

					d_cprefix = String.Join("", parts);
				}

				return d_cprefix;
			}
		}

		protected string CPrefixUp
		{
			get
			{
				if (d_cprefixup == null)
				{
					d_cprefixup = Context.ToAsciiOnly(d_program.Options.Basename).ToUpper();
				}

				return d_cprefixup;
			}
		}

		protected string CPrefixDown
		{
			get
			{
				if (d_cprefixdown == null)
				{
					d_cprefixdown = CPrefixUp.ToLower();
				}

				return d_cprefixdown;
			}
		}

		protected Dictionary<Tree.NodePath, object> GenerateMapping(string format, IEnumerable<Tree.Embedding.Argument> args)
		{
			var mapping = new Dictionary<Tree.NodePath, object>();

			foreach (Tree.Embedding.Argument arg in args)
			{
				mapping[arg.Path] = String.Format(format, arg.Index);
			}

			return mapping;
		}

		public string ReadResource(string resource)
		{
			resource = "Cdn.RawC.Programmer.Formatters." + GetType().Name + ".Resources." + resource;

			Stream res = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
			StreamReader reader = new StreamReader(res);
			return reader.ReadToEnd();
		}

		private string EventNodeStateVariable(Cdn.EventLogicalNode node,
		                                      EventNodeState.StateType type,
		                                      Context context)
		{
			var st = d_program.StateTable[EventNodeState.Key(node, type)];

			return String.Format("{0}[{1}]", context.This(d_program.StateTable), st.AliasOrIndex);
		}

		protected virtual Context CreateContext()
		{
			return null;
		}

		private string EventConditionHolds(Cdn.Event ev,
		                                   Cdn.EventLogicalNode node,
		                                   EventNodeState.StateType type,
		                                   Context context)
		{
			var st = EventNodeStateVariable(node, type, context);

			switch (node.CompareType)
			{
			case Cdn.MathFunctionType.Less:
			case Cdn.MathFunctionType.Greater:
				return String.Format("{0} > 0",
				                     st);
			case Cdn.MathFunctionType.LessOrEqual:
			case Cdn.MathFunctionType.GreaterOrEqual:
			case Cdn.MathFunctionType.And:
			case Cdn.MathFunctionType.Or:
				return String.Format("{0} >= 0",
				                     st);
			case Cdn.MathFunctionType.Equal:
				if (ev.Approximation != Double.MaxValue)
				{
					var approx = context.TranslateNumber(ev.Approximation);

					return String.Format("{0}({1}) <= {2}",
					                     context.MathFunction(MathFunctionType.Abs, 1),
					                     st,
					                     approx);
				}
				else
				{
					return "1";
				}
			default:
				return "0";
			}
		}

		protected virtual void WriteEventsUpdateDistance(TextWriter writer)
		{
			// Write updates of the logical nodes
			var states = new List<EventNodeState>(Knowledge.Instance.EventNodeStates);

			if (states.Count == 0)
			{
				return;
			}

			bool first = true;
			var context = CreateContext();

			// Compute in reverse order to get the right dependencies
			for (int i = states.Count - 1; i >= 0; --i)
			{
				var st = states[i];

				if (st.Type != EventNodeState.StateType.Current)
				{
					continue;
				}

				if (first)
				{
					first = false;
				}
				else
				{
					writer.WriteLine();
				}

				switch (st.Node.CompareType)
				{
				case Cdn.MathFunctionType.And:
				case Cdn.MathFunctionType.Or:
				{
					var dist = EventNodeStateVariable(st.Node,
					                                  EventNodeState.StateType.Distance,
					                                  context);

					var cur = EventNodeStateVariable(st.Node,
					                                 EventNodeState.StateType.Current,
					                                 context);

					var ldist = EventNodeStateVariable(st.Node.Left,
					                                   EventNodeState.StateType.Distance,
					                                   context);

					var rdist = EventNodeStateVariable(st.Node.Right,
					                                   EventNodeState.StateType.Distance,
					                                   context);

					if (st.Node.CompareType == Cdn.MathFunctionType.And)
					{
						var lcond = EventConditionHolds(st.Event,
						                                st.Node.Left,
						                                EventNodeState.StateType.Current,
						                                context);

						var rcond = EventConditionHolds(st.Event,
						                                st.Node.Right,
						                                EventNodeState.StateType.Current,
						                                context);

						writer.WriteLine("\tif ({0} >= 0 && {1} >= 0)", ldist, rdist);
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = {1}({2}, {3});", dist, context.MathFunction(MathFunctionType.Max, 2), ldist, rdist);
						writer.WriteLine("\t}");
						writer.WriteLine("\telse if ({0} >= 0 && {1})", ldist, rcond);
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = {1};", dist, ldist);
						writer.WriteLine("\t}");
						writer.WriteLine("\telse if ({0} >= 0 && {1})", rdist, lcond);
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = {1};", dist, rdist);
						writer.WriteLine("\t}");
						writer.WriteLine("\telse");
						writer.WriteLine("\t{");
						writer.WriteLine("\t\t{0} = -1;", dist);
						writer.WriteLine("\t}");
						writer.WriteLine();
					}
					else
					{
						writer.WriteLine("\t{0} = {1}({2}, {3});", dist, context.MathFunction(MathFunctionType.Max, 2), ldist, rdist);
					}

					writer.WriteLine("\t{0} = ({1} >= 0 ? 0 : -1);", cur, dist);
				}
				break;
				default:
				{
					var prevCond = EventConditionHolds(st.Event,
					                                   st.Node,
					                                   EventNodeState.StateType.Previous,
					                                   context);

					var curCond = EventConditionHolds(st.Event,
					                                  st.Node,
					                                  EventNodeState.StateType.Current,
					                                  context);

					var dist = EventNodeStateVariable(st.Node,
					                                  EventNodeState.StateType.Distance,
					                                  context);

					var prev = EventNodeStateVariable(st.Node,
					                                  EventNodeState.StateType.Previous,
					                                  context);

					var cur = EventNodeStateVariable(st.Node,
					                                 EventNodeState.StateType.Current,
					                                 context);

					// Compute distance for actual values
					writer.WriteLine("\tif (!({0}) && {1})", prevCond, curCond);
					writer.WriteLine("\t{");

					if (st.Event.Approximation == Double.MaxValue)
					{
						writer.WriteLine("\t\t{0} = 1;", dist);
					}
					else
					{
						var approx = context.TranslateNumber(st.Event.Approximation);

						writer.WriteLine("\t\tif ({0} <= {1})", cur, approx);
						writer.WriteLine("\t\t{");
						writer.WriteLine("\t\t\t{0} = 1;", dist);
						writer.WriteLine("\t\t}");
						writer.WriteLine("\t\telse");
						writer.WriteLine("\t\t{");
						writer.Write("\t\t\t{0} = {1} / ({1} - {2})", dist, prev, cur);

						if (st.Node.CompareType == Cdn.MathFunctionType.Less ||
						    st.Node.CompareType == Cdn.MathFunctionType.Greater)
						{
							writer.WriteLine(" + 1e-10;");
						}
						else
						{
							writer.WriteLine(";");
						}

						writer.WriteLine("\t\t}");
					}

					writer.WriteLine("\t}");
					writer.WriteLine("\telse");
					writer.WriteLine("\t{");
					writer.WriteLine("\t\t{0} = -1;", dist);
					writer.WriteLine("\t}");

					break;
				}
				}
			}
		}

		public bool NeedsSpaceForEvents()
		{
			if (Knowledge.Instance.EventsCount == 0)
			{
				return false;
			}

			foreach (var ev in Knowledge.Instance.Events)
			{
				if (ev.Approximation != Double.MaxValue)
				{
					return true;
				}
			}

			return false;
		}
	}
}