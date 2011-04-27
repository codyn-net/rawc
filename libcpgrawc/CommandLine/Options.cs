using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Cpg.RawC.CommandLine
{
	public class Options : OptionGroup
	{
		private bool d_ignoreUnknown;
		private bool d_doubleDash;
		private string d_appname;
		private string d_appdescription;
		private string d_extradescription;
		private List<OptionGroup> d_groups;

		public Options() : this(null)
		{
		}
		
		public Options(string appname) : this(appname, null)
		{
		}
		
		public Options(string appname, string appdescription) : this(appname, appdescription, null)
		{
		}
		
		public Options(string appname, string appdescription, string extradescription)
		{
			d_ignoreUnknown = false;
			d_doubleDash = false;
			
			d_appname = appname;
			d_appdescription = appdescription;
			d_extradescription = extradescription;
			
			d_groups = new List<OptionGroup>();
			d_groups.Add(this);
		}
		
		public void Add(OptionGroup grp)
		{
			d_groups.Add(grp);
		}
		
		public void Remove(OptionGroup grp)
		{
			d_groups.Remove(grp);
		}
		
		public bool PassDoubleDash
		{
			get
			{
				return d_doubleDash;
			}
			set
			{
				d_doubleDash = value;
			}
		}
		
		public bool IgnoreUnknown
		{
			get
			{
				return d_ignoreUnknown;
			}
			set
			{
				d_ignoreUnknown = value;
			}
		}
		
		public string AppName
		{
			get
			{
				return d_appname;
			}
			set
			{
				d_appname = value;
			}
		}
		
		public string AppDescription
		{
			get
			{
				return d_appdescription;
			}
			set
			{
				d_appdescription = value;
			}
		}
		
		public string ExtraDescription
		{
			get
			{
				return d_extradescription;
			}
			set
			{
				d_extradescription = value;
			}
		}
		
		public void ShowHelp()
		{
			ShowHelp(Console.Out);
		}
		
		private bool HasOptions
		{
			get
			{
				foreach (OptionGroup grp in d_groups)
				{
					if (grp.OptionsCount > 0)
					{
						return true;
					}
				}
				
				return false;
			}
		}
		
		public new void ShowHelp(TextWriter writer)
		{
			writer.WriteLine("Usage:");
			
			if (!String.IsNullOrEmpty(d_appname))
			{
				writer.Write("  {0}", d_appname);
			}
			else
			{
				writer.Write("  {0}", AppDomain.CurrentDomain.FriendlyName);
			}
			
			if (HasOptions)
			{
				writer.Write(" [OPTION...]");
			}
			
			if (!String.IsNullOrEmpty(d_extradescription))
			{
				writer.Write(" {0}", d_extradescription);
			}
			
			if (!String.IsNullOrEmpty(d_appdescription))
			{
				writer.Write(" - {0}", d_appdescription);
			}
			
			writer.WriteLine();
			writer.WriteLine();
			
			foreach (OptionGroup grp in d_groups)
			{
				grp.ShowHelp(writer);
			}
		}
		
		private void ParseOption(OptionGroup opt, string[] args, string arg, Info info, bool canarg, string argument, ref int idx)
		{
			if (info.ValueType == typeof(bool))
			{
				info.Set(opt, true);
			}
			else if (canarg && (argument != null || idx < args.Length))
			{
				if (argument == null)
				{
					argument = args[idx];
					++idx;
				}

				info.Set(opt, argument);
			}
			else if (info.Option.OptionalArgument)
			{
				info.Set(opt, info.Option.DefaultArgument);
			}
			else
			{
				throw new OptionException("Expected value for option `{0}'...", arg);
			}
		}
		
		private bool ParseLong(string[] args, string arg, string argument, ref int idx)
		{
			Info info;
			
			foreach (OptionGroup opt in d_groups)
			{
				if (opt.LongName(arg, out info))
				{
					ParseOption(opt, args, arg, info, true, argument, ref idx);
					return true;
				}
			}

			return false;
		}
		
		private bool ParseShort(string[] args, string arg, bool islast, string argument, ref int idx)
		{
			Info info;
			
			foreach (OptionGroup opt in d_groups)
			{
				if (opt.ShortName(arg, out info))
				{
					if (info.ValueType != typeof(bool) && !islast && !info.Option.OptionalArgument)
					{
						throw new OptionException("Expected value for option `{0}'...", arg);
					}

					ParseOption(opt, args, arg, info, islast, argument, ref idx);
					return true;
				}
			}
			
			return false;
		}
		
		public virtual void Parse(ref string[] args)
		{
			List<string> rest = new List<string>();
			bool pass = false;
			int i = 0;
			
			while (i < args.Length)
			{
				string arg = args[i];
				++i;

				if (pass)
				{
					rest.Add(arg);
					continue;
				}
				
				if (d_doubleDash && arg == "--")
				{
					pass = true;
					continue;
				}
				
				if (!arg.StartsWith("-"))
				{
					rest.Add(arg);
					continue;
				}
				
				string argument = null;
				int pos = arg.IndexOf('=');
				
				if (pos >= 0)
				{
					argument = arg.Substring(pos + 1);
					arg = arg.Substring(0, pos);
				}
				
				bool ret = true;
				
				if (arg.StartsWith("--"))
				{
					ret = ParseLong(args, arg.Substring(2), argument, ref i);
				}
				else
				{
					string shortarg = arg.Substring(1);

					for (int j = 0; j < shortarg.Length; ++j)
					{
						bool islast = j == shortarg.Length - 1;

						ret = ParseShort(args, shortarg.Substring(j, 1), islast, argument, ref i);
					}
				}
				
				if (!ret)
				{
					if (d_ignoreUnknown)
					{
						rest.Add(arg);
					}
					else
					{
						throw new OptionException("The option `{0}' does not exist...", arg);
					}
				}
			}
			
			args = rest.ToArray();
		}
	}
}

