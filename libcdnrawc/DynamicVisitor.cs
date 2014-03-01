using System;
using System.Reflection;
using System.Collections.Generic;

namespace Cdn.RawC
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

		private static Dictionary<Type, Dictionary<Type, List<MethodInfo>>> s_cache;

		private Dictionary<Type, List<MethodInfo>> d_methods;

		static DynamicVisitor()
		{
			s_cache = new Dictionary<Type, Dictionary<Type, List<MethodInfo>>>();
		}

		public DynamicVisitor(Type returnType, params Type[] parameterTypes) : this(returnType, BindingFlags.Default, System.Reflection.BindingFlags.Default, null, parameterTypes)
		{
		}

		public delegate bool MethodMatcher(MethodInfo info);

		public DynamicVisitor(Type returnType, BindingFlags binding, System.Reflection.BindingFlags methodbinding, MethodMatcher matcher, params Type[] parameterTypes)
		{
			if (parameterTypes.Length == 0)
			{
				throw new Exception("Parameter types must have at least one element");
			}

			d_returnType = returnType;
			d_parameterTypes = parameterTypes;
			d_binding = binding;
			d_methodBinding = methodbinding;

			Scan(matcher);
		}

		private List<MethodInfo> Lookup(Type type)
		{
			Type orig = type;

			while (true)
			{
				List<MethodInfo> method;

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
					return new List<MethodInfo>();
				}
			}
		}

		public delegate bool InvokeSelector(MethodInfo method);

		public T InvokeSelect<T>(InvokeSelector selector, params object[] parameters)
		{
			Type type = parameters[0].GetType();
			List<MethodInfo> method = Lookup(type);

			foreach (var m in method)
			{
				if (selector(m))
				{
					return (T)m.Invoke(this, parameters);
				}
			}

			throw new NotImplementedException(String.Format("The handler for `{0}' ({1}) is not yet implemented...", parameters[0].GetType(), parameters[0]));
		}

		public T Invoke<T>(params object[] parameters)
		{
			return InvokeSelect<T>(a => true, parameters);
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

		private void Scan(MethodMatcher matcher)
		{
			if (s_cache.TryGetValue(GetType(), out d_methods))
			{
				return;
			}

			d_methods = new Dictionary<Type, List<MethodInfo>>();

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

				if (matcher != null && !matcher(method))
				{
					continue;
				}

				Add(method, parameters[0].ParameterType);
			}
		}

		private void Add(MethodInfo method, Type type)
		{
			List<MethodInfo> lst;

			if (!d_methods.TryGetValue(type, out lst))
			{
				lst = new List<MethodInfo>();
				d_methods[type] = lst;
			}

			lst.Add(method);
		}
	}
}

