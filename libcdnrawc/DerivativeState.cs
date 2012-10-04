using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class DerivativeState : State
	{
		public class Key
		{
			private static Dictionary<object, uint> s_keys;
			private static uint s_uniqueId;

			private uint d_id;

			static Key()
			{
				s_keys = new Dictionary<object, uint>();
			}

			public Key(Variable v)
			{
				if (!s_keys.ContainsKey(v))
				{
					s_keys[v] = ++s_uniqueId;
				}

				d_id = s_keys[v];
			}

			public override bool Equals(object obj)
			{
				var askey = obj as Key;

				if (askey != null)
				{
					return d_id.Equals(askey.d_id);
				}

				return d_id.Equals(obj);
			}

			public override int GetHashCode()
			{
				return d_id.GetHashCode();
			}
		}

		private Key d_key;

		public DerivativeState(Variable v, EdgeAction[] actions) : base(v, actions)
		{
			Type |= Flags.Derivative;

			d_key = new Key(v);
		}

		public override object DataKey
		{
			get { return d_key; }
		}
	}
}

