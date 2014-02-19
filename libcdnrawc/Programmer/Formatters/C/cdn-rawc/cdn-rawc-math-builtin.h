#if defined(CDN_MATH_SLINSOLVE_V_REQUIRED) && !defined(CDN_MATH_SLINSOLVE_V)
#define CDN_MATH_SLTDL_V_REQUIRED
#define CDN_MATH_SLTDLDINVLINVT_V_REQUIRED
#define CDN_MATH_SLTDLLINV_V_REQUIRED
#endif

#if defined(CDN_MATH_INDEX_V_REQUIRED) && !defined(CDN_MATH_INDEX_V)
#define CDN_MATH_INDEX_V_USE_BUILTIN
#define CDN_MATH_INDEX_V cdn_math_index_v_builtin


static ValueType *cdn_math_index_v_builtin (ValueType *ret,
                                            ValueType *x0,
                                            uint32_t  *indices,
                                            uint32_t   l);

static ValueType *
cdn_math_index_v_builtin (ValueType *ret,
                          ValueType *x0,
                          uint32_t  *indices,
                          uint32_t   l)
{
	uint32_t i;

	for (i = 0; i < l; ++i)
	{
		ret[i] = x0[indices[i]];
	}

	return ret;
}
#endif /* CDN_MATH_INDEX_V */

#if defined(CDN_MATH_VCAT_V_REQUIRED) && !defined(CDN_MATH_VCAT_V)
#define CDN_MATH_VCAT_V_USE_BUILTIN
#define CDN_MATH_VCAT_V cdn_math_vcat_v_builtin


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
{
	uint32_t c;
	uint32_t i1 = 0;
	uint32_t i2 = 0;
	uint32_t ptr = 0;

	for (c = 0; c < columns; ++c)
	{
		uint32_t r = 0;

		for (r = 0; r < rows1; ++r)
		{
			ret[ptr++] = x0[i1++];
		}

		for (r = 0; r < rows2; ++r)
		{
			ret[ptr++] = x1[i2++];
		}
	}

	return ret;
}
#endif /* CDN_MATH_VCAT_V */

#if defined(CDN_MATH_TRANSPOSE_REQUIRED) && !defined(CDN_MATH_TRANSPOSE)
#define CDN_MATH_TRANSPOSE_USE_BUILTIN
#define CDN_MATH_TRANSPOSE cdn_math_transpose_builtin


static ValueType cdn_math_transpose_builtin (ValueType x0);

static ValueType
cdn_math_tranpose_builtin (ValueType x0)
{
	return x0;
}
#endif /* CDN_MATH_TRANSPOSE */

#if defined(CDN_MATH_TRANSPOSE_V_REQUIRED) && !defined(CDN_MATH_TRANSPOSE_V)
#define CDN_MATH_TRANSPOSE_V_USE_BUILTIN
#define CDN_MATH_TRANSPOSE_V cdn_math_transpose_v_builtin


static ValueType *cdn_math_transpose_v_builtin (ValueType *ret,
                                                ValueType *x0,
                                                uint32_t   rows,
                                                uint32_t   columns);

static ValueType *
cdn_math_transpose_v_builtin (ValueType *ret,
                              ValueType *x0,
                              uint32_t   rows,
                              uint32_t   columns)
{
	uint32_t c;
	uint32_t i = 0;
	uint32_t ptr = 0;

	for (c = 0; c < columns; ++c)
	{
		uint32_t r;

		ptr = c;

		for (r = 0; r < rows; ++r)
		{
			ret[ptr] = x0[i++];
			ptr += columns;
		}
	}

	return ret;
}
#endif /* CDN_MATH_TRANSPOSE_V */

#if defined(CDN_MATH_MATRIX_MULTIPLY_V_REQUIRED) && !defined(CDN_MATH_MATRIX_MULTIPLY_V)
#define CDN_MATH_MATRIX_MULTIPLY_V_USE_BUILTIN
#define CDN_MATH_MATRIX_MULTIPLY_V cdn_math_matrix_multiply_v_builtin


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
{
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
}
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
{
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

	return ret;
}
#endif

static ValueType *
cdn_math_matrix_multiply_v_builtin (ValueType *ret,
                                    ValueType *x0,
                                    ValueType *x1,
                                    uint32_t   Rx0,
                                    uint32_t   Cx0,
                                    uint32_t   Cx1)
{
#ifdef ENABLE_BLAS
	return cdn_math_matrix_multiply_v_blas_builtin (ret, x0, x1, Rx0, Cx0, Cx1);
#else
	return cdn_math_matrix_multiply_v_no_blas_builtin (ret, x0, x1, Rx0, Cx0, Cx1);
#endif
}

#endif /* CDN_MATH_MATRIX_MULTIPLY_V */

#if defined(CDN_MATH_MATRIX_MULTIPLY_REQUIRED) && !defined(CDN_MATH_MATRIX_MULTIPLY)
#define CDN_MATH_MATRIX_MULTIPLY_USE_BUILTIN
#define CDN_MATH_MATRIX_MULTIPLY cdn_math_matrix_multiply_builtin


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
{
	ValueType retval;

	CDN_MATH_MATRIX_MULTIPLY_V (&retval, x0, x1, Rx0, Cx0, Cx1);
	return retval;
}


#endif /* CDN_MATH_MATRIX_MULTIPLY */

#if defined(CDN_MATH_LINSOLVE_V_REQUIRED) && !defined(CDN_MATH_LINSOLVE_V)
#define CDN_MATH_LINSOLVE_V_USE_BUILTIN
#define CDN_MATH_LINSOLVE_V cdn_math_linsolve_v_builtin


static ValueType *cdn_math_linsolve_v_builtin (ValueType *ret,
                                               ValueType *A,
                                               ValueType *b,
                                               uint32_t   RA,
                                               uint32_t   CB,
                                               int64_t   *ipiv);

#ifndef ENABLE_LAPACK
static ValueType *
cdn_math_linsolve_v_no_lapack_builtin (ValueType *ret,
                                       ValueType *A,
                                       ValueType *b,
                                       uint32_t   RA,
                                       uint32_t   CB,
                                       int64_t   *ipiv)
{
	#error("The linsolve function is not supported without LAPACK");
}
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
                                    int64_t   *ipiv)
{
	LP_ValueType *lpA = A;
	LP_ValueType *lpb = ret;
	LP_int lpRA = RA;
	LP_int lpCB = CB;
	LP_int info;
	LP_int *lpipiv = (LP_int *)ipiv;

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
}
#endif

static ValueType *
cdn_math_linsolve_v_builtin (ValueType *ret,
                             ValueType *A,
                             ValueType *b,
                             uint32_t   RA,
                             uint32_t   CB,
                             int64_t   *ipiv)
{
#ifdef ENABLE_LAPACK
	return cdn_math_linsolve_v_lapack_builtin (ret, A, b, RA, CB, ipiv);
#else
	return cdn_math_linsolve_v_no_lapack_builtin (ret, A, b, RA, CB, ipiv);
#endif
}

#endif /* CDN_MATH_LINSOLVE_V */

#if defined(CDN_MATH_QR_V_REQUIRED) && !defined(CDN_MATH_QR_V)
#define CDN_MATH_QR_V_USE_BUILTIN
#define CDN_MATH_QR_V cdn_math_qr_v_builtin


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
{
	#error("The `qr' function is not supported without LAPACK");
}
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
{
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
}
#endif

static ValueType *
cdn_math_qr_v_builtin (ValueType *ret,
                       ValueType *A,
                       uint32_t   RA,
                       uint32_t   CA,
                       ValueType *tau,
                       ValueType *work,
                       uint32_t   lwork)
{
#ifdef ENABLE_LAPACK
	return cdn_math_qr_v_lapack_builtin (ret, A, RA, CA, tau, work, lwork);
#else
	return cdn_math_qr_v_no_lapack_builtin (ret, A, RA, CA, tau, work, lwork);
#endif
}

#endif /* CDN_MATH_QR_V */

#if defined(CDN_MATH_SLTDL_V_REQUIRED) && !defined(CDN_MATH_SLTDL_V)
#define CDN_MATH_SLTDL_V_USE_BUILTIN
#define CDN_MATH_SLTDL_V cdn_math_sltdl_v_builtin


static ValueType *cdn_math_sltdl_v_builtin (ValueType *ret,
                                            ValueType *A,
                                            uint32_t   RA,
                                            ValueType *L);

static ValueType *
cdn_math_sltdl_v_builtin (ValueType *ret,
                          ValueType *A,
                          uint32_t   RA,
                          ValueType *L)
{
	int32_t k;
	int32_t n = (int32_t)RA;

	// Copy A to ret, then do the factorization in place in ret
	if (ret != A)
	{
		memcpy (ret, A, sizeof (ValueType) * n * n);
	}

	for (k = n - 1; k >= 0; --k)
	{
		int32_t i = (int32_t)L[k];
		int32_t ikk = k * (n + 1);

		while (i >= 0)
		{
			int32_t j;
			ValueType a;
			int32_t iki;

			iki = k + i * n;

			// a = A_{iki} / A_{ikk}
			a = ret[iki] / ret[ikk];

			j = i;

			while (j >= 0)
			{
				int32_t ijn = j * n;
				int32_t iij = i + ijn;
				int32_t ikj = k + ijn;

				// A_{iij} = A_{iij} - a A_{ikj}
				ret[iij] -= a * ret[ikj];

				j = (int32_t)L[j];
			}

			// H_{ki} = a
			ret[iki] = a;
			i = (int32_t)L[i];
		}
	}

	return ret;
}

#endif /* CDN_MATH_SLTDL_V */

#if defined(CDN_MATH_SLTDLDINVLINVT_V_REQUIRED) && !defined(CDN_MATH_SLTDLDINVLINVT_V)
#define CDN_MATH_SLTDLDINVLINVT_V_USE_BUILTIN
#define CDN_MATH_SLTDLDINVLINVT_V cdn_math_sltdldinvlinvt_v_builtin

static ValueType *cdn_math_sltdldinvlinvt_v_builtin (ValueType *ret,
                                                     ValueType *LTDL,
                                                     uint32_t   RLTDL,
                                                     ValueType *B,
                                                     uint32_t   CB,
                                                     ValueType *L);

static void
sltdldinvlinvt_impl (ValueType *LTDL,
                     ValueType *B,
                     ValueType *L,
                     int32_t    n)
{
	int32_t i;
	int32_t diag;

	diag = n * n - 1;

	// Solve for b = D⁻¹ L⁻ᵀ b
	// see Sparse Factorization Algorithms, page 115
	for (i = n - 1; i >= 0; --i)
	{
		int32_t j;

		j = (int32_t)L[i];

		while (j >= 0)
		{
			int32_t ij = i + j * n;

			// x_j = x_j - L_{ij} x_i
			B[j] -= LTDL[ij] * B[i];
			j = (int32_t)L[j];
		}

		// Apply D⁻¹ from the diagonal elements in ptrLTDL
		B[i] /= LTDL[diag];
		diag -= n + 1;
	}
}

static ValueType *
cdn_math_sltdldinvlinvt_v_builtin (ValueType *ret,
                                   ValueType *LTDL,
                                   uint32_t   RLTDL,
                                   ValueType *B,
                                   uint32_t   CB,
                                   ValueType *L)
{
	uint32_t k;
	ValueType *retptr;

	if (ret != B)
	{
		memcpy (ret, B, sizeof (ValueType) * RLTDL * CB);
	}

	retptr = ret;

	for (k = 0; k < CB; ++k)
	{
		sltdldinvlinvt_impl (LTDL, retptr, L, (int32_t)RLTDL);
		retptr += RLTDL;
	}

	return ret;
}

#endif /* CDN_MATH_SLTDLDINVLINVT_V */

#if defined(CDN_MATH_SLTDLLINV_V_REQUIRED) && !defined(CDN_MATH_SLTDLLINV_V)
#define CDN_MATH_SLTDLLINV_V_USE_BUILTIN
#define CDN_MATH_SLTDLLINV_V cdn_math_sltdllinv_v_builtin


static ValueType *cdn_math_sltdllinv_v_builtin (ValueType *ret,
                                                ValueType *LTDL,
                                                uint32_t   RLTDL,
                                                ValueType *B,
                                                uint32_t   CB,
                                                ValueType *L);

static void
sltdllinv_impl (ValueType *LTDL,
                ValueType *B,
                ValueType *L,
                int32_t    n)
{
	int32_t i;

	// Then finally solve for L^-1 b
	// see Sparse Factorization Algorithms, page 115
	for (i = 0; i < n; ++i)
	{
		int32_t j;

		j = (int32_t)L[i];

		while (j >= 0)
		{
			int32_t ij = i + j * n;

			// x_i = x_i - L_{ij} x_j
			B[i] -= LTDL[ij] * B[j];
			j = (int32_t)L[j];
		}
	}
}

static ValueType *
cdn_math_sltdllinv_v_builtin (ValueType *ret,
                              ValueType *LTDL,
                              uint32_t   RLTDL,
                              ValueType *B,
                              uint32_t   CB,
                              ValueType *L)
{
	uint32_t i;
	ValueType *retptr;

	if (ret != B)
	{
		memcpy (ret, B, sizeof (ValueType) * RLTDL * CB);
	}

	retptr = ret;

	for (i = 0; i < CB; ++i)
	{
		sltdllinv_impl (LTDL, retptr, L, (int32_t)RLTDL);
		retptr += RLTDL;
	}

	return ret;
}

#endif /* CDN_MATH_SLTDLLINV_V */

#if defined(CDN_MATH_SLTDLLINVT_V_REQUIRED) && !defined(CDN_MATH_SLTDLLINVT_V)
#define CDN_MATH_SLTDLLINVT_V_USE_BUILTIN
#define CDN_MATH_SLTDLLINVT_V cdn_math_sltdllinvt_v_builtin


static ValueType *cdn_math_sltdllinvt_v_builtin (ValueType *ret,
                                                 ValueType *LTDL,
                                                 uint32_t   RLTDL,
                                                 ValueType *B,
                                                 uint32_t   CB,
                                                 ValueType *L);

static void
sltdllinvt_impl (ValueType *LTDL,
                 ValueType *B,
                 ValueType *L,
                 int32_t    n)
{
	int32_t i;

	// Then finally solve for L^-T b
	// see Sparse Factorization Algorithms, page 115
	for (i = n - 1; i >= 0; --i)
	{
		int32_t j;

		j = (int32_t)L[i];

		while (j >= 0)
		{
			int32_t ij = i + j * n;

			// x_i = x_i - L_{ij} x_i
			B[j] -= LTDL[ij] * B[i];
			j = (int32_t)L[j];
		}
	}
}

static ValueType *
cdn_math_sltdllinvt_v_builtin (ValueType *ret,
                               ValueType *LTDL,
                               uint32_t   RLTDL,
                               ValueType *B,
                               uint32_t   CB,
                               ValueType *L)
{
	uint32_t i;
	ValueType *retptr;

	if (ret != B)
	{
		memcpy (ret, B, sizeof (ValueType) * RLTDL * CB);
	}

	retptr = ret;

	for (i = 0; i < CB; ++i)
	{
		sltdllinvt_impl (LTDL, retptr, L, (int32_t)RLTDL);
		retptr += RLTDL;
	}

	return ret;
}

#endif /* CDN_MATH_SLTDLLINVT_V */

#if defined(CDN_MATH_SLTDLDINV_V_REQUIRED) && !defined(CDN_MATH_SLTDLDINV_V)
#define CDN_MATH_SLTDLDINV_V_USE_BUILTIN
#define CDN_MATH_SLTDLDINV_V cdn_math_sltdldinv_v_builtin


static ValueType *cdn_math_sltdldinv_v_builtin (ValueType *ret,
                                                ValueType *LTDL,
                                                uint32_t   RLTDL,
                                                ValueType *B,
                                                uint32_t   CB);

static void
sltdldinv_impl (ValueType *LTDL,
                ValueType *B,
                int32_t    n)
{
	int32_t i;
	int32_t diag;

	diag = n * n - 1;

	// First solve for b = D^-1 b
	// see Sparse Factorization Algorithms, page 115
	for (i = n - 1; i >= 0; --i)
	{
		// Apply D-1 from the diagonal elements if ptrA
		B[i] /= LTDL[diag];
		diag -= n + 1;
	}
}

static ValueType *
cdn_math_sltdldinv_v_builtin (ValueType *ret,
                              ValueType *LTDL,
                              uint32_t   RLTDL,
                              ValueType *B,
                              uint32_t   CB)
{
	uint32_t i;
	ValueType *retptr;

	if (ret != B)
	{
		memcpy (ret, B, sizeof (ValueType) * RLTDL * CB);
	}

	retptr = ret;

	for (i = 0; i < CB; ++i)
	{
		sltdldinv_impl (LTDL, retptr, (int32_t)RLTDL);
		retptr += RLTDL;
	}

	return ret;
}

#endif /* CDN_MATH_SLTDLDINV_V */

#if defined(CDN_MATH_SLINSOLVE_V_REQUIRED) && !defined(CDN_MATH_SLINSOLVE_V)
#define CDN_MATH_SLINSOLVE_V_USE_BUILTIN
#define CDN_MATH_SLINSOLVE_V cdn_math_slinsolve_v_builtin


static ValueType *cdn_math_slinsolve_v_builtin (ValueType *ret,
                                                ValueType *A,
                                                uint32_t   RA,
                                                ValueType *b,
                                                uint32_t   CB,
                                                ValueType *L,
                                                ValueType *LTDL);

static ValueType *
cdn_math_slinsolve_v_builtin (ValueType *ret,
                              ValueType *A,
                              uint32_t   RA,
                              ValueType *B,
                              uint32_t   CB,
                              ValueType *L,
                              ValueType *LTDL)
{
	memcpy (LTDL, A, sizeof (ValueType) * RA * RA);
	memcpy (ret, B, sizeof (ValueType) * RA * CB);

	// Factorize A into LTDL in place using LTDL factorization
	cdn_math_sltdl_v_builtin (LTDL, LTDL, RA, L);

	// then compute b = D^-1 L^-T b
	cdn_math_sltdldinvlinvt_v_builtin (ret, LTDL, RA, ret, CB, L);

	// finally compute b = L^-1 b
	cdn_math_sltdllinv_v_builtin (ret, LTDL, RA, ret, CB, L);

	return ret;
}

#endif /* CDN_MATH_SLINSOLVE_V */

#if defined(CDN_MATH_INVERSE_V_REQUIRED) && !defined(CDN_MATH_INVERSE_V)
#define CDN_MATH_INVERSE_V_USE_BUILTIN
#define CDN_MATH_INVERSE_V cdn_math_inverse_v_builtin


static ValueType *cdn_math_inverse_v_builtin (ValueType *ret,
                                              ValueType *A,
                                              uint32_t   RA,
                                              int64_t   *ipiv,
                                              ValueType *work,
                                              int32_t    lwork);

#ifndef ENABLE_LAPACK
static ValueType *
cdn_math_inverse_v_no_lapack_builtin (ValueType *ret,
                                      ValueType *A,
                                      uint32_t   RA,
                                      int64_t   *ipiv,
                                      ValueType *work,
                                      int32_t    lwork)
{
	#error("The inv function is not supported without LAPACK");
}
#else

#define fdgetrf_ sgetrf_
#define fdgetri_ sgetri_

static ValueType *
cdn_math_inverse_v_lapack_builtin (ValueType *ret,
                                   ValueType *A,
                                   uint32_t   RA,
                                   int64_t   *ipiv,
                                   ValueType *work,
                                   int32_t    lwork)
{
	LP_ValueType *lpA = ret;
	LP_int lpRA = RA;
	LP_int *lpipiv = (LP_int *)ipiv;
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
}
#endif

static ValueType *
cdn_math_inverse_v_builtin (ValueType *ret,
                             ValueType *A,
                             uint32_t   RA,
                             int64_t   *ipiv,
                             ValueType *work,
                             int32_t    lwork)
{
#ifdef ENABLE_LAPACK
	return cdn_math_inverse_v_lapack_builtin (ret, A, RA, ipiv, work, lwork);
#else
	return cdn_math_inverse_v_no_lapack_builtin (ret, A, RA, ipiv, work, lwork);
#endif
}

#endif /* CDN_MATH_INVERSE_V */

#if defined(CDN_MATH_PSEUDOINVERSE_V_REQUIRED) && !defined(CDN_MATH_PSEUDOINVERSE_V)
#define CDN_MATH_PSEUDOINVERSE_V_USE_BUILTIN
#define CDN_MATH_PSEUDOINVERSE_V cdn_math_pseudoinverse_v_builtin


static ValueType *cdn_math_pseudoinverse_v_builtin (ValueType *ret,
                                                     ValueType *A,
                                                     uint32_t   RA,
                                                     uint32_t   CA,
                                                     ValueType *B,
                                                     uint32_t   RB,
                                                     ValueType *S,
                                                     ValueType *work,
                                                     uint32_t   lwork,
                                                     int64_t   *iwork);

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
                                             int64_t   *iwork)
{
	#error("The pinv function is not supported without LAPACK");
}
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
                                          int64_t   *iwork)
{
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
	LP_int       *lpiwork = (LP_int *)iwork;
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
	{
		memcpy (retptr, B, sizeof (ValueType) * CA);

		retptr += CA;
		B += RB;
	}

	return ret;
}
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
                                  int64_t   *iwork)
{
#ifdef ENABLE_LAPACK
	return cdn_math_pseudoinverse_v_lapack_builtin (ret, A, RA, CA, B, RB, S, work, lwork, iwork);
#else
	return cdn_math_pseudoinverse_v_no_lapack_builtin (ret, A, RA, CA, B, RB, S, work, lwork, iwork);
#endif
}

#endif /* CDN_MATH_PSEUDOINVERSE_V */

#if defined(CDN_MATH_DIAG_REQUIRED) && !defined(CDN_MATH_DIAG)
#define CDN_MATH_DIAG_USE_BUILTIN
#define CDN_MATH_DIAG cdn_math_diag_builtin


static void cdn_math_diag_builtin (ValueType x0);

static void
cdn_math_diag_builtin (ValueType x0)
{
	return x0;
}

#endif /* CDN_MATH_DIAG */

#if defined(CDN_MATH_DIAG_V_M_REQUIRED) && !defined(CDN_MATH_DIAG_V_M)
#define CDN_MATH_DIAG_V_M_USE_BUILTIN
#define CDN_MATH_DIAG_V_M cdn_math_diag_v_m_builtin


static ValueType *cdn_math_diag_v_m_builtin (ValueType *ret,
                                             ValueType *x0,
                                             uint32_t   n);

static ValueType *
cdn_math_diag_v_m_builtin (ValueType *ret,
                           ValueType *x0,
                           uint32_t   n)
{
	uint32_t i;

	for (i = 0; i < n; ++i)
	{
		ret[i] = *x0;
		x0 += n + 1;
	}

	return ret;
}
#endif /* CDN_MATH_DIAG_V_M */

#if defined(CDN_MATH_DIAG_V_V_REQUIRED) && !defined(CDN_MATH_DIAG_V_V)
#define CDN_MATH_DIAG_V_V_USE_BUILTIN
#define CDN_MATH_DIAG_V_V cdn_math_diag_v_v_builtin


static ValueType *cdn_math_diag_v_v_builtin (ValueType *ret,
                                             ValueType *x0,
                                             uint32_t   n);

static ValueType *
cdn_math_diag_v_v_builtin (ValueType *ret,
                           ValueType *x0,
                           uint32_t   n)
{
	uint32_t i;
	ValueType *retptr = ret;

	for (i = 0; i < n; ++i)
	{
		*retptr = x0[i];
		retptr += n + 1;
	}

	return ret;
}
#endif /* CDN_MATH_DIAG_V_V */

#if defined(CDN_MATH_TRIL_V_REQUIRED) && !defined(CDN_MATH_TRIL_V)
#define CDN_MATH_TRIL_V_USE_BUILTIN
#define CDN_MATH_TRIL_V cdn_math_tril_v_builtin


static ValueType *cdn_math_tril_v_builtin (ValueType *ret,
                                           ValueType *x0,
                                           uint32_t   rows,
                                           uint32_t   columns);

static ValueType *
cdn_math_tril_v_builtin (ValueType *ret,
                         ValueType *x0,
                         uint32_t   rows,
                         uint32_t   columns)
{
	uint32_t c;
	ValueType *retptr = ret;

	for (c = 0; c < columns && c < rows; ++c)
	{
		memcpy (retptr, x0, sizeof (ValueType) * (rows - c));

		retptr += rows + 1;
		x0 += rows + 1;
	}

	return ret;
}
#endif /* CDN_MATH_TRIL_V */

#if defined(CDN_MATH_TRIU_V_REQUIRED) && !defined(CDN_MATH_TRIU_V)
#define CDN_MATH_TRIU_V_USE_BUILTIN
#define CDN_MATH_TRIU_V cdn_math_triu_v_builtin


static ValueType *cdn_math_triu_v_builtin (ValueType *ret,
                                           ValueType *x0,
                                           uint32_t   rows,
                                           uint32_t   columns);

static ValueType *
cdn_math_triu_v_builtin (ValueType *ret,
                         ValueType *x0,
                         uint32_t   rows,
                         uint32_t   columns)
{
	uint32_t c;
	ValueType *retptr = ret;

	for (c = 1; c <= columns; ++c)
	{
		memcpy (retptr, x0, sizeof (ValueType) * (c > rows ? rows : c));

		retptr += rows;
		x0 += rows;
	}

	return ret;
}
#endif /* CDN_MATH_TRIU_V */

#if defined(CDN_MATH_CSUM_V_REQUIRED) && !defined(CDN_MATH_CSUM_V)
#define CDN_MATH_CSUM_V_USE_BUILTIN
#define CDN_MATH_CSUM_V cdn_math_csum_v_builtin


static ValueType *cdn_math_csum_v_builtin (ValueType *ret,
                                           ValueType *x0,
                                           uint32_t   rows,
                                           uint32_t   columns);

static ValueType *
cdn_math_csum_v_builtin (ValueType *ret,
                         ValueType *x0,
                         uint32_t   rows,
                         uint32_t   columns)
{
	uint32_t c;

	if (columns == 1)
	{
		memcpy (ret, x0, sizeof(ValueType) * rows);
		return ret;
	}

	for (c = 0; c < columns; ++c)
	{
		uint32_t r;

		for (r = 0; r < rows; ++r)
		{
			if (c == 0)
			{
				ret[r] = *x0++;
			}
			else
			{
				ret[r] += *x0++;
			}
		}
	}

	return ret;
}
#endif /* CDN_MATH_CSUM_V */

#if defined(CDN_MATH_RSUM_V_REQUIRED) && !defined(CDN_MATH_RSUM_V)
#define CDN_MATH_RSUM_V_USE_BUILTIN
#define CDN_MATH_RSUM_V cdn_math_rsum_v_builtin


static ValueType *cdn_math_rsum_v_builtin (ValueType *ret,
                                           ValueType *x0,
                                           uint32_t   rows,
                                           uint32_t   columns);

static ValueType *
cdn_math_rsum_v_builtin (ValueType *ret,
                         ValueType *x0,
                         uint32_t   rows,
                         uint32_t   columns)
{
	uint32_t c;

	if (rows == 1)
	{
		memcpy (ret, x0, sizeof(ValueType) * columns);
		return ret;
	}

	for (c = 0; c < columns; ++c)
	{
		uint32_t r;

		ret[c] = 0;

		for (r = 0; r < rows; ++r)
		{
			ret[c] += *x0++;
		}
	}

	return ret;
}
#endif /* CDN_MATH_RSUM_V */
