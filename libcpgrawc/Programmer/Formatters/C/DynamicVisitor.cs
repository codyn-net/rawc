using System;
using System.Reflection;
using System.Collections.Generic;

namespace Cpg.RawC.Programmer.Formatters.C
{
	public class DynamicVisitor
	{
		[Flags()]
		public enum BindingFlags
		{
			Default,
			ExactReturnType,
			ExactParameters,
			ExactDynamicParameter
		}

		private Type d_returnType;
		private Type[] d_parameterTypes;
		private BindingFlags d_binding;
		private System.Reflection.BindingFlags d_methodBinding;

		private static Dictionary<Type, Dictionary<Type, MethodInfo>> s_cache;

		private Dictionary<Type, MethodInfo> d_methods;

		static DynamicVisitor()
		{
			s_cache = new Dictionary<Type, Dictionary<Type, MethodInfo>>();
		}
		
		public DynamicVisitor(Type returnType, params Type[] parameterTypes) : this(returnType, BindingFlags.Default, System.Reflection.BindingFlags.Default, parameterTypes)
		{
		}

		public DynamicVisitor(Type returnType, BindingFlags binding, System.Reflection.BindingFlags methodbinding, params Type[] parameterTypes)
		{
			if (parameterTypes.Length == 0)
			{
				throw new Exception("Parameter types must have at least one element");
			}

			d_returnType = returnType;
			d_parameterTypes = parameterTypes;
			d_binding = binding;
			d_methodBinding = methodbinding;
			
			Scan();
		}
		
		private MethodInfo Lookup(Type type)
		{
			Type orig = type;

			while (true)
			{
				MethodInfo method;

				if (d_methods.TryGetValue(type, out method))
				{
					if (type != orig)
					{
						d_methods[orig] = method;
					}

					return method;
				}
				
				type = type.BaseType;
				
				if (type == null || !TypeIsA(type, d_parameterTypes[0], false))
				{
					return null;
				}
			}
		}
		
		public T Invoke<T>(params object[] parameters)
		{
			Type type = parameters[0].GetType();
			MethodInfo method = Lookup(type);
			
			if (method != null)
			{
				return (T)method.Invoke(this, parameters);
			}
			else
			{
				throw new NotImplementedException(String.Format("The handler for `{0}' ({1}) is not yet implemented...", parameters[0].GetType(), parameters[0]));
			}
		}
		
		private bool TypeIsA(Type a, Type b)
		{
			return TypeIsA(a, b, false);
		}
		
		private bool TypeIsA(Type a, Type b, bool exact)
		{
			if (a == b)
			{
				return true;
			}
			
			if (exact)
			{
				return false;
			}
			
			if (b.IsInterface)
			{
				return a.GetInterface(b.FullName) != null;
			}
			else
			{
				return a.IsSubclassOf(b);
			}
		}
		
		private void Scan()
		{
			if (s_cache.TryGetValue(GetType(), out d_methods))
			{
				return;
			}
			
			d_methods = new Dictionary<Type, MethodInfo>();

			foreach (MethodInfo method in GetType().GetMethods(d_methodBinding))
			{
				if (!TypeIsA(method.ReturnType, d_returnType, (d_binding & BindingFlags.ExactReturnType) != 0))
				{
					continue;
				}
				
				ParameterInfo[] parameters = method.GetParameters();
				
				if (parameters.Length != d_parameterTypes.Length)
				{
					continue;
				}
				
				bool parametermatch = true;
				
				for (int i = 0; i < parameters.Length; ++i)
				{
					if (!TypeIsA(parameters[i].ParameterType, d_parameterTypes[i], (i == 0 ? false : (d_binding & BindingFlags.ExactParameters) != 0)))
					{
						parametermatch = false;
						break;
					}
				}
				
				if (!parametermatch)
				{
					continue;
				}
				
				Add(method, parameters[0].ParameterType);
			}
		}
		
		private void Add(MethodInfo method, Type type)
		{
			d_methods[type] = method;
		}
	}
}

