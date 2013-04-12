using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC.Programmer.Formatters.CLike
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
		private Stack<List<Temporary>> d_tempstack;
		private static HashSet<string> s_usedMathFunctions;
		
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

		public static HashSet<string> UsedMathFunctions
		{
			get
			{
				if (s_usedMathFunctions == null)
				{
					s_usedMathFunctions = new HashSet<string>();
				}

				return s_usedMathFunctions;
			}
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

		protected virtual Context Clone()
		{
			return new Context(d_program, d_options);
		}

		public Context Base()
		{
			var ctx = Clone();
			
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
			get { return d_stack.Count == 0 ? null : d_stack.Peek().Node; }
		}
		
		public Tree.Node Root
		{
			get { return d_root; }
		}
		
		public State State
		{
			get { return d_stack.Count == 0 ? null : d_stack.Peek().State; }
		}
		
		public Program Program
		{
			get { return d_program; }
		}
		
		public Options Options
		{
			get { return d_options; }
		}
		
		public bool TryMapping(Tree.Node node, out string ret)
		{
			Tree.NodePath path = node.RelPath(d_root);
			return d_mapping.TryGetValue(path, out ret);
		}
		
		public virtual string MathFunction(Tree.Node node)
		{
			Cdn.InstructionFunction instruction = (Cdn.InstructionFunction)node.Instruction;
			var smanip = instruction.GetStackManipulation();
			var type = (Cdn.MathFunctionType)instruction.Id;
			
			if (!smanip.Push.Dimension.IsOne)
			{
				return MathFunctionV(type, node);
			}
			
			for (int i = 0; i < node.Children.Count; ++i)
			{
				if (!node.Children[i].Dimension.IsOne)
				{
					return MathFunctionV(type, node);
				}
			}
			
			return MathFunction(type, (int)smanip.Pop.Num);
		}
		
		public virtual string MathFunction(Cdn.MathFunctionType type, int arguments)
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
				val = name.ToLower();
				break;
			case MathFunctionType.Power:
				val = "pow";
				break;
			default:
				throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
			}
			
			return val;
		}
		
		public virtual string MathFunctionV(Cdn.MathFunctionType type, Tree.Node node)
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
				val = String.Format("{0}_v", name.ToLower());
				break;
			case MathFunctionType.Power:
				val = "pow_v";
				break;
			case MathFunctionType.UnaryMinus:
				val = "uminus_v";
				break;
			case MathFunctionType.Multiply:
			{
				var d1 = node.Children[0].Dimension;
				var d2 = node.Children[1].Dimension;

				if (d1.Rows == d2.Columns && d1.Columns == d2.Rows)
				{
					return "matrix_multiply_v";
				}
				else
				{
					val = "emultiply_v";
				}
				break;
			}
			default:
				throw new NotImplementedException(String.Format("The math function `{0}' is not supported...", name));
			}
			
			if (node.Children.Count == 2)
			{
				var n1 = node.Children[0].Dimension.IsOne;
				var n2 = node.Children[1].Dimension.IsOne;

				return String.Format("{0}_{1}_{2}", val, n1 ? "1" : "m", n2 ? "1" : "m");
			}
			else if (node.Children.Count == 3)
			{
				var n1 = node.Children[0].Dimension.IsOne;
				var n2 = node.Children[1].Dimension.IsOne;
				var n3 = node.Children[2].Dimension.IsOne;

				return String.Format("{0}_{1}_{2}_{3}", val, n1 ? "1" : "m", n2 ? "1" : "m", n3 ? "1" : "m");
			}	
			
			return val;
		}
		
		public Dictionary<Tree.NodePath, string> Mapping
		{
			get { return d_mapping; }
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

		public static string ToAsciiOnly(string t)
		{
			var ascii = BinaryAnalysis.UnidecodeSharp.Unidecoder.Unidecode(t);
			StringBuilder builder = new StringBuilder();

			foreach (char c in ascii)
			{
				if (!char.IsLetterOrDigit(c))
				{
					builder.Append("_");
				}
				else
				{
					builder.Append(c);
				}
			}

			return builder.ToString();
		}

		public virtual string This(string name)
		{
			return name;
		}

		public virtual bool SupportsPointers
		{
			get { return false; }
		}

		public virtual string MemCpy(string dest, string destStart, string source, string sourceStart, string type, int nelem)
		{
			throw new Exception("Memcpy is not available for this format...");
		}

		public virtual string MemZero(string dest, string destStart, string type, int nelem)
		{
			throw new Exception("Memzero is not available for this format...");
		}
	}
}

