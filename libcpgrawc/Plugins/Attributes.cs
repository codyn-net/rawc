using System;

namespace Cpg.RawC.Plugins.Attributes
{
	[AttributeUsage(AttributeTargets.Field)]
	public class SettingAttribute : Attribute
	{
		public string d_name;
		public object d_default;
		public string d_description;

		public SettingAttribute(string name, object def)
		{
			Name = name;
			Default = def;
		}

		public SettingAttribute(string name) : this(name, null)
		{
		}

		public SettingAttribute() : this("")
		{
		}
		
		public string Name
		{
			get
			{
				return d_name;
			}
			set
			{
				d_name = value;
			}
		}
		
		public string Description
		{
			get
			{
				return d_description;
			}
			set
			{
				d_description = value;
			}
		}
		
		public object Default
		{
			get
			{
				return d_default;
			}
			set
			{
				d_default = value;
			}
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class PluginAttribute : Attribute
	{
		private string d_name;
		private string d_description;
		private string d_author;

		public string Name
		{
			get
			{
				return d_name;
			}
			set
			{
				d_name = value;
			}
		}
		
		public string Description
		{
			get
			{
				return d_description;
			}
			set
			{
				d_description = value;
			}
		}
		
		public string Author
		{
			get
			{
				return d_author;
			}
			set
			{
				d_author = value;
			}
		}
	}
}

