#ifndef __CDN_RAWC_MACROS_H__
#define __CDN_RAWC_MACROS_H__

#ifdef __cplusplus
#define CDN_RAWC_BEGIN_DECLS extern "C" {
#define CDN_RAWC_END_DECLS }
#else
#define CDN_RAWC_BEGIN_DECLS
#define CDN_RAWC_END_DECLS
#endif

// clang attribute availability macro
#ifndef __has_attribute
	#define __has_attribute(x) 0
#endif

#if (__GNUC__ >= 2 && __GNUC_MINOR__ > 96) || __has_attribute(pure)
#define GNUC_PURE __attribute__ ((pure))
#else
#define GNUC_PURE
#endif

#if defined(__GNUC__)  || __has_attribute(always_inline)
#define GNUC_INLINE __attribute__ ((always_inline))
#else
#define GNUC_INLINE
#endif

#endif /* __CDN_RAWC_MACROS_H__ */

