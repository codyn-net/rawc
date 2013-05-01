using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace Cdn.RawC.CommandLine
{
	public class OptionGroup
	{
		internal abstract class Info
		{
			private OptionAttribute d_option;
			private object d_iscollection;
			private Type d_collectionType;
			private MethodInfo d_collectionAddMethod;

			public Info(OptionAttribute option)
			{
				d_option = option;
			}

			public OptionAttribute Option
			{
				get
				{
					return d_option;
				}
			}

			private object ConvertEnum(object val)
			{
				Array vals = Enum.GetValues(ValueType);
				string[] names = Enum.GetNames(ValueType);
				string cmpname = val.ToString().ToLower();

				for (int i = 0; i < names.Length; ++i)
				{
					if (names[i].ToLower() == cmpname)
					{
						return vals.GetValue(i);
					}
				}

				throw new InvalidCastException(String.Format("Could not cast `{0}' to `{1}'", val, ValueType.Name));
			}

			protected object Convert(object val)
			{
				if (ValueType == typeof(Enum))
				{
					return ConvertEnum(val);
				}
				else if (IsCollection)
				{
					return System.Convert.ChangeType(val, d_collectionType);
				}
				else
				{
					return System.Convert.ChangeType(val, ValueType);
				}
			}

			public abstract object Get(object instance);
			public abstract void Set(object instance, object val);
			public abstract Type ValueType
			{
				get;
			}

			public bool IsCollection
			{
				get
				{
					if (d_iscollection == null)
					{
						d_iscollection = false;

						foreach (Type type in ValueType.GetInterfaces())
						{
							if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
							{
								d_iscollection = true;
								d_collectionType = type.GetGenericArguments()[0];
								d_collectionAddMethod = ValueType.GetMethod("Add", new Type[] {d_collectionType});

								break;
							}
						}
					}

					return (bool)d_iscollection;
				}
			}

			public Type CollectionType
			{
				get
				{
					return d_collectionType;
				}
			}

			public void AddCollection(object instance, object val)
			{
				object collection = Get(instance);

				if (collection == null)
				{
					collection = ValueType.GetConstructor(new Type[] {}).Invoke(new object[] {});

					Set(instance, collection);
				}

				d_collectionAddMethod.Invoke(collection, new object[] {val});
			}
		}

		internal class InfoField : Info
		{
			FieldInfo d_info;

			public InfoField(OptionAttribute option, FieldInfo info) : base(option)
			{
				d_info = info;
			}

			public override object Get(object instance)
			{
				return d_info.GetValue(instance);
			}

			public override void Set(object instance, object val)
			{
				if (IsCollection)
				{
					if (val.GetType().IsSubclassOf(CollectionType) || val.GetType() == CollectionType)
					{
						AddCollection(instance, val);
					}
					else
					{
						d_info.SetValue(instance, val);
					}
				}
				else
				{
					d_info.SetValue(instance, Convert(val));
				}
			}

			public override Type ValueType
			{
				get
				{
					return d_info.FieldType;
				}
			}
		}

		internal class InfoVariable : Info
		{
			PropertyInfo d_info;

			public InfoVariable(OptionAttribute option, PropertyInfo info) : base(option)
			{
				d_info = info;
			}

			public override object Get(object instance)
			{
				if (d_info.CanRead)
				{
					return d_info.GetValue(instance, null);
				}
				else
				{
					return null;
				}
			}

			public override void Set(object instance, object val)
			{
				if (IsCollection && (val as ICollection<string>) == null)
				{
					AddCollection(instance, Convert(val));
				}
				else
				{
					d_info.SetValue(instance, Convert(val), null);
				}
			}

			public override Type ValueType
			{
				get
				{
					return d_info.PropertyType;
				}
			}
		}

		private string d_name;

		private Dictionary<string, Info> d_longnames;
		private Dictionary<string, Info> d_shortnames;
		private List<Info> d_options;

		public OptionGroup() : this(null)
		{
		}

		public OptionGroup(string name)
		{
			d_name = name;

			d_options = new List<Info>();
			d_shortnames = new Dictionary<string, Info>();
			d_longnames = new Dictionary<string, Info>();

			Scan();
		}

		private void Add(Info info)
		{
			d_options.Add(info);

			if (!String.IsNullOrEmpty(info.Option.ShortName))
			{
				d_shortnames[info.Option.ShortName] = info;
			}

			if (!String.IsNullOrEmpty(info.Option.LongName))
			{
				d_longnames[info.Option.LongName] = info;
			}
		}

		private void Scan()
		{
			foreach (FieldInfo info in GetType().GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				object[] attrs = info.GetCustomAttributes(typeof(OptionAttribute), true);

				if (attrs == null || attrs.Length == 0)
				{
					continue;
				}

				OptionAttribute attr = (OptionAttribute)attrs[0];
				Add(new InfoField(attr, info));
			}

			foreach (PropertyInfo info in GetType().GetProperties(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
			{
				object[] attrs = info.GetCustomAttributes(typeof(OptionAttribute), true);

				if (attrs == null || attrs.Length == 0)
				{
					continue;
				}

				OptionAttribute attr = (OptionAttribute)attrs[0];
				Add(new InfoVariable(attr, info));
			}
		}

		public string Name
		{
			get
			{
				return d_name;
			}
		}

		internal Info ShortOption(string name)
		{
			Info ret = null;

			d_shortnames.TryGetValue(name, out ret);
			return ret;
		}

		internal Info LongOption(string name)
		{
			Info ret = null;

			d_longnames.TryGetValue(name, out ret);
			return ret;
		}

		internal int OptionsCount
		{
			get
			{
				return d_options.Count;
			}
		}

		internal bool LongName(string name, out Info info)
		{
			return d_longnames.TryGetValue(name, out info);
		}

		internal bool ShortName(string name, out Info info)
		{
			return d_shortnames.TryGetValue(name, out info);
		}

		public virtual void ShowHelp(TextWriter writer)
		{
			if (d_options.Count == 0)
			{
				return;
			}

			writer.WriteLine("{0} Options:", d_name != null ? d_name : "Help");

			List<string> d_optionstrs = new List<string>();
			List<string> d_optiondescs = new List<string>();

			int maxname = 0;

			foreach (Info info in d_options)
			{
				OptionAttribute opt = info.Option;
				string name;

				if (!String.IsNullOrEmpty(opt.LongName) && !String.IsNullOrEmpty(opt.ShortName))
				{
					name = String.Format("-{0}, --{1}", opt.ShortName, opt.LongName);
				}
				else if (!String.IsNullOrEmpty(opt.LongName))
				{
					name = String.Format("    --{0}", opt.LongName);
				}
				else
				{
					name = String.Format("-{0}", opt.ShortName);
				}

				if (info.ValueType != typeof(bool))
				{
					if (info.Option.OptionalArgument)
					{
						name = String.Format("{0}[={1}]", name, opt.ArgumentName);
					}
					else
					{
						name = String.Format("{0}={1}", name, opt.ArgumentName);
					}
				}

				maxname = System.Math.Max(maxname, name.Length);

				d_optionstrs.Add(name);
				d_optiondescs.Add(opt.Description);
			}

			for (int i = 0; i < d_optionstrs.Count; ++i)
			{
				object val = d_options[i].Get(this);
				string def = "";

				if (val != null)
				{
					def = String.Format(" (default: {0})", val.ToString());
				}

				writer.WriteLine("  {0}    {1}{2}", d_optionstrs[i].PadRight(maxname), d_optiondescs[i], def);
			}

			writer.WriteLine();
		}

		internal IEnumerable<Info> Infos
		{
			get
			{
				return d_options;
			}
		}
	}
}

