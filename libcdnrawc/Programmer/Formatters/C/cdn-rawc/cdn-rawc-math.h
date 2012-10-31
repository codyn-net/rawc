#ifndef GNUC_INLINE
#ifdef __GNUC__
#define GNUC_INLINE __attribute__ ((always_inline))
#else
#define GNUC_INLINE
#endif
#endif

#ifndef CDN_MATH_DEFINE_PROTOS
#define CDN_MATH_DEFINE_PROTOS

#include <math.h>
#include <stdlib.h>

#define CDN_MATH_DEFINE_PROTO_UNARY(func) \
	static ValueType cdn_math_##func##_builtin (ValueType x0) GNUC_INLINE;

#define CDN_MATH_DEFINE_PROTO_BINARY(func) \
	static ValueType cdn_math_##func##_builtin (ValueType x0, ValueType x1) GNUC_INLINE;

#define CDN_MATH_DEFINE_PROTO_TERNARY(func) \
	static ValueType cdn_math_##func##_builtin (ValueType x0, ValueType x1, ValueType x2) GNUC_INLINE;

#define CDN_MATH_VALUE_TYPE_FUNC_REAL_ONE_MORE(Func,ValueType) CDN_MATH_VALUE_TYPE_FUNC_##ValueType(Func)
#define CDN_MATH_VALUE_TYPE_FUNC_REAL(Func,ValueType) CDN_MATH_VALUE_TYPE_FUNC_REAL_ONE_MORE(Func,ValueType)
#define CDN_MATH_VALUE_TYPE_FUNC(Func) CDN_MATH_VALUE_TYPE_FUNC_REAL(Func,ValueType)
#define CDN_MATH_VALUE_TYPE_FUNC_float(Func) Func##f
#define CDN_MATH_VALUE_TYPE_FUNC_double(Func) Func

#define CDN_MATH_DEFINE_WRAPPER_BINARY(func, FUNC)				\
static ValueType								\
cdn_math_##func##_builtin (ValueType x0, ValueType x1)				\
{										\
	return CDN_MATH_VALUE_TYPE_FUNC(func) (x0, x1);				\
}

#define CDN_MATH_DEFINE_WRAPPER_UNARY(func, FUNC)				\
static ValueType								\
cdn_math_##func##_builtin (ValueType x0)					\
{										\
	return CDN_MATH_VALUE_TYPE_FUNC(func) (x0);				\
}

#endif

#if defined(CDN_MATH_SIN_REQUIRED) && !defined(CDN_MATH_SIN)
#define CDN_MATH_SIN_USE_BUILTIN
#define CDN_MATH_SIN cdn_math_sin_builtin

CDN_MATH_DEFINE_PROTO_UNARY(sin)
CDN_MATH_DEFINE_WRAPPER_UNARY(sin, SIN)

#endif

#if defined(CDN_MATH_COS_REQUIRED) && !defined(CDN_MATH_COS)
#define CDN_MATH_COS_USE_BUILTIN
#define CDN_MATH_COS cdn_math_cos_builtin

CDN_MATH_DEFINE_PROTO_UNARY(cos)
CDN_MATH_DEFINE_WRAPPER_UNARY(cos, COS)

#endif

#if defined(CDN_MATH_TAN_REQUIRED) && !defined(CDN_MATH_TAN)
#define CDN_MATH_TAN_USE_BUILTIN
#define CDN_MATH_TAN cdn_math_tan_builtin

CDN_MATH_DEFINE_PROTO_UNARY(tan)
CDN_MATH_DEFINE_WRAPPER_UNARY(tan, TAN)

#endif

#if defined(CDN_MATH_ASIN_REQUIRED) && !defined(CDN_MATH_ASIN)
#define CDN_MATH_ASIN_USE_BUILTIN
#define CDN_MATH_ASIN cdn_math_asin_builtin

CDN_MATH_DEFINE_PROTO_UNARY(asin)
CDN_MATH_DEFINE_WRAPPER_UNARY(asin, ASIN)

#endif

#if defined(CDN_MATH_ACOS_REQUIRED) && !defined(CDN_MATH_ACOS)
#define CDN_MATH_ACOS_USE_BUILTIN
#define CDN_MATH_ACOS cdn_math_acos_builtin

CDN_MATH_DEFINE_PROTO_UNARY(acos)
CDN_MATH_DEFINE_WRAPPER_UNARY(acos, ACOS)

#endif

#if defined(CDN_MATH_ATAN_REQUIRED) && !defined(CDN_MATH_ATAN)
#define CDN_MATH_ATAN_USE_BUILTIN
#define CDN_MATH_ATAN cdn_math_atan_builtin

CDN_MATH_DEFINE_PROTO_UNARY(atan)
CDN_MATH_DEFINE_WRAPPER_UNARY(atan, ATAN)

#endif

#if defined(CDN_MATH_SINH_REQUIRED) && !defined(CDN_MATH_SINH)
#define CDN_MATH_SINH_USE_BUILTIN
#define CDN_MATH_SINH cdn_math_sinh_builtin

CDN_MATH_DEFINE_PROTO_UNARY(sinh)
CDN_MATH_DEFINE_WRAPPER_UNARY(sinh, SINH)

#endif

#if defined(CDN_MATH_COSH_REQUIRED) && !defined(CDN_MATH_COSH)
#define CDN_MATH_COSH_USE_BUILTIN
#define CDN_MATH_COSH cdn_math_cosh_builtin

CDN_MATH_DEFINE_PROTO_UNARY(cosh)
CDN_MATH_DEFINE_WRAPPER_UNARY(cosh, COSH)

#endif

#if defined(CDN_MATH_TANH_REQUIRED) && !defined(CDN_MATH_TANH)
#define CDN_MATH_TANH_USE_BUILTIN
#define CDN_MATH_TANH cdn_math_tanh_builtin

CDN_MATH_DEFINE_PROTO_UNARY(tanh)
CDN_MATH_DEFINE_WRAPPER_UNARY(tanh, TANH)

#endif

#if defined(CDN_MATH_SQRT_REQUIRED) && !defined(CDN_MATH_SQRT)
#define CDN_MATH_SQRT_USE_BUILTIN
#define CDN_MATH_SQRT cdn_math_sqrt_builtin

CDN_MATH_DEFINE_PROTO_UNARY(sqrt)
CDN_MATH_DEFINE_WRAPPER_UNARY(sqrt, SQRT)

#endif

#if defined(CDN_MATH_FLOOR_REQUIRED) && !defined(CDN_MATH_FLOOR)
#define CDN_MATH_FLOOR_USE_BUILTIN
#define CDN_MATH_FLOOR cdn_math_floor_builtin

CDN_MATH_DEFINE_PROTO_UNARY(floor)
CDN_MATH_DEFINE_WRAPPER_UNARY(floor, FLOOR)

#endif

#if defined(CDN_MATH_CEIL_REQUIRED) && !defined(CDN_MATH_CEIL)
#define CDN_MATH_CEIL_USE_BUILTIN
#define CDN_MATH_CEIL cdn_math_ceil_builtin

CDN_MATH_DEFINE_PROTO_UNARY(ceil)
CDN_MATH_DEFINE_WRAPPER_UNARY(ceil, CEIL)

#endif

#if defined(CDN_MATH_ROUND_REQUIRED) && !defined(CDN_MATH_ROUND)
#define CDN_MATH_ROUND_USE_BUILTIN
#define CDN_MATH_ROUND cdn_math_round_builtin

CDN_MATH_DEFINE_PROTO_UNARY(round)
CDN_MATH_DEFINE_WRAPPER_UNARY(round, ROUND)

#endif

#if defined(CDN_MATH_EXP_REQUIRED) && !defined(CDN_MATH_EXP)
#define CDN_MATH_EXP_USE_BUILTIN
#define CDN_MATH_EXP cdn_math_exp_builtin

CDN_MATH_DEFINE_PROTO_UNARY(exp)
CDN_MATH_DEFINE_WRAPPER_UNARY(exp, EXP)

#endif

#if defined(CDN_MATH_ERF_REQUIRED) && !defined(CDN_MATH_ERF)
#define CDN_MATH_ERF_USE_BUILTIN
#define CDN_MATH_ERF cdn_math_erf_builtin

CDN_MATH_DEFINE_PROTO_UNARY(erf)
CDN_MATH_DEFINE_WRAPPER_UNARY(erf, ERF)

#endif

#if defined(CDN_MATH_LOG10_REQUIRED) && !defined(CDN_MATH_LOG10)
#define CDN_MATH_LOG10_USE_BUILTIN
#define CDN_MATH_LOG10 cdn_math_log10_builtin

CDN_MATH_DEFINE_PROTO_UNARY(log10)
CDN_MATH_DEFINE_WRAPPER_UNARY(log10, LOG10)

#endif

#if defined(CDN_MATH_EXP2_REQUIRED) && !defined(CDN_MATH_EXP2)
#define CDN_MATH_EXP2_USE_BUILTIN
#define CDN_MATH_EXP2 cdn_math_exp2_builtin

CDN_MATH_DEFINE_PROTO_UNARY(exp2)
CDN_MATH_DEFINE_WRAPPER_UNARY(exp2, EXP2)

#endif

#if defined(CDN_MATH_ATAN2_REQUIRED) && !defined(CDN_MATH_ATAN2)
#define CDN_MATH_ATAN2_USE_BUILTIN
#define CDN_MATH_ATAN2 cdn_math_atan2_builtin

CDN_MATH_DEFINE_PROTO_BINARY(atan2)
CDN_MATH_DEFINE_WRAPPER_BINARY(atan2, ATAN2)

#endif

#if defined(CDN_MATH_POW_REQUIRED) && !defined(CDN_MATH_POW)
#define CDN_MATH_POW_USE_BUILTIN
#define CDN_MATH_POW cdn_math_pow_builtin

CDN_MATH_DEFINE_PROTO_BINARY(pow)
CDN_MATH_DEFINE_WRAPPER_BINARY(pow, POW)

#endif

#if defined(CDN_MATH_HYPOT_REQUIRED) && !defined(CDN_MATH_HYPOT)
#define CDN_MATH_HYPOT_USE_BUILTIN
#define CDN_MATH_HYPOT cdn_math_hypot_builtin

CDN_MATH_DEFINE_PROTO_BINARY(hypot)
CDN_MATH_DEFINE_WRAPPER_BINARY(hypot, HYPOT)

#endif

#if defined(CDN_MATH_INVSQRT_REQUIRED) && !defined(CDN_MATH_INVSQRT)
#define CDN_MATH_INVSQRT_USE_BUILTIN
#define CDN_MATH_INVSQRT cdn_math_invsqrt_builtin

CDN_MATH_DEFINE_PROTO_UNARY(invsqrt)

static ValueType
cdn_math_invsqrt_builtin (ValueType x0)
{
	return 1.0 / CDN_MATH_SQRT (x0);
}

#endif

#if defined(CDN_MATH_ABS_REQUIRED) && !defined(CDN_MATH_ABS)
#define CDN_MATH_ABS_USE_BUILTIN
#define CDN_MATH_ABS cdn_math_abs_builtin

CDN_MATH_DEFINE_PROTO_UNARY(abs)

static ValueType
cdn_math_abs_builtin (ValueType x0)
{
	return CDN_MATH_VALUE_TYPE_FUNC(fabs) (x0);
}

#endif

#if defined(CDN_MATH_LN_REQUIRED) && !defined(CDN_MATH_LN)
#define CDN_MATH_LN_USE_BUILTIN
#define CDN_MATH_LN cdn_math_ln_builtin

CDN_MATH_DEFINE_PROTO_UNARY(ln)

static ValueType
cdn_math_ln_builtin (ValueType x0)
{
	return CDN_MATH_VALUE_TYPE_FUNC(log) (x0);
}

#endif

#if defined(CDN_MATH_SIGN_REQUIRED) && !defined(CDN_MATH_SIGN)
#define CDN_MATH_SIGN_USE_BUILTIN
#define CDN_MATH_SIGN cdn_math_sign_builtin

CDN_MATH_DEFINE_PROTO_UNARY(sign)
CDN_MATH_DEFINE_WRAPPER_UNARY(sign, SIGN)

static ValueType
cdn_math_sign_builtin (ValueType x0)
{
	return signbit (x0) ? -1 : 1;
}

#endif

#if defined(CDN_MATH_CSIGN_REQUIRED) && !defined(CDN_MATH_CSIGN)
#define CDN_MATH_CSIGN_USE_BUILTIN
#define CDN_MATH_CSIGN cdn_math_csign_builtin

CDN_MATH_DEFINE_PROTO_UNARY(csign)

static ValueType
cdn_math_csign_builtin (ValueType x0, ValueType x1)
{
	return copysign (x0, x1);
}

#endif

#if defined(CDN_MATH_LERP_REQUIRED) && !defined(CDN_MATH_LERP)
#define CDN_MATH_LERP_USE_BUILTIN
#define CDN_MATH_LERP cdn_math_lerp_builtin

CDN_MATH_DEFINE_PROTO_TERNARY(lerp)

static ValueType
cdn_math_lerp_builtin (ValueType x0, ValueType x1, ValueType x2)
{
	return x1 + (x2 - x1) * x0;
}

#endif

#if defined(CDN_MATH_CLIP_REQUIRED) && !defined(CDN_MATH_CLIP)
#define CDN_MATH_CLIP_USE_BUILTIN
#define CDN_MATH_CLIP cdn_math_clip_builtin

CDN_MATH_DEFINE_PROTO_TERNARY(clip)

static ValueType
cdn_math_clip_builtin (ValueType x0, ValueType x1, ValueType x2)
{
	if (x0 < x1)
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
	}
}

#endif

#if defined(CDN_MATH_CYCLE_REQUIRED) && !defined(CDN_MATH_CYCLE)
#define CDN_MATH_CYCLE_USE_BUILTIN
#define CDN_MATH_CYCLE cdn_math_cycle_builtin

CDN_MATH_DEFINE_PROTO_TERNARY(cycle)

static ValueType
cdn_math_cycle_builtin (ValueType x0, ValueType x1, ValueType x2)
{
	if (x0 < x1)
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
	}
}

#endif

#if defined(CDN_MATH_MIN_REQUIRED) && !defined(CDN_MATH_MIN)
#define CDN_MATH_MIN_USE_BUILTIN
#define CDN_MATH_MIN cdn_math_min_builtin

CDN_MATH_DEFINE_PROTO_BINARY(min)

static ValueType
cdn_math_min_builtin (ValueType x0, ValueType x1)
{
	if (x0 < x1)
	{
		return x0;
	}
	else
	{
		return x1;
	}
}

#endif

#if defined(CDN_MATH_MAX_REQUIRED) && !defined(CDN_MATH_MAX)
#define CDN_MATH_MAX_USE_BUILTIN
#define CDN_MATH_MAX cdn_math_max_builtin

CDN_MATH_DEFINE_PROTO_BINARY(max)

static ValueType
cdn_math_max_builtin (ValueType x0, ValueType x1)
{
	if (x0 > x1)
	{
		return x0;
	}
	else
	{
		return x1;
	}
}

#endif

#if defined(CDN_MATH_RAND_REQUIRED) && !defined(CDN_MATH_RAND)
#define CDN_MATH_RAND_USE_BUILTIN
#define CDN_MATH_RAND cdn_math_rand_builtin

static ValueType
cdn_math_rand_builtin ()
{
	return (random () / (ValueType)RAND_MAX);
}

#endif

#if defined(CDN_MATH_MODULO_REQUIRED) && !defined(CDN_MATH_MODULO)
#define CDN_MATH_MODULO_USE_BUILTIN
#define CDN_MATH_MODULO cdn_math_modulo_builtin

CDN_MATH_DEFINE_PROTO_BINARY(modulo)

static ValueType
cdn_math_modulo_builtin (ValueType x0, ValueType x1)
{
	ValueType ans = CDN_MATH_VALUE_TYPE_FUNC(fmod) (x0, x1);

	if (ans < 0)
	{
		return ans + x1;
	}
	else
	{
		return ans;
	}
}

#endif
