using System;
using System.Runtime.InteropServices;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class Lapack
	{
		public static int InverseWorkspace(int n)
		{
			int[] ln = new int[] {n};
			double[] A = new double[n * n];
			int[] lda = new int[] {n};
			int[] ipiv = new int[] {n};
			double[] work = new double[1];
			int[] lwork = new int[] {-1};
			int[] info = new int[1];

			try
			{
				dgetri_(ln, A, lda, ipiv, work, lwork, info);
				return (int)work[0];
			}
			catch
			{
				return n * 64;
			}
		}

		public static int QrWorkspace(Cdn.Dimension d)
		{
			int[] m = new int[] {d.Rows};
			int[] n = new int[] {d.Columns};
			double[] A = new double[d.Size()];
			double[] tau = new double[d.Rows < d.Columns ? d.Rows : d.Columns];
			double[] work = new double[1];
			int[] lwork = new int[] {-1};
			int[] info = new int[1];

			try
			{
				dgeqrf_(m, n, A, m, tau, work, lwork, info);
				return (int)work[0];
			}
			catch
			{
				return d.Columns * 64;
			}
		}

		public static int[] PseudoInverseWorkspace(Cdn.Dimension d)
		{
			int[] m = new int[] {d.Rows};
			int[] n = new int[] {d.Columns};

			var maxdim = System.Math.Max(d.Rows, d.Columns);
			var mindim = System.Math.Max(d.Rows, d.Columns);

			int[] nrhs = new int[] {maxdim};
			double[] A = new double[d.Size()];
			double[] b = new double[maxdim * maxdim];
			double[] s = new double[mindim];
			double[] rcond = new double[] {-1};
			double[] rank = new double[1];
			double[] work = new double[1];
			int[] lwork = new int[] {-1};
			int[] iwork = new int[1];
			int[] info = new int[1];

			int nlvl = (int)System.Math.Log(mindim / (25.0 + 1.0), 2) + 1;
			int riwork = 3 * mindim * nlvl + 11 * mindim;

			try
			{
				dgelsd_(m, n, nrhs, A, m, b, nrhs, s, rcond, rank, work, lwork, iwork, info);
				return new int[] {(int)work[0], riwork};
			}
			catch
			{
				return new int[] {12 * mindim + 2 * mindim * 25 + 8 * mindim * nlvl + mindim * maxdim + (int)System.Math.Pow(25.0 + 1.0, 2), riwork};
			}
		}

		[DllImport("liblapack.dll")]
		private static extern void dgetri_(int[] n, double[] A, int[] lda, int[] ipiv, double[] work, int[] lwork, int[] info);

		[DllImport("liblapack.dll")]
		private static extern void dgelsd_(int[] m,
		                                   int[] n,
		                                   int[] nrhs,
		                                   double[] A,
		                                   int[] lda,
		                                   double[] b,
		                                   int[] ldb,
		                                   double[] s,
		                                   double[] rcond,
		                                   double[] rank,
		                                   double[] work,
		                                   int[] lwork,
		                                   int[] iwork,
		                                   int[] info);

		[DllImport("liblapack.dll")]
		private static extern void dgeqrf_(int[] m,
		                                   int[] n,
		                                   double[] A,
		                                   int[] lda,
		                                   double[] tau,
		                                   double[] work,
		                                   int[] lwork,
		                                   int[] info);
	}
}

