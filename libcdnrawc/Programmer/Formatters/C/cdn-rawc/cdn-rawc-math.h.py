print("""
#include <cdn-rawc/cdn-rawc-macros.h>

#ifndef CDN_MATH_DEFINE_PROTOS
#define CDN_MATH_DEFINE_PROTOS

#include <math.h>
#include <stdlib.h>

#define CDN_MATH_VALUE_TYPE_FUNC_REAL_ONE_MORE(Func,ValueType) CDN_MATH_VALUE_TYPE_FUNC_##ValueType(Func)
#define CDN_MATH_VALUE_TYPE_FUNC_REAL(Func,ValueType) CDN_MATH_VALUE_TYPE_FUNC_REAL_ONE_MORE(Func,ValueType)
#define CDN_MATH_VALUE_TYPE_FUNC(Func) CDN_MATH_VALUE_TYPE_FUNC_REAL(Func,ValueType)
#define CDN_MATH_VALUE_TYPE_FUNC_float(Func) Func##f
#define CDN_MATH_VALUE_TYPE_FUNC_double(Func) Func
""")

def print_guard(f):
    fup = f.upper()

    print("""
#if defined(CDN_MATH_{1}_REQUIRED) && !defined(CDN_MATH_{1})
#define CDN_MATH_{1}_USE_BUILTIN
#define CDN_MATH_{1} cdn_math_{0}_builtin
""".format(f, fup))

def print_guard_end(f):
    print("#endif /* CDN_MATH_{0} */".format(f.upper()))

def print_func(f, n, body=None):
    print_guard(f)

    args = ["x{0}".format(i) for i in range(0, n)]

    argdecl = ", ".join(["ValueType {0}".format(x) for x in args])
    argcall = ", ".join(args)

    if not body:
        body = "return CDN_MATH_VALUE_TYPE_FUNC({0})({1});".format(f, argcall)

    print("""static ValueType cdn_math_{0}_builtin ({1}) GNUC_INLINE;

static ValueType cdn_math_{0}_builtin ({1})
{{
	{2}
}}""".format(f, argdecl, body))

    print_guard_end(f)

def print_func_v_intern(f, combos, ip):
    tps = {
        'm': 'ValueType *',
        '1': 'ValueType '
    }

    for x in combos:
        if len(x) == 1:
            name = ''
        else:
            name = '_{0}'.format("_".join(list(x)))

        if ip:
            name += "_ip"

        print_guard('{0}_v{1}'.format(f, name))

        args = ['{0}x{1}'.format(tps[x[i]], i) for i in range(len(x))]
        reti = x.index('m')

        cargs = []

        if not ip:
            args.insert(0, 'ValueType *ret')
            reti = 'ret'
        else:
            reti = 'x{0}'.format(x.index('m'))

        for i in range(len(x)):
            if x[i] == 'm':
                cargs.append('x{0}[i]'.format(i))
            else:
                cargs.append('x{0}'.format(i))

        print("""static ValueType *cdn_math_{0}_v{1}_builtin ({2}, uint32_t l);

static ValueType *cdn_math_{0}_v{1}_builtin ({2}, uint32_t l)
{{
	uint32_t i;

	for (i = 0; i < l; ++i)
	{{
		{5}[i] = CDN_MATH_{3} ({4});
	}}

	return ret;
}}""".format(f, name, ", ".join(args), f.upper(), ", ".join(cargs), reti))

        print_guard_end('{0}_v{1}'.format(f, name))

def print_func_v(f, combos):
    print_func_v_intern(f, combos, False)
    print_func_v_intern(f, combos, True)

def print_unary_v(f):
    print_func_v(f, ['m'])

def print_binary_v(f):
    print_func_v(f, ('mm', 'm1', '1m'))

def print_ternary_v(f):
    print_func_v(f, ('mmm', 'mm1', 'm1m', 'm11', '1mm', '1m1', '11m'))

def print_accumulator_v(f, body, init=None, fini=None):
    print_guard(f + '_v')

    if not init:
        init = 'x0[0]'

    if not fini:
        fini = 'ret'

    print("""static ValueType cdn_math_{0}_v_builtin (ValueType *x0, uint32_t l);

static ValueType cdn_math_{0}_v_builtin (ValueType *x0, uint32_t l)
{{
	uint32_t i;
	ValueType ret = {1};

	for (i = 1; i < l; ++i)
	{{
		{2}
	}}

	return {3};
}}""".format(f, init, body, fini))

    print_guard_end(f + '_v')

unary = [
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
]

unary_all = unary + ['invsqrt', 'abs', 'ln', 'sign']

binary = [
    'atan2',
    'pow',
    'hypot',
]

binary_all = binary + ['max', 'min', 'modulo', 'csign']
ternary_all = ['lerp', 'clip', 'cycle']

for f in unary_all:
    print("""
#if (defined(CDN_MATH_{0}_V_REQUIRED) && !defined(CDN_MATH_{0}_V))
#define CDN_MATH_{0}_REQUIRED
#endif""".format(f.upper()))

for f in binary_all:
    print("""
#if (defined(CDN_MATH_{0}_V_M_M_REQUIRED) && !defined(CDN_MATH_{0}_V_M_M)) || \\
   (defined(CDN_MATH_{0}_V_1_M_REQUIRED) && !defined(CDN_MATH_{0}_V_1_M)) || \\
   (defined(CDN_MATH_{0}_V_M_1_REQUIRED) && !defined(CDN_MATH_{0}_V_M_1))
#define CDN_MATH_{0}_REQUIRED
#endif""".format(f.upper()))

for f in ternary_all:
    print("""
#if (defined(CDN_MATH_{0}_V_M_M_M_REQUIRED) && !defined(CDN_MATH_{0}_V_M_M_M)) || \\
   (defined(CDN_MATH_{0}_V_M_M_1_REQUIRED) && !defined(CDN_MATH_{0}_V_M_M_1)) || \\
   (defined(CDN_MATH_{0}_V_M_1_M_REQUIRED) && !defined(CDN_MATH_{0}_V_M_1_M)) || \\
   (defined(CDN_MATH_{0}_V_M_1_1_REQUIRED) && !defined(CDN_MATH_{0}_V_M_1_1)) || \\
   (defined(CDN_MATH_{0}_V_1_M_M_REQUIRED) && !defined(CDN_MATH_{0}_V_1_M_M)) || \\
   (defined(CDN_MATH_{0}_V_1_M_1_REQUIRED) && !defined(CDN_MATH_{0}_V_1_M_1)) || \\
   (defined(CDN_MATH_{0}_V_1_1_M_REQUIRED) && !defined(CDN_MATH_{0}_V_1_1_M))
#define CDN_MATH_{0}_REQUIRED
#endif""".format(f.upper()))

print("""
#if (defined(CDN_MATH_MAX_V_REQUIRED) && !defined(CDN_MATH_MAX_V))
#define CDN_MATH_MAX_REQUIRED
#endif

#if (defined(CDN_MATH_MIN_V_REQUIRED) && !defined(CDN_MATH_MIN_V))
#define CDN_MATH_MIN_REQUIRED
#endif

#if (defined(CDN_MATH_HYPOT_V_REQUIRED) && !defined(CDN_MATH_HYPOT_V))
#define CDN_MATH_SQRT_REQUIRED
#endif""")

for f in unary:
    print_func(f, 1)

for f in binary:
    print_func(f, 2)

# Special cases
print_func('invsqrt', 1, 'return 1.0 / CDN_MATH_SQRT (x0);')
print_func('abs',   1, 'return CDN_MATH_VALUE_TYPE_FUNC(fabs) (x0);')
print_func('ln',    1, 'return CDN_MATH_VALUE_TYPE_FUNC(log) (x0);')
print_func('sign',  1, 'return signbit (x0) ? -1 : 1;')
print_func('csign', 2, 'return copysign (x0, x1);')
print_func('lerp',  3, 'return x1 + (x2 - x1) * x0;')

print_func('clip',  3, """if (x0 < x1)
	{
		return x1;
	}
	else if (x0 > x2)
	{
		return x2;
	}
	else
	{
		return x0;
	}""")

print_func('cycle', 3, """if (x0 < x1)
	{
		return x2 - CDN_MATH_VALUE_TYPE_FUNC(fmod) (x1 - x0, x2 - x1);
	}
	else if (x0 > x2)
	{
		return x1 + CDN_MATH_VALUE_TYPE_FUNC(fmod) (x0 - x1, x2 - x1);
	}
	else
	{
		return x0;
	})""")

print_func('min', 2, 'return x0 < x1 ? x0 : x1;')
print_func('max', 2, 'return x0 > x1 ? x0 : x1;')

print_func('rand', 1, 'return (random () / (ValueType)RAND_MAX);')
print_func('modulo', 2, """ValueType ans = CDN_MATH_VALUE_TYPE_FUNC(fmod) (x0, x1);

	return ans < 0 ? ans + x1 : ans;""")

# Generate functions for element wise vector operations
for f in unary_all:
    print_unary_v(f)

for f in binary_all:
    print_binary_v(f)

for f in ternary_all:
    print_ternary_v(f)

def print_operator_v(f, n, op):
    tps = {
        'm': 'ValueType *',
        '1': 'ValueType '
    }

    if n == 1:
        combos = ['m']
    elif n == 2:
        combos = ['mm', '1m', 'm1']
        op = " " + op + " "

    for x in combos:
        if len(x) == 1:
            name = ''
        else:
            name = '_{0}'.format("_".join(list(x)))

        print_guard('{0}_v{1}'.format(f, name))

        args = ['{0}x{1}'.format(tps[x[i]], i) for i in range(len(x))]
        reti = x.index('m')

        cargs = []

        for i in range(len(x)):
            if x[i] == 'm':
                carg = 'x{0}[i]'.format(i)
            else:
                carg = 'x{0}'.format(i)

            if n == 1:
                carg = op + carg

            cargs.append(carg)

        print("""static ValueType *cdn_math_{0}_v{1}_builtin (ValueType *ret, {2}, uint32_t l);

static ValueType *cdn_math_{0}_v{1}_builtin (ValueType *ret, {2}, uint32_t l)
{{
	uint32_t i;

	for (i = 0; i < l; ++i)
	{{
		ret[i] = {4};
	}}

	return ret;
}}""".format(f, name, ", ".join(args), f.upper(), op.join(cargs)))

        print_guard_end('{0}_v{1}'.format(f, name))

def print_index_v():
    print_guard('index_v')

    print("""
static ValueType *cdn_math_index_v_builtin (ValueType *ret,
                                            ValueType *x0,
                                            uint32_t  *indices,
                                            uint32_t   l);

static ValueType *
cdn_math_index_v_builtin (ValueType *ret,
                          ValueType *x0,
                          uint32_t  *indices,
                          uint32_t   l)
{{
	uint32_t i;

	for (i = 0; i < l; ++i)
	{{
		ret[i] = x0[indices[i]];
	}}

	return ret;
}}""")

    print_guard_end('index_v')

def print_transpose_v():
    print_guard('transpose_v')

    print("""
static ValueType *cdn_math_transpose_v_builtin (ValueType *ret,
                                                ValueType *x0,
                                                uint32_t   rows,
                                                uint32_t   columns);

static ValueType *
cdn_math_transpose_v_builtin (ValueType *ret,
                              ValueType *x0,
                              uint32_t   rows,
                              uint32_t   columns)
{{
	uint32_t r;
	uint32_t i = 0;
	uint32_t ptr = 0;

	for (r = 0; r < rows; ++r)
	{{
		uint32_t c;

		ptr = r;

		for (c = 0; c < columns; ++c)
		{{
			ret[ptr] = x0[i++];
			ptr += rows;
		}}
	}}

	return ret;
}}""")

    print_guard_end('transpose_v')

def print_hcat():
    print_guard('hcat')

    print("""
static ValueType *cdn_math_hcat_builtin (ValueType *ret,
                                         ValueType *x0,
                                         ValueType *x1,
                                         uint32_t   rows,
                                         uint32_t   columns1,
                                         uint32_t   columns2);

static ValueType *
cdn_math_hcat_builtin (ValueType *ret,
                       ValueType *x0,
                       ValueType *x1,
                       uint32_t   rows,
                       uint32_t   columns1,
                       uint32_t   columns2)
{{
	uint32_t r;
	uint32_t i1 = 0;
	uint32_t i2 = 0;
	uint32_t ptr = 0;

	for (r = 0; r < rows; ++r)
	{{
		uint32_t c;

		for (c = 0; c < columns1; ++c)
		{{
			ret[ptr++] = x0[i1++];
		}}

		for (c = 0; c < columns2; ++c)
		{{
			ret[ptr++] = x1[i2++];
		}}
	}}

	return ret;
}}""")

    print_guard_end('hcat')

def print_matrix_multiply_v():
    print_guard('matrix_multiply_v')

    print("""
static ValueType *cdn_math_matrix_multiply_v_builtin (ValueType *ret,
                                                      ValueType *x0,
                                                      ValueType *x1,
                                                      uint32_t   Rx0,
                                                      uint32_t   Cx0,
                                                      uint32_t   Cx1);

#include <stdio.h>

static ValueType *
cdn_math_matrix_multiply_v_builtin (ValueType *ret,
                                    ValueType *x0,
                                    ValueType *x1,
                                    uint32_t   Rx0,
                                    uint32_t   Cx0,
                                    uint32_t   Cx1)
{{
	uint32_t r;
	uint32_t ptr = 0;

	for (r = 0; r < Rx0; ++r)
	{{
		uint32_t c;

		for (c = 0; c < Cx1; ++c)
		{{
			uint32_t i;
			uint32_t x1ptr = c;

			ret[ptr] = 0;

			for (i = 0; i < Cx0; ++i)
			{{
				ret[ptr] += x0[i] * x1[x1ptr];

				x1ptr += Cx1;
			}}

			++ptr;
		}}

		x0 += Cx0;
	}}

	return ret;
}}""")

    print_guard_end('matrix_multiply_v')

# Element wise operators
print_operator_v('uminus', 1, '-')
print_operator_v('negate', 1, '!')

print_operator_v('minus', 2, '-')
print_operator_v('plus', 2, '+')
print_operator_v('emultiply', 2, '*')
print_operator_v('divide', 2, '/')

print_operator_v('greater', 2, '>')
print_operator_v('less', 2, '<')
print_operator_v('greater_or_equal', 2, '>=')
print_operator_v('less_or_equal', 2, '<=')
print_operator_v('equal', 2, '==')
print_operator_v('nequal', 2, '!=')
print_operator_v('or', 2, '||')
print_operator_v('and', 2, '&&')

print_accumulator_v('sum', 'ret += x0[i];')
print_accumulator_v('product', 'ret *= x0[i];')
print_accumulator_v('sqsum', 'ret += x0[i] * x0[i];', 'x0[0] * x0[0]')

print_accumulator_v('max', 'ret = CDN_MATH_MAX (ret, x0[i]);', 'x0[0]')
print_accumulator_v('min', 'ret = CDN_MATH_MIN (ret, x0[i]);', 'x0[0]')
print_accumulator_v('hypot', 'ret += x0[i] * x0[i];', 'x0[0] * x0[0]', 'CDN_MATH_SQRT (ret)')

print_index_v()
print_hcat()
print_transpose_v()
print_matrix_multiply_v()

print("#endif /* CDN_RAWC_MATH_PROTOS */")

# vi:ts=4:et
