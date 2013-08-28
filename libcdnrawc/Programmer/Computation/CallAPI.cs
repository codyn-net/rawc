using System;

namespace Cdn.RawC.Programmer.Computation
{
	public class CallAPI : INode
	{
		private APIFunction d_function;
		private Tree.Node[] d_arguments;

		public CallAPI(APIFunction function, params Tree.Node[] arguments)
		{
			d_function = function;
			d_arguments = arguments;
		}

		public APIFunction Function
		{
			get { return d_function; }
		}

		public Tree.Node[] Arguments
		{
			get { return d_arguments; }
		}
	}
}

