using System;
using System.Collections.Generic;

namespace Cdn.RawC
{
	public class ConstraintState : State
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

		public ConstraintState(Variable v) : base(v, v.Constraint, Flags.Constraint)
		{
			d_key = new Key(v);
		}

		public override object DataKey
		{
			get { return d_key; }
		}

		public override string ToString()
		{
			return String.Format("{0} (cst)", base.ToString());
		}
	}
}

