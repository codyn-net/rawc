using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Cdn.RawC.Plugins
{
	public class Plugins
	{
		private static Plugins s_instance;
		private List<string> d_searchPaths;
		private bool d_scanned;
		private Dictionary<Type, Type[]> d_resolved;
		private Dictionary<string, Type> d_matchCache;

		public static Plugins Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = new Plugins();
				}

				return s_instance;
			}
		}

		private Plugins()
		{
			d_searchPaths = new List<string>();
			d_resolved = new Dictionary<Type, Type[]>();
			d_matchCache = new Dictionary<string, Type>();
		}

		public void LoadAssembly(string filename)
		{
			Assembly.LoadFrom(filename);
		}

		private void Scan()
		{
			if (d_scanned)
			{
				return;
			}

			foreach (string path in d_searchPaths)
			{
				foreach (string file in Directory.GetFiles(path, "*.dll"))
				{
					// Try to load the assembly
					LoadAssembly(file);
				}
			}

			d_scanned = true;
		}

		public void AddSearchPath(string searchPath)
		{
			d_searchPaths.Add(searchPath);
		}

		public Type[] Find(Type instanceof)
		{
			Scan();

			if (d_resolved.ContainsKey(instanceof))
			{
				return d_resolved[instanceof];
			}

			bool isinterface = instanceof.IsInterface;
			List<Type> types = new List<Type>();

			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in asm.GetTypes())
				{
					if (type == instanceof)
					{
						types.Add(type);
					}
					else if (isinterface && type.GetInterface(instanceof.FullName) != null)
					{
						types.Add(type);
					}
					else if (!isinterface && type.IsInstanceOfType(instanceof))
					{
						types.Add(type);
					}
				}
			}

			d_resolved[instanceof] = types.ToArray();
			return d_resolved[instanceof];
		}

		public Type Find(Type instanceof, string name)
		{
			name = name.ToLower();

			if (d_matchCache.ContainsKey(name))
			{
				return d_matchCache[name];
			}

			string[] parts = name.Split('.');

			foreach (Type type in Find(instanceof))
			{
				Attributes.PluginAttribute attr = GetInfo(type);
				string[] tname;

				if (attr != null && attr.Name != null)
				{
					tname = attr.Name.ToLower().Split('.');
				}
				else
				{
					tname = type.FullName.ToLower().Split('.');
				}

				int df = tname.Length - parts.Length;

				if (df < 0)
				{
					continue;
				}

				bool ismatch = true;

				for (int i = 0; i < parts.Length; ++i)
				{
					if (parts[i] != tname[df + i])
					{
						ismatch = false;
						break;
					}
				}

				if (ismatch)
				{
					d_matchCache[name.ToLower()] = type;
					return type;
				}
			}

			return null;
		}

		public Attributes.PluginAttribute GetInfo(Type type)
		{
			object[] attrs = type.GetCustomAttributes(typeof(Attributes.PluginAttribute), true);

			if (attrs.Length != 0)
			{
				Attributes.PluginAttribute ret = (Attributes.PluginAttribute)attrs[0];

				if (ret.Name == null)
				{
					ret.Name = type.Name;
				}

				return ret;
			}
			else
			{
				return null;
			}
		}

		public T Instantiate<T>(Type type, params object[] args)
		{
			Type[] argTypes = Array.ConvertAll<object, Type>(args, a => a.GetType());
			return (T)type.GetConstructor(argTypes).Invoke(args);
		}

		public T[] Instantiate<T>(params object[] args)
		{
			Type[] types = Find(typeof(T));
			T[] ret = new T[types.Length];

			for (int i = 0; i < types.Length; ++i)
			{
				ret[i] = Instantiate<T>(types[i], args);
			}

			return ret;
		}
	}
}

