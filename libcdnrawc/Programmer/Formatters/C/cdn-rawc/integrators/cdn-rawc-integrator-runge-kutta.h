#ifndef __CDN_RAWC_INTEGRATOR_RUNGE_KUTTA_H__
#define __CDN_RAWC_INTEGRATOR_RUNGE_KUTTA_H__

#include <cdn-rawc/cdn-rawc-integrator.h>

CDN_RAWC_BEGIN_DECLS

typedef struct
{
	CdnRawcIntegrator integrator;
} CdnRawcIntegratorRungeKutta;

#define CDN_RAWC_INTEGRATOR_RUNGE_KUTTA_ORDER 4

CdnRawcIntegrator *cdn_rawc_integrator_runge_kutta (void);

CDN_RAWC_END_DECLS

#endif /* __CDN_RAWC_INTEGRATOR_RUNGE_KUTTA_H__ */

