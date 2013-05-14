using System;

namespace Cdn.RawC.Application
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			GLib.GType.Init();

			Profile.Initialize();

			Options options;

			try
			{
				options = Options.Initialize(args);
			}
			catch (CommandLine.OptionException ex)
			{
				Console.Error.WriteLine("Failed to parse options: {0}", ex.Message);
				Environment.Exit(1);
				return;
			}

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

			if (options.Quiet)
			{
				Log.Base = null;
			}

			foreach (string filename in options.Files)
			{
				Generator generator = new Generator(filename);

				try
				{
					generator.Generate();

					if (!options.Validate && !options.Compile)
					{
						string[] files = Array.ConvertAll<string, string>(generator.WrittenFiles, (a) => {
							if (a.StartsWith(Environment.CurrentDirectory + "/"))
							{
								return String.Format("`{0}'", a.Substring(Environment.CurrentDirectory.Length + 1));
							}
							else
							{
								return String.Format("`{0}'", System.IO.Path.GetFileName(a));
							}
						});

						string s;

						if (files.Length <= 1)
						{
							s = String.Join(", ", files);
						}
						else
						{
							s = String.Format("{0} and {1}", String.Join(", ", files, 0, files.Length - 1), files[files.Length - 1]);
						}

						Log.WriteLine("Generated {0} from `{1}'...", s, filename);
					}
				}
				catch (System.Exception e)
				{
					System.Exception b = e.GetBaseException();

					if (!(b is NotImplementedException) && !(b is Cdn.RawC.Exception))
					{
						throw b;
					}

					if (b is Cdn.RawC.Exception)
					{
						Console.Error.WriteLine("\nAn exceptional error occurred while processing the network:\n\n{0}\n", b.Message);
					}
					else
					{
						Console.Error.WriteLine("\nYou are using a feature which is not yet implemented in rawc:");
						Console.Error.WriteLine();
						Console.Error.WriteLine("“{0}”\n", b.Message);
					}

					if (options.Verbose)
					{
						while (e != null)
						{
							Console.Error.WriteLine("Trace:");
							Console.Error.WriteLine("======");
							Console.Error.WriteLine("  - {0}", String.Join("\n  - ", e.StackTrace.Split('\n')));

							e = e.InnerException;

							if (e != null)
							{
								Console.Error.WriteLine("\n");
							}
						}
					}
					else
					{
						Console.Error.WriteLine("Use --verbose to see a stack trace of where the problem occurred");
					}

					Console.Error.WriteLine();

					Environment.Exit(1);
				}
			}

			Profile.Report(Console.Error);
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

