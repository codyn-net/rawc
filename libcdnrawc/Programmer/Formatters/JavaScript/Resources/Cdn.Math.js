if (!Cdn.Math)
{
(function(Cdn) {

	Cdn.Math = {
		abs: Math.abs,
		acos: Math.acos,
		asin: Math.asin,
		atan: Math.atan,
		atan2: Math.atan2,
		ceil: Math.ceil,
		cos: Math.cos,
		exp: Math.exp,
		floor: Math.floor,
		ln: Math.log,

		log10: function(v) {
			return Math.log(v) / Math.LN10;
		},

		pow: Math.pow,
		round: Math.round,
		sin: Math.sin,
		sqrt: Math.sqrt,
		tan: Math.tan,

		invsqrt: function (v) {
			return 1.0 / Cdn.Math.sqrt(v);
		},

		exp2: function(v) {
			return Cdn.Math.pow(2, v);
		},

		min: function(a, b) {
			return a < b ? a : b;
		},

		max: function(a, b) {
			return a > b ? a : b;
		},

		erf: function(v) {
			var a1 = 0.254829592,
			    a2 = -0.284496736,
			    a3 = 1.421413741,
			    a4 = -1.453152027,
			    a5 =  1.061405429,
			    p  =  0.3275911;

			var sign = v < 0 ? -1 : 1;
			v = Cdn.Math.abs(v);

			var t = 1.0 / (1.0 + p * v);
			var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Cdn.Math.exp(-v * v);

			return sign * y;
		},

		hypot: function(a, b) {
			return Cdn.Math.sqrt(a * a + b * b);
		},

		sinh: function(v) {
			var a = Cdn.Math.exp(v);
			var b = Cdn.Math.exp(-v);

			return (a - b) / 2.0;
		},

		cosh: function(v) {
			var a = Cdn.Math.exp(v);
			var b = Cdn.Math.exp(-v);

			return (a + b) / 2.0;
		},

		tanh: function(v) {
			var a = Cdn.Math.exp(v);
			var b = Cdn.Math.exp(-v);

			return (a - b) / (a + b);
		},

		lerp: function(a, b, c) {
			return b + (c - b) * a;
		},

		sqsum: function(a, b) {
			return a * a + b * b;
		},

		sign: function(v) {
			return v < 0 ? -1 : 1;
		},

		csign: function(a, b) {
			var a = Cdn.Math.abs(a);
			return b < 0 ? -a : a;
		},

		clip: function(v, from, to) {
			if (v < from)
			{
				return from;
			}
			else if (v > to)
			{
				return to;
			}
			else
			{
				return v;
			}
		},

		cycle: function(v, from, to) {
			if (v < from)
			{
				return to - ((from - v) % (to - from));
			}
			else if (v > to)
			{
				return from + ((v - from) % (to - from));
			}
			else
			{
				return v;
			}
		},

		transpose_v: function(v, rows, columns) {
			ret = new Array(v.length);

			var i = 0;

			for (var r = 0; r < rows; ++r)
			{
				var ptr = r;

				for (var c = 0; c < columns; ++c)
				{
					ret[ptr] = v[i++];
					ptr += rows;
				}
			}

			return ret;
		},

		matrix_multiply_v: function(a, b, rowsa, columnsa, columnsb) {
			var ptr = 0;
			var ret = new Array(rowsa * columnsb);
			var aptr = 0;

			for (var r = 0; r < rowsa; ++r)
			{
				for (var c = 0; c < columnsb; ++c)
				{
					var bptr = c;

					ret[ptr] = 0;

					for (var i = 0; i < columnsa; ++i)
					{
						ret[ptr] += a[aptr + i] * b[bptr];
						bptr += columnsb;
					}

					++ptr;
				}

				aptr += columnsa;
			}

			return ret;
		},

		inverse: function(v) {
			// TODO
		},

		linsolve: function(v) {
			// TODO
		},

		slinsolve: function(v) {
			// TODO
		},

		sum: function(a, b) {
			return a + b;
		},

		product: function(a, b) {
			return a * b;
		},

		hcat: function(a, b, rowsa, columnsa, columnsb) {
			var i1 = 0,
			    i2 = 0,
			    ptr = 0;

			for (var r = 0; r < rowsa; ++r)
			{
				for (var c = 0; c < columnsa; ++c)
				{
					ret[ptr++] = a[i1++];
				}

				for (var c = 0; c < columnsb; ++c)
				{
					ret[ptr++] = b[i2++];
				}
			}
		},

		uminus: function(v) {
			return -v;
		},

		negate: function(v) {
			return !v;
		},

		minus: function(a, b) {
			return a - b;
		},

		plus: function(a, b) {
			return a + b;
		},

		emultiply: function(a, b) {
			return a * b;
		},

		divide: function(a, b) {
			return a / b;
		},

		greater: function(a, b) {
			return a > b;
		},

		less: function(a, b) {
			return a < b;
		},

		greater_or_equal: function(a, b) {
			return a >= b;
		},

		less_or_equal: function(a, b) {
			return a <= b;
		},

		equal: function(a, b) {
			return a == b;
		},

		nequal: function(a, b) {
			return a != b;
		},

		or: function(a, b) {
			return a || b;
		},

		and: function(a, b) {
			return a && b;
		},

		modulo: function(a, b) {
			return a % b;
		},

		index_v: function(a, b, l) {
			var ret = new Array(l);

			for (var i = 0; i < l; ++i)
			{
				ret[i] = a[b[i]];
			}

			return ret;
		},

		hypot_v: function(v, l) {
			var ret = 0.0;

			for (var i = 0; i < l; ++i)
			{
				ret += v[i] * v[i];
			}

			return Cdn.Math.sqrt(ret);
		},

		sum_v: function(v, l) {
			var ret = 0.0;

			for (var i = 0; i < l; ++i)
			{
				ret += v[i];
			}

			return ret;
		},

		product_v: function(v, l) {
			var ret = 1.0;

			for (var i = 0; i < l; ++i)
			{
				ret *= v[i];
			}

			return ret;
		},

		min_v: function(v, l) {
			var ret = v[0];

			for (var i = i; i < l; ++i)
			{
				if (v[i] < ret)
				{
					ret = v[i];
				}
			}

			return ret;
		},

		max_v: function(v, l) {
			var ret = v[0];

			for (var i = i; i < l; ++i)
			{
				if (v[i] > ret)
				{
					ret = v[i];
				}
			}

			return ret;
		},

		sqsum_v: function(v, l) {
			var ret = 0.0;

			for (var i = 0; i < l; ++i)
			{
				ret += v[i] * v[i];
			}

			return ret;
		}
	};

	// Automatically generate element wise functions
	var unaries = [
		'sin',
		'cos',
		'tan',
		'asin',
		'acos',
		'atan',
		'sinh',
		'cosh',
		'tanh',
		'sqrt',
		'floor',
		'ceil',
		'round',
		'exp',
		'erf',
		'log10',
		'exp2',
		'invsqrt',
		'abs',
		'ln',
		'sign',

		// operators
		'uminus',
		'negate'
	];

	for (var u in unaries)
	{
		(function(u) {
			var uf = Cdn.Math[u];

			Cdn.Math[u + "_v"] = function(v, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = uf(v[i]);
				}

				return ret;
			};
		})(unaries[u]);
	}

	var binaries = [
		'atan2',
		'pow',
		'hypot',
		'max',
		'min',
		'modulo',
		'csign',
		'sum',
		'product',
		'sqsum',

		// operators
		'minus',
		'plus',
		'emultiply',
		'divide',
		'greater',
		'less',
		'greater_or_equal',
		'less_or_equal',
		'equal',
		'nequal',
		'or',
		'and',
		'modulo',
	];

	for (var b in binaries)
	{
		(function(b) {
			var bf = Cdn.Math[b];

			Cdn.Math[b + "_v_1_m"] = function(a, b, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = bf(a, b[i]);
				}

				return ret;
			};

			Cdn.Math[b + "_v_m_1"] = function(a, b, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = bf(a[i], b);
				}

				return ret;
			};

			Cdn.Math[b + "_v_m_m"] = function(a, b, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = bf(a[i], b[i]);
				}

				return ret;
			};
		})(binaries[b]);
	}

	var ternaries = [
		'lerp',
		'cycle',
		'clip',
	];

	for (var t in ternaries)
	{
		(function(t) {
			var tf = Cdn.Math[t];

			Cdn.Math[t + "_v_m_m_m"] = function(a, b, c, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = tf(a[i], b[i], c[i]);
				}

				return ret;
			};

			Cdn.Math[t + "_v_m_m_1"] = function(a, b, c, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = tf(a[i], b[i], c);
				}

				return ret;
			};

			Cdn.Math[t + "_v_m_1_m"] = function(a, b, c, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = tf(a[i], b, c[i]);
				}

				return ret;
			};

			Cdn.Math[t + "_v_1_m_m"] = function(a, b, c, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = tf(a, b[i], c[i]);
				}

				return ret;
			};

			Cdn.Math[t + "_v_1_1_m"] = function(a, b, c, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = tf(a, b, c[i]);
				}

				return ret;
			};

			Cdn.Math[t + "_v_m_1_1"] = function(a, b, c, l) {
				var ret = new Array(l);

				for (var i = 0; i < l; ++i)
				{
					ret[i] = tf(a[i], b, c);
				}

				return ret;
			};
		})(ternaries[t]);
	}
})(Cdn);
}
