using System;

namespace Cdn.RawC.Programmer.Computation
{
	public class CopyTable : INode
	{
		private DataTable d_source;
		private DataTable d_target;
		private int d_sourceIndex;
		private int d_targetIndex;
		private int d_size;

		public CopyTable(DataTable source, DataTable target, int size) : this(source, target, 0, 0, size)
		{
		}

		public CopyTable(DataTable source, DataTable target, int sourceIndex, int targetIndex, int size)
		{
			d_source = source;
			d_target = target;

			d_sourceIndex = sourceIndex;
			d_targetIndex = targetIndex;

			d_size = size;
		}

		public DataTable Source
		{
			get
			{
				return d_source;
			}
		}

		public int SourceIndex
		{
			get
			{
				return d_sourceIndex;
			}
		}

		public DataTable Target
		{
			get
			{
				return d_target;
			}
		}

		public int TargetIndex
		{
			get
			{
				return d_targetIndex;
			}
		}

		public int Size
		{
			get
			{
				return d_size;
			}
		}
	}
}

