using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC
{
	public struct SparsityInfo
	{
		public Dimension Dimension;
		public int[] Sparsity;

		public bool[] Expand()
		{
			var ret = new bool[Dimension.Size()];

			for (var i = 0; i < Sparsity.Length; i++)
			{
				ret[Sparsity[i]] = true;
			}

			return ret;
		}

		public string LogicalString
		{
			get
			{
				var sb = new StringBuilder();
				sb.Append("  [ ");
	
				var ex = Expand();
	
				for (int i = 0; i < ex.Length; i++)
				{
					int ci = i % Dimension.Columns;
					int ri = i / Dimension.Columns;
	
					if (i != 0 && i % Dimension.Columns == 0)
					{
						sb.AppendLine();
						sb.Append("    ");
					}
					else if (i != 0)
					{
						sb.Append(", ");
					}
	
					sb.AppendFormat("{0}", ex[ri + ci * Dimension.Rows] ? 0 : 1);
				}
	
				sb.AppendLine(" ]");
				return sb.ToString();
			}
		}

		public override string ToString()
		{
			return String.Format("S[{0}]", String.Join(", ", Array.ConvertAll(Sparsity, (a) => a.ToString())));
		}

		public SparsityInfo Inverse()
		{
			var ret = new int[Dimension.Size() - Sparsity.Length];
			var si = 0;
			var ri = 0;

			for (var i = 0; i < Dimension.Size(); i++)
			{
				if (si < Sparsity.Length && Sparsity[si] == i)
				{
					si++;
				}
				else
				{
					ret[ri] = i;
					ri++;
				}
			}

			return new SparsityInfo() {
				Dimension = Dimension,
				Sparsity = ret
			};
		}
	}

	public class Sparsity : DynamicVisitor
	{
		private Dictionary<State, SparsityInfo> d_stateSparsity;

		public Sparsity() : base(typeof(int[]),
			BindingFlags.Default,
			System.Reflection.BindingFlags.Default |
			System.Reflection.BindingFlags.NonPublic |
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.Instance |
			System.Reflection.BindingFlags.InvokeMethod,
			a => a.Name == "InstructionSparsity",
			typeof(Instruction),
			typeof(SparsityInfo[]),
			typeof(Dictionary<Variable, SparsityInfo>))
		{
			d_stateSparsity = new Dictionary<State, SparsityInfo>();
		}

		public void Optimize()
		{
			foreach (var s in Knowledge.Instance.States)
			{
				MakeSparse(s);
			}

			foreach (var s in Knowledge.Instance.InitializeStates)
			{
				MakeSparse(s);
			}

			if (Options.Instance.PrintSparsity != null)
			{
				foreach (var v in Knowledge.Instance.Network.FindVariables(Options.Instance.PrintSparsity))
				{
					var s = Knowledge.Instance.State(v);

					if (s != null)
					{
						PrintSparsity(s, v);
					}
					else
					{
						Console.Error.WriteLine("Could not find state to print sparsity for variable `{0}'", v.FullNameForDisplay);
					}
				}

				Environment.Exit(1);
			}
		}

		private void PrintSparsity(State s, Cdn.Variable v)
		{
			SparsityInfo info;

			if (!d_stateSparsity.TryGetValue(s, out info))
			{
				Console.Error.WriteLine("Did not compute sparsity for state `{0}'", v.FullNameForDisplay);
				return;
			}

			Console.WriteLine("{0}:", v.FullNameForDisplay);
			Console.Write(info.LogicalString);
		}

		private SparsityInfo CalculateSparsity(Instruction[] instructions)
		{
			return CalculateSparsity(instructions, null);
		}

		private bool CanSparse(Cdn.MathFunctionType type)
		{
			switch (type)
			{
			case MathFunctionType.Divide:
			case MathFunctionType.Pow:
			case MathFunctionType.Power:
			case MathFunctionType.Emultiply:
			case MathFunctionType.Product:
			case MathFunctionType.Minus:
			case MathFunctionType.Plus:
			case MathFunctionType.Sqsum:
			case MathFunctionType.Sum:
			case MathFunctionType.Multiply:
			case MathFunctionType.Csum:
			case MathFunctionType.Rsum:
			case MathFunctionType.UnaryMinus:
			case MathFunctionType.PseudoInverse:
				return true;
			}

			return false;
		}

		private Instruction ReplaceSparse(Instruction i, SparsityInfo sparsity, SparsityInfo[] children)
		{
			var f = i as InstructionFunction;

			if (f == null || !CanSparse((Cdn.MathFunctionType)f.Id))
			{
				return null;
			}

			int size = 0;
			int sp = 0;
			int maxs = 0;
			bool ismultidim = false;

			for (int ii = 0; ii < children.Length; ii++)
			{
				size += children[ii].Dimension.Size();
				sp += children[ii].Sparsity.Length;

				if (!children[ii].Dimension.IsOne)
				{
					ismultidim = true;
				}

				var ms = children[ii].Dimension.Size() - children[ii].Sparsity.Length;

				if (ms > maxs)
				{
					maxs = ms;
				}
			}

			if (!ismultidim)
			{
				return null;
			}

			if (maxs > 100)
			{
				// Too many calculations to special case
				return null;
			}

			if (sp == 0 || (double)size / (double)sp < 0.1)
			{
				// Not sparse enough
				return null;
			}

			return new Cdn.RawC.Programmer.Instructions.SparseOperator(f, sparsity, children);
		}

		private SparsityInfo MakeSparse(State s)
		{
			SparsityInfo info;

			if (d_stateSparsity.TryGetValue(s, out info))
			{
				return info;
			}

			var instrs = new Stack<SparsityInfo>();
			var newinst = new List<Instruction>();
			bool didrepl = false;

			foreach (var i in s.Instructions)
			{
				var smanip = i.GetStackManipulation();

				var num = smanip.Pop.Num;
				var children = new SparsityInfo[num];

				for (int n = 0; n < num; n++)
				{
					children[num - n - 1] = instrs.Pop();
				}

				var sp = Invoke<int[]>(i, children, null);

				var spi = new SparsityInfo() {
					Dimension = smanip.Push.Dimension,
					Sparsity = sp
				};

				var newi = ReplaceSparse(i, spi, children);

				if (newi != null)
				{
					didrepl = true;
					newinst.Add(newi);
				}
				else if (newinst != null)
				{
					newinst.Add(i);
				}

				instrs.Push(spi);
			}

			if (didrepl)
			{
				s.Instructions = newinst.ToArray();
			}

			info = instrs.Pop();
			d_stateSparsity[s] = info;

			return info;
		}

		private SparsityInfo CalculateSparsity(Instruction[] instructions, Dictionary<Variable, SparsityInfo> varmapping)
		{
			var instrs = new Stack<SparsityInfo>();

			foreach (var i in instructions)
			{
				var smanip = i.GetStackManipulation();
				var num = smanip.Pop.Num;
				var children = new SparsityInfo[num];

				for (int n = 0; n < num; n++)
				{
					children[num - n - 1] = instrs.Pop();
				}

				var sp = Invoke<int[]>(i, children, varmapping);

				var info = new SparsityInfo() {
					Dimension = smanip.Push.Dimension,
					Sparsity = sp
				};

				instrs.Push(info);
			}

			return instrs.Pop();
		}

		private int[] InstructionSparsity(InstructionMatrix instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			// The sparsity of the matrix is simply the combined sparsity
			// of its children
			var sp = new List<int>();
			int offset = 0;

			foreach (var child in children)
			{
				foreach (var i in child.Sparsity)
				{
					sp.Add(i + offset);
				}

				offset += child.Dimension.Size();
			}

			return sp.ToArray();
		}

		private int[] VariableSparsity(Variable v, Dictionary<Variable, SparsityInfo> mapping)
		{
			SparsityInfo info;

			if (mapping != null && mapping.TryGetValue(v, out info))
			{
				return info.Sparsity;
			}

			if (v.HasFlag(VariableFlags.In))
			{
				return new int[0];
			}

			var st = Knowledge.Instance.State(v);

			// Integrated states are like ins, they can never be assumed
			// to be sparse
			if ((st.Type & State.Flags.Integrated) != 0)
			{
				return new int[0];
			}

			return MakeSparse(st).Sparsity;
		}

		private int[] InstructionSparsity(InstructionVariable instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			var ret = VariableSparsity(instr.Variable, mapping);

			if (instr.HasSlice)
			{
				// Slice the sparsity also
				Dimension slicedim;
				var slice = instr.GetSlice(out slicedim);

				return SparsitySlice(ret, slice);
			}

			return ret;
		}

		private bool SparsityContains(int[] sparsity, int i)
		{
			var f = Array.BinarySearch(sparsity, i);

			return (f >= 0 && f < sparsity.Length && sparsity[f] == i);
		}

		private int[] SparsitySlice(int[] sparsity, int[] slice)
		{
			var ret = new List<int>();

			for (var i = 0; i < slice.Length; i++)
			{
				if (SparsityContains(sparsity, slice[i]))
				{
					ret.Add(i);
				}
			}

			return ret.ToArray();
		}

		private int[] CopySparsity(int[] sparsity)
		{
			int[] ret = new int[sparsity.Length];
			Array.Copy(sparsity, ret, sparsity.Length);

			return ret;
		}

		private int[] MakeDiagSparsity(int n)
		{
			var ret = new int[n * n - n];
			int i = 0;

			for (int ci = 0; ci < n; ci++)
			{
				for (int ri = 0; ri < n; ri++)
				{
					if (ci != ri)
					{
						ret[i] = ri + ci * n;
						i++;
					}
				}
			}

			return ret;
		}

		private int[] DiagSparsity(SparsityInfo[] children)
		{
			var ret = new List<int>();
			var c = children[0];

			if (c.Dimension.Rows == 1 || c.Dimension.Columns == 1)
			{
				int n = c.Dimension.Size();
				int i = 0;

				for (int ci = 0; ci < n; ci++)
				{
					for (int ri = 0; ri < n; ri++)
					{
						if (ci == ri)
						{
							if (SparsityContains(c.Sparsity, ci * n + ci))
							{
								ret.Add(i);
							}
						}
						else
						{
							ret.Add(i);
						}

						i++;
					}
				}
			}
			else
			{
				int n = c.Dimension.Rows;
				int i = 0;

				for (int d = 0; d < n; d++)
				{
					if (SparsityContains(c.Sparsity, i))
					{
						ret.Add(d);
					}

					i += n;
				}
			}

			return ret.ToArray();
		}

		private int[] FullSparsity(int n)
		{
			int[] ret = new int[n];

			for (int i = 0; i < ret.Length; i++)
			{
				ret[i] = i;
			}

			return ret;
		}

		private int[] DividePowerSparsity(SparsityInfo[] children)
		{
			var c = children[0];

			if (c.Dimension.IsOne && c.Sparsity.Length != 0)
			{
				return FullSparsity(children[1].Dimension.Size());
			}
			else if (!c.Dimension.IsOne)
			{
				return CopySparsity(c.Sparsity);
			}

			return new int[0];
		}

		private int[] IntersectSparsity(int[] s1, int[] s2)
		{
			var ret = new List<int>();
			var i1 = 0;
			var i2 = 0;

			while (i1 < s1.Length && i2 < s2.Length)
			{
				if (s1[i1] == s2[i2])
				{
					ret.Add(s1[i1]);
					i1++;
					i2++;
				}
				else if (s1[i1] < s2[i2])
				{
					i1++;
				}
				else
				{
					i2++;
				}
			}

			return ret.ToArray();
		}

		private int[] IntersectSparsity(SparsityInfo[] children)
		{
			if (children.Length == 1)
			{
				return CopySparsity(children[0].Sparsity);
			}

			if (children.Length == 2)
			{
				var l = children[0];
				var r = children[1];

				if (l.Dimension.IsOne && l.Sparsity.Length > 0)
				{
					return CopySparsity(r.Sparsity);
				}
				else if (r.Dimension.IsOne && r.Sparsity.Length > 0)
				{
					return CopySparsity(l.Sparsity);
				}
				else if (!l.Dimension.IsOne && !r.Dimension.IsOne)
				{
					return IntersectSparsity(l.Sparsity, r.Sparsity);
				}
			}
			else
			{
				bool issparse = true;

				for (int i = 0; i < children.Length; i++)
				{
					if (children[i].Sparsity.Length == 0)
					{
						issparse = false;
						break;
					}
				}

				if (issparse)
				{
					return new int[] { 0 };
				}
			}

			return new int[0];
		}

		private int[] UnionSparsity(int[] s1, int[] s2)
		{
			int i1 = 0;
			int i2 = 0;
			var ret = new List<int>();

			while (i1 < s1.Length && i2 < s2.Length)
			{
				if (s1[i1] == s2[i2])
				{
					ret.Add(s1[i1]);

					i1++;
					i2++;
				}
				else if (s1[i1] < s2[i2])
				{
					ret.Add(s1[i1]);
					i1++;
				}
				else
				{
					ret.Add(s2[i2]);
					i2++;
				}
			}

			while (i1 < s1.Length)
			{
				ret.Add(s1[i1]);
				i1++;
			}

			while (i2 < s2.Length)
			{
				ret.Add(s2[i2]);
				i2++;
			}

			return ret.ToArray();
		}

		private int[] UnionSparsity(SparsityInfo[] children)
		{
			if (children.Length == 1)
			{
				if (children[0].Sparsity.Length > 0)
				{
					return new int[] { 0 };
				}
			}
			else if (children.Length == 2)
			{
				var l = children[0];
				var r = children[1];

				if (l.Dimension.IsOne)
				{
					if (l.Sparsity.Length > 0)
					{
						return FullSparsity(r.Dimension.Size());
					}
					else
					{
						return CopySparsity(r.Sparsity);
					}
				}
				else if (r.Dimension.IsOne)
				{
					if (r.Sparsity.Length > 0)
					{
						return FullSparsity(l.Dimension.Size());
					}
					else
					{
						return CopySparsity(l.Sparsity);
					}
				}
				else
				{
					return UnionSparsity(l.Sparsity, r.Sparsity);
				}
			}
			else
			{
				for (int i = 0; i < children.Length; i++)
				{
					if (children[i].Sparsity.Length > 0)
					{
						return new int[] { 0 };
					}
				}
			}

			return new int[0];
		}

		private int[] MatrixMultiplySparsity(SparsityInfo l, SparsityInfo r)
		{
			// Compute matrix multiply sparsity
			var s1 = l.Expand();
			var s2 = r.Expand();

			var ret = new List<int>();
			int i = 0;

			for (int ci = 0; ci < r.Dimension.Columns; ci++)
			{
				for (int ri = 0; ri < l.Dimension.Rows; ri++)
				{
					bool issparse = true;

					for (int k = 0; k < r.Dimension.Rows; k++)
					{
						var p1i = ri + k * l.Dimension.Rows;
						var p1 = s1[p1i];

						var p2i = k + ci * r.Dimension.Rows;
						var p2 = s2[p2i];

						if (!p1 && !p2)
						{
							issparse = false;
							break;
						}
					}

					if (issparse)
					{
						ret.Add(i);
					}

					i++;
				}
			}

			return ret.ToArray();
		}

		private int[] MultiplySparsity(SparsityInfo[] children)
		{
			var l = children[0];
			var r = children[1];

			if (!(l.Dimension.Columns == r.Dimension.Rows && !l.Dimension.IsOne))
			{
				return UnionSparsity(children);
			}

			return MatrixMultiplySparsity(l, r);
		}

		private int[] CSumSparsity(SparsityInfo[] children)
		{
			var c = children[0];
			var ret = new List<int>();

			var s = c.Expand();

			for (int r = 0; r < c.Dimension.Rows; r++)
			{
				bool issparse = true;

				for (int ci = 0; ci < c.Dimension.Columns; ci++)
				{
					int i = ci * c.Dimension.Rows + r;

					if (!s[i])
					{
						issparse = false;
						break;
					}
				}

				if (issparse)
				{
					ret.Add(r);
				}
			}

			return ret.ToArray();
		}

		private int[] RSumSparsity(SparsityInfo[] children)
		{
			var c = children[0];
			var ret = new List<int>();

			var s = c.Expand();
			int i = 0;

			for (int ci = 0; ci < c.Dimension.Columns; ci++)
			{
				bool issparse = true;

				for (int r = 0; r < c.Dimension.Rows; r++)
				{
					if (!s[i + r])
					{
						issparse = false;
						break;
					}
				}

				i += c.Dimension.Rows;

				if (issparse)
				{
					ret.Add(ci);
				}
			}

			return ret.ToArray();
		}

		private int[] TransposeSparsity(SparsityInfo[] children)
		{
			var c = children[0];

			var ret = new int[c.Sparsity.Length];
			var d = c.Dimension;

			for (var i = 0; i < c.Sparsity.Length; i++)
			{
				var ri = c.Sparsity[i] % d.Rows;
				var ci = c.Sparsity[i] / d.Rows;

				ret[i] = ci + ri * d.Columns;
			}

			Array.Sort(ret);
			return ret;
		}

		private int[] VcatSparsity(Dimension d, SparsityInfo[] children)
		{
			var ret = new List<int>();
			int offset = 0;

			foreach (var c in children)
			{
				foreach (var s in c.Sparsity)
				{
					int ri = s % c.Dimension.Rows + offset;
					int ci = s / c.Dimension.Rows;

					ret.Add(ri + ci * d.Rows);
				}

				offset += c.Dimension.Rows;
			}

			ret.Sort();

			return ret.ToArray();
		}

		private int[] MakeTrilSparsity(int[] sparsity, int n)
		{
			return MakeTrilSparsity(sparsity, n, 0);
		}

		private int[] MakeTrilSparsity(int[] sparsity, int n, int offset)
		{
			var ret = new List<int>(sparsity);

			for (int ci = 1 + offset; ci < n; ci++)
			{
				for (int ri = 0; ri < ci; ri++)
				{
					ret.Add(ri + ci * n);
				}
			}

			ret.Sort();
			return ret.ToArray();
		}

		private int[] MakeTriuSparsity(int[] sparsity, int n)
		{
			return MakeTriuSparsity(sparsity, n, 0);
		}

		private int[] MakeTriuSparsity(int[] sparsity, int n, int offset)
		{
			var ret = new List<int>(sparsity);

			for (int ri = 1 + offset; ri < n; ri++)
			{
				for (int ci = 0; ci < ri; ci++)
				{
					ret.Add(ri + ci * n);
				}
			}

			ret.Sort();
			return ret.ToArray();
		}

		private int[] SltdlDinvLinvtSparsity(SparsityInfo[] children)
		{
			var l = MakeTriuSparsity(children[2].Sparsity, children[2].Dimension.Rows);
			var r = children[0];

			return MatrixMultiplySparsity(new SparsityInfo() {
				Dimension = children[2].Dimension,
				Sparsity = l
			}, r);
		}

		private int[] SltdlLinvSparsity(SparsityInfo[] children)
		{
			var l = MakeTrilSparsity(children[2].Sparsity, children[2].Dimension.Rows);
			var r = children[0];

			return MatrixMultiplySparsity(new SparsityInfo() {
				Dimension = children[2].Dimension,
				Sparsity = l
			}, r);
		}

		private int[] SltdlLinvtSparsity(SparsityInfo[] children)
		{
			var l = MakeTriuSparsity(children[2].Sparsity, children[2].Dimension.Rows);
			var r = children[0];

			return MatrixMultiplySparsity(new SparsityInfo() {
				Dimension = children[2].Dimension,
				Sparsity = l
			}, r);
		}

		private int[] SlinsolveSparsity(SparsityInfo[] children)
		{
			var ret = SltdlDinvLinvtSparsity(children);

			return SltdlLinvSparsity(new SparsityInfo[] {
				new SparsityInfo() {
					Dimension = children[0].Dimension,
					Sparsity = ret
				},
				new SparsityInfo() {},
				children[2]
			});
		}

		private int[] PseudoInverseSparsity(SparsityInfo[] children)
		{
			var c = children[0];
			var sp = c.Expand();
			var ret = new List<int>();

			for (int ri = 0; ri < c.Dimension.Rows; ri++)
			{
				bool iszero = true;

				for (int ci = 0; ci < c.Dimension.Columns; ci++)
				{
					if (!sp[ri + ci * c.Dimension.Rows])
					{
						iszero = false;
						break;
					}
				}

				if (iszero)
				{
					// Make whole row sparse
					for (int k = 0; k < c.Dimension.Columns; k++)
					{
						ret.Add(ri * c.Dimension.Columns + k);
					}
				}
			}

			return ret.ToArray();
		}

		private int[] SltdlDinvSparsity(SparsityInfo[] children)
		{
			var diag = MakeDiagSparsity(children[1].Dimension.Rows);

			return MatrixMultiplySparsity(new SparsityInfo() {
				Dimension = children[1].Dimension,
				Sparsity = diag
			}, children[0]);
		}

		private int[] InstructionSparsity(InstructionFunction instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			switch ((Cdn.MathFunctionType)instr.Id)
			{
			case MathFunctionType.Diag:
				return DiagSparsity(children);
			case MathFunctionType.Divide:
			case MathFunctionType.Pow:
			case MathFunctionType.Power:
				return DividePowerSparsity(children);
			case MathFunctionType.Emultiply:
			case MathFunctionType.Product:
				return UnionSparsity(children);
			case MathFunctionType.Minus:
			case MathFunctionType.Plus:
			case MathFunctionType.Sqsum:
			case MathFunctionType.Sum:
			case MathFunctionType.UnaryMinus:
				return IntersectSparsity(children);
			case MathFunctionType.Multiply:
				return MultiplySparsity(children);
			case MathFunctionType.Csum:
				return CSumSparsity(children);
			case MathFunctionType.Rsum:
				return RSumSparsity(children);
			case MathFunctionType.Transpose:
				return TransposeSparsity(children);
			case MathFunctionType.Vcat:
				return VcatSparsity(instr.GetStackManipulation().Push.Dimension, children);
			case MathFunctionType.Sltdl:
				// The LTDL factorization has the same sparsity pattern as its
				// first argument
				return CopySparsity(children[0].Sparsity);
			case MathFunctionType.SltdlDinv:
				return SltdlDinvSparsity(children);
			case MathFunctionType.SltdlDinvLinvt:
				return SltdlDinvLinvtSparsity(children);
			case MathFunctionType.SltdlLinv:
				return SltdlLinvSparsity(children);
			case MathFunctionType.SltdlLinvt:
				return SltdlLinvtSparsity(children);
			case MathFunctionType.Slinsolve:
				return SlinsolveSparsity(children);
			case MathFunctionType.PseudoInverse:
				return PseudoInverseSparsity(children);
			default:
				break;
			}

			return new int[0];
		}

		private int[] FunctionSparsity(Cdn.Function function, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			// Get the sparsity of the result of evaluating the custom function expression
			// under it's variables having the sparsity specified by children
			var vmap = new Dictionary<Variable, SparsityInfo>();

			int i = 0;

			foreach (var arg in function.Arguments)
			{
				var v = arg.Variable;

				if (!arg.Explicit)
				{
					break;
				}

				if (i >= children.Length)
				{
					vmap[v] = CalculateSparsity(arg.DefaultValue.Instructions);
				}
				else
				{
					vmap[v] = children[i];
				}

				i++;
			}

			var ret = CalculateSparsity(function.Expression.Instructions, vmap);

			return ret.Sparsity;
		}

		private int[] InstructionSparsity(InstructionCustomFunction instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			return FunctionSparsity(instr.Function, children, mapping);
		}

		private int[] InstructionSparsity(InstructionNumber instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			// Just check for zero value
			if (System.Math.Abs(instr.Value) <= double.Epsilon)
			{
				return new int[] { 0 };
			}

			return new int[0];
		}

		private int[] InstructionSparsity(Instruction instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			// Nothing is assumed to be sparse by default
			return new int[0];
		}

		private int[] InstructionSparsity(InstructionIndex instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			var idx = instr.Indices;
			var c = children[0];

			var ret = new List<int>();

			// Sample the sparsity of the child using the slice, correct for offset
			for (int i = 0; i < idx.Length; i++)
			{
				if (SparsityContains(c.Sparsity, idx[i]))
				{
					ret.Add(i);
				}
			}

			return ret.ToArray();
		}

		private int[] InstructionSparsity(InstructionCustomOperator instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			var f = instr.Operator.PrimaryFunction;

			if (f != null)
			{
				return FunctionSparsity(f, children, mapping);
			}

			return new int[0];
		}
	}
}

