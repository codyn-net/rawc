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

#define CDN_EPSILON_double DBL_EPSILON
#define CDN_EPSILON_float  FLT_EPSILON

#define CDN_EPSILON_REAL_ONE_MORE(ValueType) CDN_EPSILON_##ValueType
#define CDN_EPSILON_REAL(ValueType) CDN_EPSILON_REAL_ONE_MORE(ValueType)
#define CDN_EPSILON CDN_EPSILON_REAL(ValueType)

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

