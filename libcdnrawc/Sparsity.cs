using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class Sparsity : DynamicVisitor
	{
		private struct SparsityInfo
		{
			public Dimension Dimension;
			public int[] Sparsity;
		}

		private Dictionary<State, SparsityInfo> d_stateSparsity;

		public Sparsity() : base(typeof(SparsityInfo),
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
			while (true)
			{
				foreach (var s in Knowledge.Instance.States)
				{
					CalculateSparsity(s);
				}

				var newmap = new Dictionary<State, SparsityInfo>();

				foreach (var s in Knowledge.Instance.Integrated)
				{
					var info = d_stateSparsity[s];

					var d = Knowledge.Instance.DerivativeState(s);
					var dinfo = d_stateSparsity[d];

					// Check if the derivative state would reduce sparsity
					var inter = IntersectSparsity(info.Sparsity, dinfo.Sparsity);

					if (inter.Length != info.Sparsity.Length)
					{
						// Pin sparsity of the state and recalculate
						newmap[s] = new SparsityInfo() {
							Sparsity = inter,
							Dimension = info.Dimension
						};
					}
				}

				if (newmap.Count == 0)
				{
					break;
				}

				d_stateSparsity = newmap;
			}

			// Here we successfully have the full sparsity calculated.
			// Now do instruction replacement.
		}

		private SparsityInfo CalculateSparsity(State state)
		{
			SparsityInfo ret;

			if (d_stateSparsity.TryGetValue(state, out ret))
			{
				return ret;
			}

			return CalculateSparsity(state.Instructions);
		}

		private SparsityInfo CalculateSparsity(Instruction[] instructions)
		{
			return CalculateSparsity(instructions, null);
		}

		private Instruction ReplaceSparse(Instruction i, int[] sparsity, SparsityInfo[] children)
		{
			var f = i as InstructionFunction;

			if (f == null)
			{
				return null;
			}

			if (sparsity.Length == 0)
			{
				return null;
			}

			int size = 0;
			int sp = 0;
			int maxs = 0;

			for (int ii = 0; ii < children.Length; ii++)
			{
				size += children[ii].Dimension.Size();
				sp += children[ii].Sparsity.Length;

				var ms = children[ii].Dimension.Size() - children[ii].Sparsity.Length;

				if (ms > maxs)
				{
					maxs = ms;
				}
			}

			if (maxs > 100)
			{
				// Too many calculations to special case
				return null;
			}

			if ((double)size / (double)sp < 0.1)
			{
				// Not sparse enough
				return null;
			}

			var retsparse = new Cdn.RawC.Programmer.Instructions.Sparsity(sparsity);
			var argsparse = new Cdn.RawC.Programmer.Instructions.Sparsity[children.Length];

			for (int ii = 0; ii < argsparse.Length; ii++)
			{
				argsparse[ii] = new Cdn.RawC.Programmer.Instructions.Sparsity(children[ii].Sparsity);
			}

			return new Cdn.RawC.Programmer.Instructions.SparseOperator(f, retsparse, argsparse);
		}

		private void MakeSparse(State s)
		{
			var instrs = new Queue<SparsityInfo>();
			var newinst = new List<Instruction>();
			bool didrepl = false;

			foreach (var i in s.Instructions)
			{
				var smanip = i.GetStackManipulation();
				var num = smanip.Pop.Num;
				var children = new SparsityInfo[num];

				for (int n = 0; n < num; n++)
				{
					children[n] = instrs.Dequeue();
				}

				var sp = Invoke<int[]>(i, children, null);

				var newi = ReplaceSparse(i, sp, children);

				if (newi != null)
				{
					didrepl = true;
					newinst.Add(newi);
				}
				else if (newinst != null)
				{
					newinst.Add(i);
				}

				instrs.Enqueue(new SparsityInfo() {
					Dimension = smanip.Push.Dimension,
					Sparsity = sp
				});
			}

			if (didrepl)
			{
				s.Instructions = newinst.ToArray();
			}
		}

		private SparsityInfo CalculateSparsity(Instruction[] instructions, Dictionary<Variable, SparsityInfo> varmapping)
		{
			var instrs = new Queue<SparsityInfo>();

			foreach (var i in instructions)
			{
				var smanip = i.GetStackManipulation();
				var num = smanip.Pop.Num;
				var children = new SparsityInfo[num];

				for (int n = 0; n < num; n++)
				{
					children[n] = instrs.Dequeue();
				}

				var sp = Invoke<int[]>(i, children, varmapping);

				instrs.Enqueue(new SparsityInfo() {
					Dimension = smanip.Push.Dimension,
					Sparsity = sp
				});
			}

			return instrs.Dequeue();
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

			if (mapping.TryGetValue(v, out info))
			{
				return info.Sparsity;
			}

			if (v.HasFlag(VariableFlags.In))
			{
				return new int[0];
			}

			var st = Knowledge.Instance.State(v);
			return CalculateSparsity(st).Sparsity;
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

		private int[] DiagSparsity(SparsityInfo[] children)
		{
			var ret = new List<int>();

			if (children[0].Dimension.Rows == 1 || children[0].Dimension.Columns == 1)
			{
				int n = children[0].Dimension.Size();
				int i = 0;

				for (int c = 0; c < n; c++)
				{
					for (int r = 0; r < n; r++)
					{
						if (c == r)
						{
							if (children[c].Sparsity.Length != 0)
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
				int n = children[0].Dimension.Rows;
				int i = 0;

				for (int d = 0; d < n; d++)
				{
					if (SparsityContains(children[0].Sparsity, i))
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
						CopySparsity(r.Sparsity);
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
						CopySparsity(l.Sparsity);
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

		private bool[] ExpandSparsity(SparsityInfo info)
		{
			var ret = new bool[info.Dimension.Size()];

			for (var i = 0; i < info.Sparsity.Length; i++)
			{
				ret[i] = true;
			}

			return ret;
		}

		private int[] MultiplySparsity(SparsityInfo[] children)
		{
			var l = children[0];
			var r = children[1];

			if (!(l.Dimension.Columns == r.Dimension.Rows && !l.Dimension.IsOne))
			{
				return UnionSparsity(children);
			}

			// Compute matrix multiply sparsity
			var s1 = ExpandSparsity(l);
			var s2 = ExpandSparsity(r);

			var ret = new List<int>();
			int i = 0;

			for (int ci = 0; ci < r.Dimension.Columns; ci++)
			{
				for (int ri = 0; ri < l.Dimension.Rows; ri++)
				{
					bool issparse = true;

					for (int k = 0; k < r.Dimension.Rows; k++)
					{
						var p1 = s1[ri + k * l.Dimension.Columns];
						var p2 = s2[k + ci * r.Dimension.Rows];

						if (!(p1 || p2))
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

		private int[] CSumSparsity(SparsityInfo[] children)
		{
			var c = children[0];
			var ret = new List<int>();

			var s = ExpandSparsity(c);

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

			var s = ExpandSparsity(c);
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
				return IntersectSparsity(children);
			case MathFunctionType.Multiply:
				return MultiplySparsity(children);
			case MathFunctionType.Csum:
				return CSumSparsity(children);
			case MathFunctionType.Rsum:
				return RSumSparsity(children);
			default:
				break;
			}

			return new int[0];
		}

		private int[] InstructionSparsity(InstructionCustomFunction instr, SparsityInfo[] children, Dictionary<Variable, SparsityInfo> mapping)
		{
			// Get the sparsity of the result of evaluating the custom function expression
			// under it's variables having the sparsity specified by children
			var vmap = new Dictionary<Variable, SparsityInfo>();

			int i = 0;

			foreach (var arg in instr.Function.Arguments)
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

			return CalculateSparsity(instr.Function.Expression.Instructions, vmap).Sparsity;
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
	}
}

