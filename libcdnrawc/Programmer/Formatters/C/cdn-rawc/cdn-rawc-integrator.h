#ifndef __CDN_RAWC_INTEGRATOR_H__
#define __CDN_RAWC_INTEGRATOR_H__

#include <cdn-rawc/cdn-rawc-types.h>
#include <cdn-rawc/cdn-rawc-macros.h>

CDN_RAWC_BEGIN_DECLS

typedef enum
{
	CDN_RAWC_INTEGRATOR_EVENT_RESULT_OK,
	CDN_RAWC_INTEGRATOR_EVENT_RESULT_REFINE,
} CdnRawcIntegratorEventResult;

void cdn_rawc_integrator_run (CdnRawcIntegrator *integrator,
                              CdnRawcNetwork    *network,
                              void              *data,
                              ValueType          from,
                              ValueType          step,
                              ValueType          to);

void cdn_rawc_integrator_step (CdnRawcIntegrator *integrator,
                               CdnRawcNetwork    *network,
                               void              *data,
                               ValueType          t,
                               ValueType          dt);

void cdn_rawc_integrator_step_diff (CdnRawcIntegrator *integrator,
                                    CdnRawcNetwork    *network,
                                    void              *data,
                                    ValueType          t,
                                    ValueType          dt);

CdnRawcIntegratorEventResult
cdn_rawc_integrator_process_events (CdnRawcIntegrator *integrator,
                                    CdnRawcNetwork    *network,
                                    void              *data,
                                    ValueType          t,
                                    ValueType         *dt);

CDN_RAWC_END_DECLS

#endif /* __CDN_RAWC_INTEGRATOR_H__ */

