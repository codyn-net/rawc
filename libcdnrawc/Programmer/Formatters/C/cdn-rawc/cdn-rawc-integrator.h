#ifndef __CDN_RAWC_INTEGRATOR_H__
#define __CDN_RAWC_INTEGRATOR_H__

#include <cdn-rawc/cdn-rawc-types.h>

CDN_RAWC_BEGIN_DECLS

void cdn_rawc_integrator_step (CdnRawcIntegrator *integrator,
                               CdnRawcNetwork    *network,
                               ValueType         *data,
                               ValueType          t,
                               ValueType          dt);

void cdn_rawc_integrator_step_diff (CdnRawcIntegrator *integrator,
                                    CdnRawcNetwork    *network,
                                    ValueType         *data,
                                    ValueType          t,
                                    ValueType          dt);

CDN_RAWC_END_DECLS

#endif /* __CDN_RAWC_INTEGRATOR_H__ */

