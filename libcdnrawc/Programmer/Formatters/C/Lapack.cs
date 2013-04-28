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
		
		[DllImport("liblapack.dll")]
		private static extern void dgetri_(int[] n, double[] A, int[] lda, int[] ipiv, double[] work, int[] lwork, int[] info);
	}
}

