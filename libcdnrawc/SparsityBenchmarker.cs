using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

namespace Cdn.RawC
{
	public class SparsityBenchmarker
	{
		private int d_maxSize;
		private int d_minSize;

		public SparsityBenchmarker()
		{
			d_minSize = 2;
			d_maxSize = 6;
		}

		public void Generate()
		{
			string output = Options.Instance.Output;

			if (String.IsNullOrEmpty(output))
			{
				output = ".";
			}

			var odir = Path.Combine(output, "rawc_spbench");

			Directory.CreateDirectory(odir);

			var fname = Path.Combine(odir, "spbench.c");

			var writer = File.CreateText(fname);

			writer.WriteLine("#define CDN_MATH_MATRIX_MULTIPLY_V_REQUIRED");
			writer.WriteLine();
			writer.WriteLine("#include <cdn-rawc/cdn-rawc-math.h>");
			writer.WriteLine("#include <sys/time.h>");
			writer.WriteLine("#include <stdio.h>");

			var benches = new List<string>();

			for (int i = d_minSize; i <= d_maxSize; i++)
			{
				benches.AddRange(GenerateMultiply(writer, i));
			}

			writer.WriteLine("static void");
			writer.WriteLine("spbench_run (const char *name, void (*func)(int), int n)");
			writer.WriteLine("{");
			writer.WriteLine("\tstruct timeval start, end;");
			writer.WriteLine("\tuint64_t elapsedus;");
			writer.WriteLine();
			writer.WriteLine("\tgettimeofday(&start, NULL);");
			writer.WriteLine();
			writer.WriteLine("\tfunc(n);");
			writer.WriteLine();
			writer.WriteLine("\tgettimeofday(&end, NULL);");
			writer.WriteLine("\telapsedus = ((uint64_t)end.tv_sec * 1000000 + (uint64_t)end.tv_usec) - ((uint64_t)start.tv_sec * 1000000 + (uint64_t)start.tv_usec);");
			writer.WriteLine("\tprintf (\"%s: %f (%llu.%llu)\\n\", name, elapsedus * 1e-6, elapsedus / 1000000, elapsedus % 1000000);");
			writer.WriteLine("}");
			writer.WriteLine();

			writer.WriteLine("int");
			writer.WriteLine("main(int argc, char **argv)");
			writer.WriteLine("{");
			writer.WriteLine("\tint n = 1e6;");
			writer.WriteLine();

			writer.WriteLine("\tif (argc > 1)");
			writer.WriteLine("\t{");
			writer.WriteLine("\t\tn = atoi(argv[1]);");
			writer.WriteLine("\t}");
			writer.WriteLine();

			foreach (var n in benches)
			{
				writer.WriteLine("\tspbench_run(\"{0}\", spbench_{0}, n);", n);
			}

			writer.WriteLine("\treturn 0;");
			writer.WriteLine("}");

			writer.Flush();
			writer.Close();

			Directory.CreateDirectory(Path.Combine(odir, "cdn-rawc"));

			CopyResource("Cdn.RawC.Resources.SparsityBench.make", Path.Combine(odir, "Makefile"));
			CopyResource("Cdn.RawC.Programmer.Formatters.C.Resources.cdn-rawc-math.h", Path.Combine(odir, "cdn-rawc", "cdn-rawc-math.h"));
			CopyResource("Cdn.RawC.Programmer.Formatters.C.Resources.cdn-rawc-macros.h", Path.Combine(odir, "cdn-rawc", "cdn-rawc-macros.h"));
		}

		private void CopyResource(string name, string outfile)
		{
			Stream res = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
			StreamReader reader = new StreamReader(res);
			string ret = reader.ReadToEnd();

			var writer = File.CreateText(outfile);
			writer.Write(ret);

			writer.Flush();
			writer.Close();
		}

		private delegate void BenchWriter(TextWriter writer);

		private void WriteIndented(TextWriter writer, string s, int n)
		{
			var id = new String('\t', n);

			foreach (var l in s.Split('\n'))
			{
				if (l.Length != 0)
				{
					writer.Write(id);
				}

				writer.WriteLine(l);
			}
		}

		private void WriteBench(TextWriter writer, string name, BenchWriter init, BenchWriter w)
		{
			writer.WriteLine("static void");
			writer.WriteLine("spbench_{0} (int n)", name);
			writer.WriteLine("{");
			writer.WriteLine("\tint i;");

			var wr = new StringWriter();
			init(wr);

			WriteIndented(writer, wr.ToString(), 1);

			writer.WriteLine();

			writer.WriteLine("\tfor (i = 0; i < n; i++)");
			writer.WriteLine("\t{");

			wr = new StringWriter();
			w(wr);

			WriteIndented(writer, wr.ToString(), 2);

			writer.WriteLine("\t}");
			writer.WriteLine("}\n");
		}

		private string GenMatrix(string name, int m, int n, int numsparse)
		{
			var b = new StringBuilder();

			b.AppendFormat("static double {0}[{1}] = {{", name, m * n);
			var rnd = new Random();

			for (int i = 0; i < m * n; i++)
			{
				if (i != 0)
				{
					b.Append(", ");
				}

				var val = rnd.NextDouble();
				b.Append(val.ToString("G"));
			}

			b.Append("};");

			return b.ToString();
		}

		private string[] GenerateMultiply(TextWriter writer, int size)
		{
			var name = String.Format("matrix_multiply_{0}", size);
			var m1 = GenMatrix("m1", size, size, 0);
			var m2 = GenMatrix("m2", size, size, 0);
			var ret = String.Format("volatile double ret[{0}] = {{0,}};", size * size);

			// Test matrix matrix multiplication
			WriteBench(writer, name + "_blas", (TextWriter w) => {
				w.WriteLine(m1);
				w.WriteLine(m2);
				w.WriteLine(ret);
			}, (TextWriter w) => {
				w.WriteLine("CDN_MATH_MATRIX_MULTIPLY_V(ret, m1, m2, {0}, {1}, {2});", size, size, size);
			});

			writer.WriteLine("static double *");
			writer.WriteLine("matrix_multiply_v_{0}(volatile double *ret, volatile double *m1, volatile double *m2)", size);
			writer.WriteLine("{");

			// Manual implementation of matrix matrix multiplication
			for (int c = 0; c < size; c++)
			{
				for (int r = 0; r < size; r++)
				{
					int i = c * size + r;

					writer.Write("\tret[{0}] = ", i);

					for (int k = 0; k < size; k++)
					{
						if (k != 0)
						{
							writer.Write(" + ");
						}

						int m1i = r + k * size;
						int m2i = c * size + k;

						writer.Write("m1[{0}] * m2[{1}]", m1i, m2i);
					}

					writer.WriteLine(";");
				}
			}

			writer.WriteLine();
			writer.WriteLine("\treturn ret;");
			writer.WriteLine("}\n");

			WriteBench(writer, name + "_sparse", (TextWriter w) => {
				w.WriteLine(m1);
				w.WriteLine(m2);
				w.WriteLine(ret);
			}, (TextWriter w) => {
				w.WriteLine("matrix_multiply_v_{0} (ret, m1, m2);", size);
			});

			return new string[] {
				name + "_blas",
				name + "_sparse"
			};
		}
	}
}