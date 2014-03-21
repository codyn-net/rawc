using System;
using System.Collections.Generic;
using System.IO;

namespace Cdn.RawC
{
	public class Profile
	{
		public class Tag
		{
			string d_name;
			DateTime d_started;
			DateTime d_ended;
			TimeSpan d_duration;
			List<Tag> d_subtags;
			bool d_frozen;

			public Tag(string name) : this(name, true)
			{
			}

			public Tag(string name, bool started)
			{
				d_name = name;
				d_started = DateTime.Now;
				d_ended = DateTime.Now;
				d_duration = d_ended - d_started;
				d_frozen = !started;
				d_subtags = new List<Tag>();
			}

			public void End()
			{
				Freeze();
				Profile.Ended(this);
			}

			public void Freeze()
			{
				if (d_frozen)
				{
					return;
				}

				d_ended = DateTime.Now;
				d_duration = d_duration.Add(d_ended - d_started);

				d_started = d_ended;
				d_frozen = true;
			}

			public void Thaw()
			{
				if (!d_frozen)
				{
					return;
				}

				d_started = DateTime.Now;
				d_ended = DateTime.Now;

				d_frozen = false;
			}

			public TimeSpan Duration
			{
				get
				{
					if (d_frozen)
					{
						return d_duration;
					}
					else
					{
						return d_duration.Add(DateTime.Now - d_started);
					}
				}
			}

			public List<Tag> SubTags
			{
				get { return d_subtags; }
			}

			public void Report(TextWriter writer, int indent, TimeSpan parentDuration)
			{
				writer.Write(new String(' ', indent));

				writer.WriteLine("{0}: {1} ({2}%)",
				                 d_name,
				                 d_duration.TotalSeconds,
				                 (int)System.Math.Round((d_duration.TotalMilliseconds / parentDuration.TotalMilliseconds) * 100));

				foreach (var s in d_subtags)
				{
					s.Report(writer, indent + 2, d_duration);
				}
			}
		}

		private static List<Tag> d_tags;
		private static List<Tag> d_profiled;
		private static DateTime d_started;

		static Profile()
		{
			d_tags = new List<Tag>();
			d_profiled = new List<Tag>();
			d_started = DateTime.Now;
		}

		public static void Initialize()
		{
		}

		public delegate void DoFunc();

		public static void Do(string name, DoFunc d)
		{
			var t = Begin(name);
			d();
			t.End();
		}

		public static Tag Begin(string name)
		{
			return Begin(name, true);
		}

		public static Tag Begin(string name, bool started)
		{
			var ret = new Tag(name, started);

			if (d_tags.Count != 0)
			{
				d_tags[d_tags.Count - 1].SubTags.Add(ret);
			}

			d_tags.Add(ret);
			return ret;
		}

		public static void Ended(Tag t)
		{
			d_tags.Remove(t);

			if (d_tags.Count == 0)
			{
				d_profiled.Add(t);
			}
		}

		public static void Report(TextWriter writer)
		{
			if (!Options.EnableProfile)
			{
				return;
			}

			var duration = DateTime.Now - d_started;

			while (d_tags.Count > 0)
			{
				d_tags[d_tags.Count - 1].End();
			}

			writer.WriteLine("\nProfiling report:");
			writer.WriteLine("=================");
			writer.WriteLine();
			writer.WriteLine("Total time: {0}", duration.TotalSeconds);
			writer.WriteLine();

			foreach (var t in d_profiled)
			{
				t.Report(writer, 0, duration);
			}

			if (d_profiled.Count > 0)
			{
				writer.WriteLine();
			}
		}
	}
}

