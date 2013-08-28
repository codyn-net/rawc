using System;

namespace Cdn.RawC.Programmer.Computation
{
	public class Comment : INode
	{
		private string d_text;

		public Comment(string text)
		{
			d_text = text;
		}

		public string Text
		{
			get
			{
				return d_text;
			}
		}
	}
}

