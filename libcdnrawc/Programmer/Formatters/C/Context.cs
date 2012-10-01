using System;
using System.Collections.Generic;

namespace Cdn.RawC.Programmer.Formatters.C
{
	public class Context
	{
		private class Item
		{
			public State State;
			public Tree.Node Node;
			
			public Item(State state, Tree.Node node)
			{
				State = state;
				Node = node;
			}
		}

		private Program d_program;
		private Tree.Node d_root;
		private Stack<Item> d_stack;
		private Dictionary<Tree.NodePath, string> d_mapping;
		private Options d_options;
		
		public Context(Program program, Options options) : this(program, options, null, null)
		{
		}

		public Context(Program program, Options options, Tree.Node node, Dictionary<Tree.NodePath, string> mapping)
		{
			d_program = program;
			d_options = options;
			
			d_stack = new Stack<Item>();
			Push(node);
			
			if (mapping != null)
			{
				d_mapping = mapping;
			}
			else
			{
				d_mapping = new Dictionary<Tree.NodePath, string>();
			}
		}
		
		public Context Base()
		{
			return new Context(d_program, d_options);
		}
		
		public Context Push(State state, Tree.Node node)
		{
			if (node == null)
			{
				return this;
			}

			d_stack.Push(new Item(state, node));
			
			if (d_root == null)
			{
				d_root = node;
			}
			
			return this;
		}
		
		public Context Push(Tree.Node node)
		{
			return Push(State, node);
		}
		
		public Context Pop()
		{
			d_stack.Pop();
			
			if (d_stack.Count == 0)
			{
				d_root = null;
			}
			
			return this;
		}
		
		public Tree.Node Node
		{
			get
			{
				return d_stack.Count == 0 ? null : d_stack.Peek().Node;
			}
		}
		
		public Tree.Node Root
		{
			get
			{
				return d_root;
			}
		}
		
		public State State
		{
			get
			{
				return d_stack.Count == 0 ? null : d_stack.Peek().State;
			}
		}
		
		public Program Program
		{
			get
			{
				return d_program;
			}
		}
		
		public Options Options
		{
			get
			{
				return d_options;
			}
		}
		
		public bool TryMapping(Tree.Node node, out string ret)
		{
			Tree.NodePath path = node.RelPath(d_root);
			return d_mapping.TryGetValue(path, out ret);
		}
		
		public static string MathFunctionDefine(Cdn.InstructionFunction instruction)
		{
			return MathFunctionDefine((Cdn.MathFunctionType)instruction.Id, (int)instruction.GetStackManipulation().Pop.Num);
		}
		
		public static string MathFunctionDefine(Cdn.MathFunctionType type, int arguments)
		{
			string name = Enum.GetName(typeof(Cdn.MathFunctionType), type);
			string val;

			switch (type)
			{
			case MathFunctionType.Abs:
			case MathFunctionType.Acos:
			case MathFunctionType.Asin:
			case MathFunctionType.Atan:
			case MathFunctionType.Atan2:
			case MathFunctionType.Ceil:
			case MathFunctionType.Cos:
			case MathFunctionType.Cosh:
			case MathFunctionType.Exp:
			case MathFunctionType.Exp2:
			case MathFunctionType.Floor:
			case MathFunctionType.Hypot:
			case MathFunctionType.Invsqrt:
			case MathFunctionType.Lerp:
			case MathFunctionType.Ln:
			case MathFunctionType.Log10:
			case MathFunctionType.Max:
			case MathFunctionType.Min:
			case MathFunctionType.Pow:
			case MathFunctionType.Round:
			case MathFunctionType.Sin:
			case MathFunctionType.Sinh:
			case MathFunctionType.Sqrt:
			case MathFunctionType.Sqsum:
			case MathFunctionType.Tan:
			case MathFunctionType.Tanh:
			case MathFunctionType.Scale:
				val = String.Format("CDN_MATH_{0}", name.ToUpper());
				break;
			case MathFunctionType.Power:
				val = "CDN_MATH_POW";
				break;
			default:
				throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
			}
			
			if (Cdn.Math.FunctionIsVariable(type))
			{
				val = String.Format("{0}{1}", val, arguments);
			}
			
			return val;
		}
		
		public Dictionary<Tree.NodePath, string> Mapping
		{
			get
			{
				return d_mapping;
			}
		}
	}
}

