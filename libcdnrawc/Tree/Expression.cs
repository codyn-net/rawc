using System;
using System.Collections.Generic;
using System.Text;

namespace Cdn.RawC.Tree
{
	public class Expression
	{
		private Cdn.Expression d_expression;
		private uint[] d_hash;
		private static Dictionary<string, uint> s_hashMapping;
		private static uint s_nextMap;
		
		static Expression()
		{
			s_hashMapping = new Dictionary<string, uint>();
			s_nextMap = (uint)MathFunctionType.Num + (uint)MathFunctionType.Num + 1;
		}
		
		private static uint HashMap(string id)
		{
			uint ret;

			if (!s_hashMapping.TryGetValue(id, out ret))
			{
				ret = s_nextMap++;
				s_hashMapping[id] = ret;
			}
			
			return ret;
		}
		
		public static void Reset()
		{
			s_hashMapping.Clear();
		}

		public Expression(Cdn.Expression expression) : this(expression, false)
		{
		}
		
		public Expression(Cdn.Expression expression, bool strict)
		{
			d_expression = expression;
			
			ComputeHash(strict);
		}
		
		private static bool InstructionIs<T>(Instruction inst, out T t)
		{
			if (inst is T)
			{
				t = (T)(object)inst;
				return true;
			}
			else
			{
				t = default(T);
			}
			
			return false;
		}

		public static IEnumerable<uint> InstructionCodes(Instruction inst)
		{
			return InstructionCodes(inst, false);
		}

		public static uint InstructionCode(Instruction inst)
		{
			return InstructionCode(inst, false);
		}

		public static uint InstructionCode(Instruction inst, bool strict)
		{
			foreach (uint i in InstructionCodes(inst, strict))
			{
				return i;
			}

			return 0;
		}
		
		public static IEnumerable<uint> InstructionCodes(Instruction inst, bool strict)
		{
			InstructionFunction ifunc;
			InstructionCustomOperator icusop;
			InstructionCustomFunction icusf;
			InstructionVariable ivar;
			InstructionNumber inum;
			InstructionRand irand;

			if (InstructionIs(inst, out icusf))
			{
				// Generate byte code for this function by name
				yield return HashMap("f_" + icusf.Function.FullId);
			}
			else if (InstructionIs(inst, out icusop))
			{
				if (icusop.Operator is OperatorDelayed && !strict)
				{
					// These are actually part of the state table, so we use
					// a placeholder code here
					yield return PlaceholderCode;
				}
				else
				{
					bool ns = strict || icusop.Operator is OperatorDelayed;

					yield return HashMap("co_" + icusop.Operator.Name);

					Cdn.Function f = icusop.Operator.PrimaryFunction;

					if (f != null && f.Expression != null)
					{
						foreach (Instruction i in f.Expression.Instructions)
						{
							foreach (uint id in InstructionCodes(i, ns))
							{
								yield return id;
							}
						}
					}
					else
					{
						foreach (Cdn.Expression[] exprs in icusop.Operator.AllExpressions())
						{
							foreach (Cdn.Expression e in exprs)
							{
								foreach (Instruction i in e.Instructions)
								{
									foreach (uint id in InstructionCodes(i, ns))
									{
										yield return id;
									}
								}
							}
						}

						foreach (Cdn.Expression[] exprs in icusop.Operator.AllIndices())
						{
							foreach (Cdn.Expression e in exprs)
							{
								foreach (Instruction i in e.Instructions)
								{
									foreach (uint id in InstructionCodes(i, ns))
									{
										yield return id;
									}
								}
							}
						}
					}
				}
			}
			else if (InstructionIs(inst, out ifunc))
			{
				// Functions just store the id
				yield return (uint)ifunc.Id + 1;
			}
			else if (strict)
			{
				if (InstructionIs(inst, out ivar))
				{
					yield return HashMap(String.Format("var_{0}", ivar.Variable.FullName));
				}
				else if (InstructionIs(inst, out inum))
				{
					yield return HashMap(String.Format("num_{0}", inum.Value));
				}
				else if (InstructionIs(inst, out irand))
				{
					yield return HashMap(String.Format("rand_{0}", irand.Handle));
				}
				else
				{
					throw new NotImplementedException(String.Format("Unhandled strict instruction code: {0}", inst.GetType()));
				}
			}
			else if (InstructionIs(inst, out irand))
			{
				var smanip = irand.GetStackManipulation();

				if (smanip != null && smanip.Pop.Num == 0)
				{
					yield return PlaceholderCode;
				}
				else
				{
					yield return HashMap(String.Format("rand_{0}", irand.Handle));
				}
			}
			else
			{
				// Placeholder for numbers and properties
				yield return PlaceholderCode;
			}
		}
		
		private void ComputeHash(bool strict)
		{
			Instruction[] instructions = d_expression.Instructions;

			List<uint> hash = new List<uint>();
			
			// Hash byte codes are layout like this:
			// [MathFunctionNum]: Functions
			// [MathOperatorNum]: Operators
			// [...]:             Custom functions and operators
			// (uintmax):         Variable or number
			foreach (Instruction inst in instructions)
			{
				hash.AddRange(InstructionCodes(inst, strict));
			}
			
			d_hash = hash.ToArray();
		}
		
		public uint[] Hash
		{
			get
			{
				return d_hash;
			}
		}
		
		public bool HashEqual(Expression other)
		{
			if (other == null)
			{
				return false;
			}
			
			if (d_hash.Length != other.Hash.Length)
			{
				return false;
			}
			
			for (int i = 0; i < d_hash.Length; ++i)
			{
				if (d_hash[i] != other.Hash[i])
				{
					return false;
				}
			}
			
			return true;
		}
		
		public Cdn.Expression WrappedObject
		{
			get
			{
				return d_expression;
			}
		}
		
		public static uint PlaceholderCode
		{
			get
			{
				return 0;
			}
		}
		
		public static implicit operator Cdn.Expression(Expression expression)
		{
			return expression.WrappedObject;
		}

		public static Expression Expand(params Cdn.Expression[] expressions)
		{
			return Expand(null, expressions);
		}

		public static Expression Expand(Dictionary<Instruction, Instruction> instmap, params Cdn.Expression[] expressions)
		{
			Cdn.Expression expression = new Cdn.Expression("0");
			List<Instruction> instructions = new List<Instruction>();

			if (expressions.Length == 0)
			{
				expression.Compile(null, null);
				return new Expression(expression);
			}
			
			for (int i = 0; i < expressions.Length; ++i)
			{
				// Concatenate all the expressions together
				Expand(instmap, expressions[i], instructions);
				
				if (i != 0)
				{
					// Add plus operator to add expressions together
					instructions.Add(new InstructionFunction((uint)MathFunctionType.Plus, "+", 2));
				}
			}
			
			expression.SetInstructionsTake(instructions.ToArray());
			return new Expression(expression);
		}
		
		private static void Expand(Dictionary<Instruction, Instruction> instmap, Cdn.Expression expr, List<Instruction> instructions)
		{
			foreach (Instruction inst in expr.Instructions)
			{
				InstructionVariable prop = inst as InstructionVariable;
				
				if (prop != null)
				{
					// See if we need to expand it
					Variable property = prop.Variable;
					
					if (Knowledge.Instance.State(property) == null)
					{
						// Expand the instruction
						Expand(instmap, property.Expression, instructions);
						continue;
					}
				}

				var cp = inst.Copy() as Instruction;

				if (instmap != null)
				{
					instmap.Add(inst, cp);
				}

				instructions.Add(cp);
			}
		}
		
		public string HashString
		{
			get
			{
				string[] ret = Array.ConvertAll<uint, string>(Hash, a => a.ToString());
				
				return String.Join(" ", ret);
			}
		}
	}
}

