print("""
#include <cdn-rawc/cdn-rawc-macros.h>

#ifndef CDN_MATH_DEFINE_PROTOS
#define CDN_MATH_DEFINE_PROTOS

#ifdef CDN_MATH_MATRIX_MULTIPLY_REQUIRED
#define CDN_MATH_MATRIX_MULTIPLY_V_REQUIRED
#endif

#include <math.h>
#include <stdlib.h>
#include <stdint.h>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

#define CDN_MATH_VALUE_TYPE_FUNC_REAL_ONE_MORE(Func,ValueType) CDN_MATH_VALUE_TYPE_FUNC_##ValueType(Func)
#define CDN_MATH_VALUE_TYPE_FUNC_REAL(Func,ValueType) CDN_MATH_VALUE_TYPE_FUNC_REAL_ONE_MORE(Func,ValueType)
#define CDN_MATH_VALUE_TYPE_FUNC(Func) CDN_MATH_VALUE_TYPE_FUNC_REAL(Func,ValueType)
#define CDN_MATH_VALUE_TYPE_FUNC_float(Func) Func##f
#define CDN_MATH_VALUE_TYPE_FUNC_double(Func) Func

#ifdef ENABLE_LAPACK
#ifdef PLATFORM_OSX
#define LP_int __CLPK_integer
#define LP_double __CLPK_doublereal
#define LP_float __CLPK_floatreal
#else
#define LP_int int32_t
#define LP_double double
#define LP_float float
#endif

#define LP_ValueTypeRealOneMore(ValueType) LP_##ValueType
#define LP_ValueTypeReal(ValueType) LP_ValueTypeRealOneMore(ValueType)
#define LP_ValueType LP_ValueTypeReal(ValueType)

#ifndef PLATFORM_OSX
extern void dgetrf_ (LP_int *,
                     LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_int *,
                     LP_int *);

extern void dgetri_ (LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_int *);

extern void dgelsd_ (LP_int *,
                     LP_int *,
                     LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_double *,
                     LP_double *,
                     LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_int *,
                     LP_int *);

extern void dgesv_ (LP_int *,
                    LP_int *,
                    LP_double *,
                    LP_int *,
                    LP_int *,
                    LP_double *,
                    LP_int *,
                    LP_int *);

extern void dgeqrf_ (LP_int *,
                     LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_double *,
                     LP_double *,
                     LP_int *,
                     LP_int *);

extern void dorgqr_ (LP_int *,
                     LP_int *,
                     LP_int *,
                     LP_double *,
                     LP_int *,
                     LP_double *,
                     LP_double *,
                     LP_int *,
                     LP_int *);



extern void sgetrf_ (LP_int *,
                     LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_int *,
                     LP_int *);

extern void sgetri_ (LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_int *);

extern void sgelsd_ (LP_int *,
                     LP_int *,
                     LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_float *,
                     LP_float *,
                     LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_int *,
                     LP_int *);

extern void sgesv_ (LP_int *,
                    LP_int *,
                    LP_float *,
                    LP_int *,
                    LP_int *,
                    LP_float *,
                    LP_int *,
                    LP_int *);

extern void sgeqrf_ (LP_int *,
                     LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_float *,
                     LP_float *,
                     LP_int *,
                     LP_int *);

extern void sorgqr_ (LP_int *,
                     LP_int *,
                     LP_int *,
                     LP_float *,
                     LP_int *,
                     LP_float *,
                     LP_float *,
                     LP_int *,
                     LP_int *);

#endif

#endif

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

    if f != 'rand':
        args = ["x{0}".format(i) for i in range(0, n)]

        argdecl = ", ".join(["ValueType {0}".format(x) for x in args])
        argcall = ", ".join(args)
    else:
        argdecl = 'void'

    if not body:
        body = "return CDN_MATH_VALUE_TYPE_FUNC({0})({1});".format(f, argcall)

    print("""static inline ValueType cdn_math_{0}_builtin ({1}) GNUC_INLINE;

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

binary_all = binary + ['max', 'min', 'modulo', 'csign', 'sum', 'product', 'sqsum']
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
	}""")

print_func('min', 2, 'return x0 < x1 ? x0 : x1;')
print_func('max', 2, 'return x0 > x1 ? x0 : x1;')
print_func('hypot', 2, 'return hypot(x0, x1);')
print_func('product', 2, 'return x0 * x1;')
print_func('sum', 2, 'return x0 + x1;')
print_func('sqsum', 2, 'return x0 * x0 + x1 * x1;')

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

def print_transpose():
    print_guard('transpose')

    print("""
static ValueType cdn_math_transpose_builtin (ValueType x0);

static ValueType
cdn_math_tranpose_builtin (ValueType x0)
{{
	return x0;
}}""")

    print_guard_end('transpose')

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

def print_vcat_v():
    print_guard('vcat_v')

    print("""
static ValueType *cdn_math_vcat_v_builtin (ValueType *ret,
                                           ValueType *x0,
                                           ValueType *x1,
                                           uint32_t   rows1,
                                           uint32_t   rows2,
                                           uint32_t   columns);

static ValueType *
cdn_math_vcat_v_builtin (ValueType *ret,
                        ValueType *x0,
                        ValueType *x1,
                        uint32_t   rows1,
                        uint32_t   rows2,
                        uint32_t   columns)
{{
	uint32_t c;
	uint32_t i1 = 0;
	uint32_t i2 = 0;
	uint32_t ptr = 0;

	for (c = 0; c < columns; ++c)
	{{
		uint32_t r = 0;

		for (r = 0; r < rows1; ++r)
		{{
			ret[ptr++] = x0[i1++];
		}}

		for (r = 0; r < rows2; ++r)
		{{
			ret[ptr++] = x1[i2++];
		}}
	}}

	return ret;
}}""")

    print_guard_end('vcat_v')

def print_linsolve_v():
    print_guard('linsolve_v')

    print("""
static ValueType *cdn_math_linsolve_v_builtin (ValueType *ret,
                                               ValueType *A,
                                               ValueType *b,
                                               uint32_t   RA,
                                               uint32_t   CB,
                                               int32_t   *ipiv);

#ifndef ENABLE_LAPACK
static ValueType *
cdn_math_linsolve_v_no_lapack_builtin (ValueType *ret,
                                       ValueType *A,
                                       ValueType *b,
                                       uint32_t   RA,
                                       uint32_t   CB,
                                       int32_t   *ipiv)
{{
	#error("The linsolve function is not supported without LAPACK");
}}
#else
#ifdef PLATFORM_OSX
#include <vecLib/vecLib.h>
#else
#include <clapack.h>
#endif

#define fdgesv_ sgesv_

static ValueType *
cdn_math_linsolve_v_lapack_builtin (ValueType *ret,
                                    ValueType *A,
                                    ValueType *b,
                                    uint32_t   RA,
                                    uint32_t   CB,
                                    int32_t   *ipiv)
{{
	LP_ValueType *lpA = A;
	LP_ValueType *lpb = ret;
	LP_int lpRA = RA;
	LP_int lpCB = CB;
	LP_int info;
	LP_int *lpipiv = ipiv;

	memcpy (ret, b, sizeof (ValueType) * RA * CB);

	CDN_MATH_VALUE_TYPE_FUNC(dgesv_) (&lpRA,
	                                  &lpCB,
	                                  lpA,
	                                  &lpRA,
	                                  lpipiv,
	                                  lpb,
	                                  &lpRA,
	                                  &info);

	return ret;
}}
#endif

static ValueType *
cdn_math_linsolve_v_builtin (ValueType *ret,
                             ValueType *A,
                             ValueType *b,
                             uint32_t   RA,
                             uint32_t   CB,
                             int32_t   *ipiv)
{{
#ifdef ENABLE_LAPACK
	return cdn_math_linsolve_v_lapack_builtin (ret, A, b, RA, CB, ipiv);
#else
	return cdn_math_linsolve_v_no_lapack_builtin (ret, A, b, RA, CB, ipiv);
#endif
}}
""")

    print_guard_end('linsolve_v')

def print_inverse_v():
    print_guard('inverse_v')

    print("""
static ValueType *cdn_math_inverse_v_builtin (ValueType *ret,
                                              ValueType *A,
                                              uint32_t   RA,
                                              int32_t   *ipiv,
                                              ValueType *work,
                                              int32_t    lwork);

#ifndef ENABLE_LAPACK
static ValueType *
cdn_math_inverse_v_no_lapack_builtin (ValueType *ret,
                                      ValueType *A,
                                      uint32_t   RA,
                                      int32_t   *ipiv,
                                      ValueType *work,
                                      int32_t    lwork)
{{
	#error("The inv function is not supported without LAPACK");
}}
#else

#define fdgetrf_ sgetrf_
#define fdgetri_ sgetri_

static ValueType *
cdn_math_inverse_v_lapack_builtin (ValueType *ret,
                                   ValueType *A,
                                   uint32_t   RA,
                                   int32_t   *ipiv,
                                   ValueType *work,
                                   int32_t    lwork)
{{
	LP_ValueType *lpA = ret;
	LP_int lpRA = RA;
	LP_int *lpipiv = ipiv;
	LP_ValueType *lpwork = work;
	LP_int lplwork = lwork;
	LP_int info;

	memcpy (ret, A, sizeof (ValueType) * RA * RA);

	CDN_MATH_VALUE_TYPE_FUNC(dgetrf_) (&lpRA,
	                                   &lpRA,
	                                   lpA,
	                                   &lpRA,
	                                   lpipiv,
	                                   &info);

	CDN_MATH_VALUE_TYPE_FUNC(dgetri_) (&lpRA,
	                                   lpA,
	                                   &lpRA,
	                                   lpipiv,
	                                   lpwork,
	                                   &lplwork,
	                                   &info);

	return ret;
}}
#endif

static ValueType *
cdn_math_inverse_v_builtin (ValueType *ret,
                             ValueType *A,
                             uint32_t   RA,
                             int32_t   *ipiv,
                             ValueType *work,
                             int32_t    lwork)
{{
#ifdef ENABLE_LAPACK
	return cdn_math_inverse_v_lapack_builtin (ret, A, RA, ipiv, work, lwork);
#else
	return cdn_math_inverse_v_no_lapack_builtin (ret, A, RA, ipiv, work, lwork);
#endif
}}
""")

    print_guard_end('inverse_v')


def print_pseudo_inverse_v():
    print_guard('pseudoinverse_v')

    print("""
static ValueType *cdn_math_pseudoinverse_v_builtin (ValueType *ret,
                                                     ValueType *A,
                                                     uint32_t   RA,
                                                     uint32_t   CA,
                                                     ValueType *B,
                                                     uint32_t   RB,
                                                     ValueType *S,
                                                     ValueType *work,
                                                     uint32_t   lwork,
                                                     int32_t   *iwork);

#ifndef ENABLE_LAPACK
static ValueType *
cdn_math_pseudoinverse_v_no_lapack_builtin (ValueType *ret,
                                             ValueType *A,
                                             uint32_t   RA,
                                             uint32_t   CA,
                                             ValueType *B,
                                             uint32_t   RB,
                                             ValueType *S,
                                             ValueType *work,
                                             uint32_t   lwork,
                                             int32_t   *iwork)
{{
	#error("The pinv function is not supported without LAPACK");
}}
#else

#define fdgelsd_ sgelsd_

static ValueType *
cdn_math_pseudoinverse_v_lapack_builtin (ValueType *ret,
                                          ValueType *A,
                                          uint32_t   RA,
                                          uint32_t   CA,
                                          ValueType *B,
                                          uint32_t   RB,
                                          ValueType *S,
                                          ValueType *work,
                                          uint32_t   lwork,
                                          int32_t   *iwork)
{{
	LP_ValueType *lpA     = A;
	LP_int        lpRA    = RA;
	LP_int        lpCA    = CA;
	LP_ValueType *lpB     = B;
	LP_int        lpRB    = RB;
	LP_ValueType *lpS     = S;
	LP_ValueType  rcond   = -1;
	LP_int        rank;
	LP_ValueType *lpwork  = work;
	LP_int        lplwork = lwork;
	LP_int       *lpiwork = iwork;
	LP_int        info;
	uint32_t      i;
	ValueType    *retptr;

	CDN_MATH_VALUE_TYPE_FUNC(dgelsd_) (&lpRA,
	                                   &lpCA,
	                                   &lpRB,
	                                   lpA,
	                                   &lpRA,
	                                   lpB,
	                                   &lpRB,
	                                   lpS,
	                                   &rcond,
	                                   &rank,
	                                   lpwork,
	                                   &lplwork,
	                                   lpiwork,
	                                   &info);

	retptr = ret;

	// copy back to ret
	for (i = 0; i < RA; ++i)
	{{
		memcpy (retptr, B, sizeof (ValueType) * CA);

		retptr += CA;
		B += RB;
	}}

	return ret;
}}
#endif

static ValueType *
cdn_math_pseudoinverse_v_builtin (ValueType *ret,
                                  ValueType *A,
                                  uint32_t   RA,
                                  uint32_t   CA,
                                  ValueType *B,
                                  uint32_t   RB,
                                  ValueType *S,
                                  ValueType *work,
                                  uint32_t   lwork,
                                  int32_t   *iwork)
{{
#ifdef ENABLE_LAPACK
	return cdn_math_pseudoinverse_v_lapack_builtin (ret, A, RA, CA, B, RB, S, work, lwork, iwork);
#else
	return cdn_math_pseudoinverse_v_no_lapack_builtin (ret, A, RA, CA, B, RB, S, work, lwork, iwork);
#endif
}}
""")

    print_guard_end('pseudoinverse_v')

def print_qr_v():
    print_guard('qr_v')

    print("""
static ValueType *cdn_math_qr_v_builtin (ValueType *ret,
                                         ValueType *A,
                                         uint32_t   RA,
                                         uint32_t   CA,
                                         ValueType *tau,
                                         ValueType *work,
                                         uint32_t   lwork);

#ifndef ENABLE_LAPACK
static ValueType *
cdn_math_qr_v_no_lapack_builtin (ValueType *ret,
                                 ValueType *A,
                                 uint32_t   RA,
                                 uint32_t   CA,
                                 ValueType *tau,
                                 ValueType *work,
                                 uint32_t   lwork)
{{
	#error("The `qr' function is not supported without LAPACK");
}}
#else

#define fdgeqrf_ sgeqrf_
#define fdorgqr_ sorgqr_

static ValueType *
cdn_math_qr_v_lapack_builtin (ValueType *ret,
                              ValueType *A,
                              uint32_t   RA,
                              uint32_t   CA,
                              ValueType *tau,
                              ValueType *work,
                              uint32_t   lwork)
{{
	LP_ValueType *lpA     = A;
	LP_int        lpRA    = RA;
	LP_int        lpCA    = CA;
	LP_ValueType *lptau   = tau;
	LP_ValueType *lpwork  = work;
	LP_int        lplwork = lwork;
	LP_int        info;
	uint32_t      i;
	ValueType    *retptr;
	ValueType    *ptrA;

	CDN_MATH_VALUE_TYPE_FUNC(dgeqrf_) (&lpRA,
	                                   &lpCA,
	                                   lpA,
	                                   &lpRA,
	                                   lptau,
	                                   lpwork,
	                                   &lplwork,
	                                   &info);

	retptr = ret + RA * RA;
	ptrA = A;

	memset (retptr, 0, sizeof(ValueType) * RA * CA);

	// copy R to ret
	for (i = 1; i <= CA; ++i)
	{
		memcpy (retptr, ptrA, sizeof(ValueType) * i);

		retptr += RA;
		ptrA += RA;
	}

	// compute q
	dorgqr_ (&lpRA,
	         &lpRA,
	         &lpCA,
	         lpA,
	         &lpRA,
	         lptau,
	         lpwork,
	         &lplwork,
	         &info);

	// copy from A to ret
	memcpy (ret, A, sizeof(ValueType) * RA * RA);

	return ret;
}}
#endif

static ValueType *
cdn_math_qr_v_builtin (ValueType *ret,
                       ValueType *A,
                       uint32_t   RA,
                       uint32_t   CA,
                       ValueType *tau,
                       ValueType *work,
                       uint32_t   lwork)
{{
#ifdef ENABLE_LAPACK
	return cdn_math_qr_v_lapack_builtin (ret, A, RA, CA, tau, work, lwork);
#else
	return cdn_math_qr_v_no_lapack_builtin (ret, A, RA, CA, tau, work, lwork);
#endif
}}
""")

    print_guard_end('qr_v')

def print_matrix_multiply_v():
    print_guard('matrix_multiply_v')

    print("""
static ValueType *cdn_math_matrix_multiply_v_builtin (ValueType *ret,
                                                      ValueType *x0,
                                                      ValueType *x1,
                                                      uint32_t   Rx0,
                                                      uint32_t   Cx0,
                                                      uint32_t   Cx1);

#ifndef ENABLE_BLAS

static ValueType *
cdn_math_matrix_multiply_v_no_blas_builtin (ValueType *ret,
                                            ValueType *x0,
                                            ValueType *x1,
                                            uint32_t   Rx0,
                                            uint32_t   Cx0,
                                            uint32_t   Cx1)
{{
	uint32_t c;
	uint32_t ptr = 0;

	for (c = 0; c < Cx1; ++c)
	{
		uint32_t r;

		for (r = 0; r < Rx0; ++r)
		{
			uint32_t i;
			uint32_t x0ptr = r;

			ret[ptr] = 0;

			for (i = 0; i < Cx0; ++i)
			{
				ret[ptr] += x0[x0ptr] * x1[i];
				x0ptr += Rx0;
			}

			++ptr;
		}

		x1 += Cx0;
	}

	return ret;
}}
#endif

#ifdef ENABLE_BLAS

#ifdef PLATFORM_OSX
#include <Accelerate/Accelerate.h>
#else
#include <cblas.h>
#endif

#define cblas_dgemmf cblas_sgemm

static ValueType *
cdn_math_matrix_multiply_v_blas_builtin (ValueType *ret,
                                         ValueType *x0,
                                         ValueType *x1,
                                         uint32_t   Rx0,
                                         uint32_t   Cx0,
                                         uint32_t   Cx1)
{{
	CDN_MATH_VALUE_TYPE_FUNC(cblas_dgemm)(CblasColMajor,
	             CblasNoTrans,
	             CblasNoTrans,
	             Rx0,
	             Cx1,
	             Cx0,
	             1,
	             x0,
	             Rx0,
	             x1,
	             Cx0,
	             0,
	             ret,
	             Rx0);
}}
#endif

static ValueType *
cdn_math_matrix_multiply_v_builtin (ValueType *ret,
                                    ValueType *x0,
                                    ValueType *x1,
                                    uint32_t   Rx0,
                                    uint32_t   Cx0,
                                    uint32_t   Cx1)
{{
#ifdef ENABLE_BLAS
	return cdn_math_matrix_multiply_v_blas_builtin (ret, x0, x1, Rx0, Cx0, Cx1);
#else
	return cdn_math_matrix_multiply_v_no_blas_builtin (ret, x0, x1, Rx0, Cx0, Cx1);
#endif
}}
""")

    print_guard_end('matrix_multiply_v')

    print_guard('matrix_multiply')

    print("""
static ValueType cdn_math_matrix_multiply_builtin (ValueType *x0,
                                                   ValueType *x1,
                                                   uint32_t   Rx0,
                                                   uint32_t   Cx0,
                                                   uint32_t   Cx1);

static ValueType
cdn_math_matrix_multiply_builtin (ValueType *x0,
                                  ValueType *x1,
                                  uint32_t   Rx0,
                                  uint32_t   Cx0,
                                  uint32_t   Cx1)
{{
	ValueType retval;

	CDN_MATH_MATRIX_MULTIPLY_V (&retval, x0, x1, Rx0, Cx0, Cx1);
	return retval;
}}

""")

    print_guard_end('matrix_multiply')

def print_slinsolve_v():
    print_guard('slinsolve_v')

    print("""
static ValueType *cdn_math_slinsolve_v_builtin (ValueType *ret,
                                                ValueType *A,
                                                uint32_t   RA,
                                                ValueType *b,
                                                uint32_t   CB,
                                                ValueType *L);

static void
_cdn_math_slinsolve_factorize (ValueType *A,
                               ValueType *L,
                               int32_t    n)
{{
	int32_t k;

	for (k = n - 1; k >= 0; --k)
	{{
		int32_t i = (int32_t)L[k];
		int32_t kk = k * (n + 1);

		while (i >= 0)
		{{
			int32_t j;
			ValueType a;
			int32_t ki;

			ki = k + i * n;

			// a = A_{ki} / A_{kk}
			a = A[ki] / A[kk];

			j = i;

			while (j >= 0)
			{{
				int32_t jn = j * n;
				int32_t ij = i + jn;
				int32_t kj = k + jn;

				// A_{ij} = A_{ij} - a A_{kj}
				A[ij] -= a * A[kj];

				j = (int32_t)L[j];
			}}

			// H_{ki} = a
			A[ki] = a;
			i = (int32_t)L[i];
		}}
	}}
}}

static void
_cdn_math_slinsolve_backsubs (ValueType *ptrA,
                              ValueType *ptrB,
                              ValueType *ptrL,
                              int32_t    n)
{{
	int32_t i;
	int32_t diag;

	diag = n * n - 1;

	// First solve for b = D^-1 L^-T b
	// see Sparce Factorization Algorithms, page 115
	for (i = n - 1; i >= 0; --i)
	{{
		int32_t j;

		j = (int32_t)ptrL[i];

		while (j >= 0)
		{{
			int32_t ij = i + j * n;

			// x_j = x_j - L_{ij} x_i
			ptrB[j] -= ptrA[ij] * ptrB[i];
			j = (int32_t)ptrL[j];
		}}

		// Apply D-1 from the diagonal elements if ptrA
		ptrB[i] /= ptrA[diag];
		diag -= n + 1;
	}}

	// Then finally solve for L^-1 b
	// see Sparce Factorization Algorithms, page 115
	for (i = 0; i < n; ++i)
	{{
		int32_t j;

		j = (int32_t)ptrL[i];

		while (j >= 0)
		{{
			int32_t ij = i + j * n;

			// x_i = x_i - L_{ij} x_j
			ptrB[i] -= ptrA[ij] * ptrB[j];
			j = (int32_t)ptrL[j];
		}}
	}}
}}

static ValueType *
cdn_math_slinsolve_v_builtin (ValueType *ret,
                              ValueType *A,
                              uint32_t   RA,
                              ValueType *b,
                              uint32_t   CB,
                              ValueType *L)
{{
	uint32_t i;
	ValueType *bptr;

	// Factorized A in place using LTDL factorization
	_cdn_math_slinsolve_factorize (A, L, RA);

	bptr = b;

	// Then backsubstitute
	for (i = 0; i < CB; ++i)
	{
		_cdn_math_slinsolve_backsubs (A, bptr, L, RA);
		bptr += RA;
	}

	memcpy (ret, b, sizeof (ValueType) * RA * CB);
	return ret;
}}
""")

    print_guard_end('slinsolve_v')

def print_diag():
    print_guard('diag')

    print("""
static void cdn_math_diag_builtin (ValueType x0);

static void
cdn_math_diag_builtin (ValueType x0)
{{
	return x0;
}}
""")

    print_guard_end('diag')

    print_guard('diag_v_m')

    print("""
static ValueType *cdn_math_diag_v_m_builtin (ValueType *ret,
                                             ValueType *x0,
                                             uint32_t   n);

static ValueType *
cdn_math_diag_v_m_builtin (ValueType *ret,
                           ValueType *x0,
                           uint32_t   n)
{{
	uint32_t i;

	for (i = 0; i < n; ++i)
	{{
		ret[i] = *x0;
		x0 += n + 1;
	}}

	return ret;
}}""")

    print_guard_end('diag_v_m')

    print_guard('diag_v_v')

    print("""
static ValueType *cdn_math_diag_v_v_builtin (ValueType *ret,
                                             ValueType *x0,
                                             uint32_t   n);

static ValueType *
cdn_math_diag_v_v_builtin (ValueType *ret,
                           ValueType *x0,
                           uint32_t   n)
{{
	uint32_t i;
	ValueType *retptr = ret;

	for (i = 0; i < n; ++i)
	{{
		*retptr = x0[i];
		retptr += n + 1;
	}}

	return ret;
}}""")

    print_guard_end('diag_v_v')

def print_tri():
    print_guard('tril_v')

    print("""
static ValueType *cdn_math_tril_v_builtin (ValueType *ret,
                                           ValueType *x0,
                                           uint32_t   rows,
                                           uint32_t   columns);

static ValueType *
cdn_math_tril_v_builtin (ValueType *ret,
                         ValueType *x0,
                         uint32_t   rows,
                         uint32_t   columns)
{{
	uint32_t c;
	ValueType *retptr = ret;

	for (c = 0; c < columns && c < rows; ++c)
	{{
		memcpy (retptr, x0, sizeof (ValueType) * (rows - c));

		retptr += rows + 1;
		x0 += rows + 1;
	}}

	return ret;
}}""")

    print_guard_end('tril_v')

    print_guard('triu_v')

    print("""
static ValueType *cdn_math_triu_v_builtin (ValueType *ret,
                                           ValueType *x0,
                                           uint32_t   rows,
                                           uint32_t   columns);

static ValueType *
cdn_math_triu_v_builtin (ValueType *ret,
                         ValueType *x0,
                         uint32_t   rows,
                         uint32_t   columns)
{{
	uint32_t c;
	ValueType *retptr = ret;

	for (c = 1; c <= columns; ++c)
	{{
		memcpy (retptr, x0, sizeof (ValueType) * (c > rows ? rows : c));

		retptr += rows;
		x0 += rows;
	}}

	return ret;
}}""")

    print_guard_end('triu_v')

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

print_accumulator_v('max', 'ret = CDN_MATH_MAX (ret, x0[i]);', 'x0[0]')
print_accumulator_v('min', 'ret = CDN_MATH_MIN (ret, x0[i]);', 'x0[0]')
print_accumulator_v('hypot', 'ret += x0[i] * x0[i];', 'x0[0] * x0[0]', 'CDN_MATH_SQRT (ret)')
print_accumulator_v('sum', 'ret += x0[i];')
print_accumulator_v('product', 'ret *= x0[i];')
print_accumulator_v('sqsum', 'ret += x0[i] * x0[i];', 'x0[0] * x0[0]')

print_index_v()
print_vcat_v()
print_transpose()
print_matrix_multiply_v()
print_linsolve_v()
print_qr_v()
print_slinsolve_v()
print_inverse_v()
print_pseudo_inverse_v()
print_diag()
print_tri()

print("#endif /* CDN_RAWC_MATH_PROTOS */")

# vi:ts=4:et
