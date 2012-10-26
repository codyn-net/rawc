#ifndef __CDN_RAWC_INTEGRATOR_EULER_H__
#define __CDN_RAWC_INTEGRATOR_EULER_H__

#include <cdn-rawc/cdn-rawc-types.h>

CDN_RAWC_BEGIN_DECLS

typedef struct
{
	CdnRawcIntegrator integrator;
} CdnRawcIntegratorEuler;

#define CDN_RAWC_INTEGRATOR_EULER_DATA_SIZE 1

CdnRawcIntegrator *cdn_rawc_integrator_euler (void);

CDN_RAWC_END_DECLS

#endif /* __CDN_RAWC_INTEGRATOR_EULER_H__ */

