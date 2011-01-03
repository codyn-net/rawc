using System;
using System.Collections.Generic;
using System.Text;

namespace Cpg.RawC
{
	public class Expression
	{
		private Cpg.Expression d_expression;
		private byte[] d_hash;

		private static Dictionary<string, byte> s_hashMapping;
		private static byte s_nextMap;
		
		static Expression()
		{
			s_hashMapping = new Dictionary<string, byte>();
			s_nextMap = (byte)MathFunctionType.Num + (byte)MathOperatorType.Num;
		}
		
		private static byte HashMap(string id)
		{
			byte ret;

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
		
		public Expression(Cpg.Expression expression)
		{
			d_expression = expression;
			
			ComputeHash();
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
		
		private void ComputeHash()
		{
			Instruction[] instructions = d_expression.Instructions;

			InstructionFunction ifunc;
			InstructionOperator iop;
			InstructionCustomOperator icusop;
			InstructionCustomFunction icusf;
			
			List<byte> hash = new List<byte>();
			
			// Hash byte codes are layout like this:
			// [MathFunctionNum]: Functions
			// [MathOperatorNum]: Operators
			// [...]:             Custom functions
			// 255 (byte max):    Property or number
			
			foreach (Instruction inst in instructions)
			{
				if (InstructionIs(inst, out icusf))
				{
					// Generate byte code for this function by name
					hash.Add(HashMap("f_" + icusf.Function.Id));
				}
				else if (InstructionIs(inst, out icusop))
				{
					throw new Exception(String.Format("Custom operators are currently not supported: {0}", icusop.Operator.Name));
				}
				else if (InstructionIs(inst, out iop))
				{
					// Operators store the id + number of functions
					hash.Add((byte)(iop.Id + (uint)Cpg.MathFunctionType.Num));
				}
				else if (InstructionIs(inst, out ifunc))
				{
					// Functions just store the id
					hash.Add((byte)ifunc.Id);
				}
				else
				{
					// Placeholder for numbers and properties
					hash.Add(PlaceholderCode);
				}
			}
			
			d_hash = hash.ToArray();
		}
		
		public byte[] Hash
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
		
		public Cpg.Expression WrappedObject
		{
			get
			{
				return d_expression;
			}
		}
		
		private static bool NumericArguments(InstructionFunction ifunc, Stack<Instruction> newinst)
		{
			Stack<Instruction> cp = new Stack<Instruction>(newinst);
						
			for (int i = 0; i < ifunc.Arguments; ++i)
			{
				Instruction inst = cp.Pop();
				
				if (!(inst is InstructionNumber))
				{
					return false;
				}
			}
			
			return true;
		}
		
		public static byte PlaceholderCode
		{
			get
			{
				return byte.MaxValue;
			}
		}
		
		private static void Precompute(InstructionFunction ifunc, Stack<Instruction> newinst)
		{
			int num = ifunc.Arguments;
			Cpg.Stack stack = new Cpg.Stack((uint)num);

			for (int i = 0; i < num; ++i)
			{
				InstructionNumber inum = (InstructionNumber)newinst.Pop();
				
				stack.Push(inum.Value);
			}
			
			ifunc.Execute(stack);
			newinst.Push(new InstructionNumber(stack.Pop()));
		}
		
		public static Cpg.Expression Precompute(Cpg.Expression expression, out bool isopt)
		{
			Stack<Instruction> newinst = new Stack<Instruction>();
			isopt = false;
			
			foreach (Instruction inst in expression.Instructions)
			{
				InstructionFunction ifunc;
				InstructionOperator iop;

				if (InstructionIs(inst, out iop) && Math.OperatorIsConstant((Cpg.MathOperatorType)iop.Id))
				{
					if (NumericArguments(iop, newinst))
					{
						Precompute(iop, newinst);
						isopt = true;
						continue;
					}
				}
				else if (InstructionIs(inst, out ifunc) && Math.FunctionIsConstant((Cpg.MathFunctionType)ifunc.Id))
				{
					if (NumericArguments(ifunc, newinst))
					{
						Precompute(ifunc, newinst);
						isopt = true;
						continue;
					}
				}
				
				newinst.Push(inst);
			}
			
			Cpg.Expression nexpr = new Cpg.Expression("0");
			nexpr.Instructions = newinst.ToArray();

			return nexpr;
		}
		
		public static implicit operator Cpg.Expression(Expression expression)
		{
			return expression.WrappedObject;
		}

		public static Expression Expand(params Cpg.Expression[] expressions)
		{
			Cpg.Expression expression = new Cpg.Expression("0");
			List<Instruction> instructions = new List<Instruction>();
			
			for (int i = 0; i < expressions.Length; ++i)
			{
				// Concatenate all the expressions together
				Expand(expressions[i], instructions);
				
				if (i != 0)
				{
					// Add plus operator to add expressions together
					instructions.Add(new InstructionOperator((uint)MathOperatorType.Plus, "+", 2));
				}
			}
			
			expression.Instructions = instructions.ToArray();
			return new Expression(expression);
		}
		
		private static void Expand(Cpg.Expression expr, List<Instruction> instructions)
		{
			foreach (Instruction inst in expr.Instructions)
			{
				InstructionProperty prop = inst as InstructionProperty;
				
				if (prop != null)
				{
					// See if we need to expand it
					Property property = prop.Property;
					
					if (!Knowledge.Instance.IsPersist(property))
					{
						// Expand the instruction
						Expand(property.Expression, instructions);
						continue;
					}
				}

				instructions.Add(inst);
			}
		}
	}
}

