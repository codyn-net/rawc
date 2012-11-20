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
		
		public class Temporary
		{
			public Tree.Node Node;
			public int Size;
			public string Name;
		}

		private Program d_program;
		private Tree.Node d_root;
		private Stack<Item> d_stack;
		private Dictionary<Tree.NodePath, string> d_mapping;
		private Options d_options;
		private List<Temporary> d_tempstorage;
		private Dictionary<Tree.Node, int> d_tempactive;
		private Stack<string> d_ret;
		private static HashSet<string> s_mathdefines;
		private Stack<List<Temporary>> d_tempstack;
		
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
			
			d_tempstorage = new List<Temporary>();
			d_tempactive = new Dictionary<Tree.Node, int>();
			d_tempstack = new Stack<List<Temporary>>();
			
			d_ret = new Stack<string>();
			d_tempstack.Push(new List<Temporary>());
		}

		public void SaveTemporaryStack()
		{
			d_tempstack.Push(new List<Temporary>());
		}

		public void RestoreTemporaryStack()
		{
			var lst = d_tempstack.Pop();

			foreach (var item in lst)
			{
				if (item != null)
				{
					ReleaseTemporary(item.Node);
				}
			}
		}
		
		public string AcquireTemporary(Tree.Node node)
		{
			int size = node.Dimension.Size();

			for (int i = 0; i < d_tempstorage.Count; ++i)
			{
				var tmp = d_tempstorage[i];

				if (tmp.Node != null)
				{
					continue;
				}
				
				if (tmp.Size < size)
				{
					continue;
				}
				
				tmp.Node = node;
				
				d_tempactive[node] = i;
				d_tempstack.Peek().Add(tmp);

				return String.Format("tmp{0}", i);
			}

			var idx = d_tempstorage.Count;
			
			var newtmp = new Temporary {
				Node = node,
				Size = size,
				Name = String.Format("tmp{0}", idx),
			};
			
			d_tempstorage.Add(newtmp);
			d_tempactive[node] = idx;

			d_tempstack.Peek().Add(newtmp);
			return newtmp.Name;
		}
		
		public List<Temporary> TemporaryStorage
		{
			get { return d_tempstorage; }
		}
		
		public static HashSet<string> MathDefines
		{
			get
			{
				if (s_mathdefines == null)
				{
					s_mathdefines = new HashSet<string>();
				}
			
				return s_mathdefines;
			}
		}
		
		public void PushRet(string ret)
		{
			d_ret.Push(ret);
		}
		
		public string PopRet()
		{
			return d_ret.Pop();
		}
		
		public string PeekRet()
		{
			if (d_ret.Count == 0)
			{
				Log.WriteLine("Empty return value stack while processing: {0}", Node);
			}

			return d_ret.Peek();
		}
		
		public void ReleaseTemporary(Tree.Node node)
		{
			int i;
			
			if (d_tempactive.TryGetValue(node, out i))
			{
				d_tempstorage[i].Node = null;
				d_tempactive.Remove(node);
			}
		}
		
		public Context Base()
		{
			var ctx = new Context(d_program, d_options);
			
			ctx.d_ret = new Stack<string>(d_ret);
			ctx.d_tempstorage = d_tempstorage;
			ctx.d_tempactive = d_tempactive;
			
			return ctx;
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
			var smanip = instruction.GetStackManipulation();
			var type = (Cdn.MathFunctionType)instruction.Id;
			
			if (!smanip.Push.Dimension.IsOne)
			{
				return MathFunctionDefineV(type, smanip);
			}
			
			for (int i = 0; i < smanip.Pop.Num; ++i)
			{
				if (!smanip.GetPopn(i).Dimension.IsOne)
				{
					return MathFunctionDefineV(type, smanip);
				}
			}
			
			return MathFunctionDefine(type, (int)smanip.Pop.Num);
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
			case MathFunctionType.Clip:
			case MathFunctionType.Cycle:
			case MathFunctionType.Modulo:
			case MathFunctionType.Erf:
			case MathFunctionType.Sign:
			case MathFunctionType.Csign:
			case MathFunctionType.Sum:
			case MathFunctionType.Product:
				val = String.Format("CDN_MATH_{0}", name.ToUpper());
				break;
			case MathFunctionType.Power:
				val = "CDN_MATH_POW";
				break;
			default:
				throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
			}
			
			return val;
		}
		
		public static string MathFunctionDefineV(Cdn.MathFunctionType type, Cdn.StackManipulation smanip)
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
			case MathFunctionType.Erf:
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
			case MathFunctionType.Clip:
			case MathFunctionType.Cycle:
			case MathFunctionType.Modulo:
			case MathFunctionType.Plus:
			case MathFunctionType.Emultiply:
			case MathFunctionType.Divide:
			case MathFunctionType.Minus:
			case MathFunctionType.Negate:
			case MathFunctionType.Less:
			case MathFunctionType.LessOrEqual:
			case MathFunctionType.GreaterOrEqual:
			case MathFunctionType.Greater:
			case MathFunctionType.Equal:
			case MathFunctionType.Nequal:
			case MathFunctionType.Hcat:
			case MathFunctionType.Csign:
			case MathFunctionType.Sum:
			case MathFunctionType.Product:
			case MathFunctionType.Transpose:
			case MathFunctionType.Sign:
				val = String.Format("CDN_MATH_{0}_V", name.ToUpper());
				break;
			case MathFunctionType.Power:
				val = "CDN_MATH_POW_V";
				break;
			case MathFunctionType.UnaryMinus:
				val = "CDN_MATH_UMINUS_V";
				break;
			case MathFunctionType.Multiply:
			{
				var d1 = smanip.GetPopn(1).Dimension;
				var d2 = smanip.GetPopn(0).Dimension;
				
				if (d1.Rows == d2.Columns && d1.Columns == d2.Rows)
				{
					return "CDN_MATH_MATRIX_MULTIPLY_V";
				}
				else
				{
					val = "CDN_MATH_EMULTIPLY_V";
				}
				break;
			}
			default:
				throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
			}
			
			if (smanip.Pop.Num == 2)
			{
				var n1 = smanip.GetPopn(1).Dimension.IsOne;
				var n2 = smanip.GetPopn(0).Dimension.IsOne;
				
				return String.Format("{0}_{1}_{2}", val, n1 ? "1" : "M", n2 ? "1" : "M");
			}
			else if (smanip.Pop.Num == 3)
			{
				var n1 = smanip.GetPopn(2).Dimension.IsOne;
				var n2 = smanip.GetPopn(1).Dimension.IsOne;
				var n3 = smanip.GetPopn(0).Dimension.IsOne;
				
				return String.Format("{0}_{1}_{2}_{3}", val, n1 ? "1" : "M", n2 ? "1" : "M", n3 ? "1" : "M");
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

		private static int IndentCount(string s)
		{
			int i;

			for (i = 0; i < s.Length; ++i)
			{
				if (s[i] != '\t')
				{
					return i;
				}
			}

			return i;
		}

		public static string Reindent(string s, string indent)
		{
			if (String.IsNullOrEmpty(s))
			{
				return s;
			}

			string[] lines = s.Split('\n');
			int cnt = -1;

			for (int i = 0; i < lines.Length; ++i)
			{
				var line = lines[i];

				if (line.Trim().Length == 0)
				{
					lines[i] = "";
				}
				else
				{
					var c = IndentCount(line);

					if (cnt == -1 || c < cnt)
					{
						cnt = c;
					}
				}
			}

			for (int i = 0; i < lines.Length; ++i)
			{
				var line = lines[i];

				if (line.Length != 0)
				{
					lines[i] = indent + line.Substring(cnt);
				}
			}

			return String.Join("\n", lines);
		}

		public string AddedIndex(DataTable.DataItem item, int added)
		{
			if (added == 0)
			{
				return item.AliasOrIndex;
			}

			if (d_options.SymbolicNames)
			{
				return String.Format("{0} + {1}", item.AliasOrIndex, added);
			}
			else
			{
				return (item.DataIndex + added).ToString();
			}
		}

		public static bool IndicesAreContinuous(IEnumerable<int> indices)
		{
			int last = 0;
			bool first = true;

			foreach (var i in indices)
			{
				if (!first && i != last + 1)
				{
					return false;
				}
				
				first = false;
				last = i;
			}
			
			return true;
		}
	}
}

