using System;
using System.Collections.Generic;
using System.Text;

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
						
						item.Alias = string.Format("{0} /* RAND_{1} */", item.Index, item.Index - firstrand);
					}
					else if ((item.Type & DataTable.DataItem.Flags.Constant) != 0)
					{
						item.Alias = string.Format("{0} /* {1} */", item.Index, item.Key);
					}
					
					continue;
				}
				
				string fullname;
				
				if (prop.Object == d_program.Options.Network || prop.Object == d_program.Options.Network.Integrator)
				{
					fullname = prop.Name;
				}
				else
				{
					fullname = prop.FullName;
				}
				
				string orig = Context.ToAsciiOnly(fullname).ToUpper();
				string prefix;
				
				if (isdiff)
				{
					prefix = String.Format("{0}_DERIV", CPrefixUp);
				}
				else
				{
					prefix = String.Format("{0}_STATE", CPrefixUp);
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
					item.Alias = enumname;
				}
				
				unique[enumname] = true;
				
				d_enumMap.Add(new EnumItem(prop, shortname, enumname, comment, item.Index.ToString()));
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

		protected Dictionary<Tree.NodePath, string> GenerateMapping(string format, IEnumerable<Tree.Embedding.Argument> args)
		{
			Dictionary<Tree.NodePath, string > mapping = new Dictionary<Tree.NodePath, string>();
			
			foreach (Tree.Embedding.Argument arg in args)
			{
				mapping[arg.Path] = String.Format(format, arg.Index);
			}
			
			return mapping;
		}
	}
}