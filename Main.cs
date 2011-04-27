using System;

namespace Cpg.RawC
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			GLib.GType.Init();

			Options options = Options.Initialize(args);
			bool doexit = false;
			
			if (options.Collector == "")
			{
				ListCollectors();
				doexit = true;
			}
			
			if (options.Filter == "")
			{
				ListFilters();
				doexit = true;
			}
			
			if (options.ShowFormatters)
			{
				ListFormat();
				doexit = true;
			}
			
			if (doexit)
			{
				return;
			}

			foreach (string filename in options.Files)
			{
				Generator generator = new Generator(filename);
				
				try
				{
					generator.Generate();
				}
				catch (Exception e)
				{
					Exception b = e.GetBaseException();
					
					if (!(b is NotImplementedException))
					{
						throw e;
					}

					Console.Error.WriteLine("You are using a feature which is not yet implemented in rawc:");
					Console.Error.WriteLine();
					Console.Error.WriteLine("“{0}”", b.Message);

					Console.Error.WriteLine();
					Console.Error.WriteLine();
					Console.Error.WriteLine("Trace:");
					Console.Error.WriteLine("======");
					Console.Error.WriteLine("  - {0}", String.Join("\n  - ", b.StackTrace.Split('\n')));
					Environment.Exit(1);
				}
			}
		}
		
		private static void ListPlugins(Type[] types)
		{
			Plugins.Plugins plugins = Plugins.Plugins.Instance;

			foreach (Type plugin in types)
			{
				Plugins.Attributes.PluginAttribute info = plugins.GetInfo(plugin);
				
				if (info != null)
				{
					Console.WriteLine("{0}: {1}", info.Name.ToLower(), info.Description);
					Console.WriteLine("  Author: {0}", info.Author);
					Console.WriteLine();
				}
			}
		}
		
		private static void ListCollectors()
		{
			Plugins.Plugins plugins = Plugins.Plugins.Instance;

			Console.WriteLine("List of available collectors:");
			Console.WriteLine("=============================");
			Console.WriteLine();

			ListPlugins(plugins.Find(typeof(Tree.Collectors.ICollector)));
		}
		
		private static void ListFilters()
		{
			Plugins.Plugins plugins = Plugins.Plugins.Instance;
			
			Console.WriteLine("List of available filters:");
			Console.WriteLine("==========================");
			Console.WriteLine();
			
			ListPlugins(plugins.Find(typeof(Tree.Filters.IFilter)));
		}
		
		private static void ListFormat()
		{
			Plugins.Plugins plugins = Plugins.Plugins.Instance;
			
			Console.WriteLine("List of available formats:");
			Console.WriteLine("==========================");
			Console.WriteLine();
			
			ListPlugins(plugins.Find(typeof(Programmer.Formatters.IFormatter)));
		}
	}
}

