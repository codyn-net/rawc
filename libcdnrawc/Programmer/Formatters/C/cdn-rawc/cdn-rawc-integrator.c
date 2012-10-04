#include "cdn-rawc-integrator.h"

void
cdn_rawc_integrator_step (CdnRawcIntegrator *integrator,
                          CdnRawcNetwork    *network,
                          ValueType         *data,
                          ValueType          t,
                          ValueType          dt)
{
	if (integrator && integrator->step)
	{
		integrator->step (integrator, network, data, t, dt);
		return;
	}

	// Precompute step
	network->pre (data, t, dt);

	// Compute diff
	network->diff (data, t, dt);

	cdn_rawc_integrator_step_diff (integrator,
	                               network,
	                               data,
	                               t,
	                               dt);
}

void
cdn_rawc_integrator_step_diff (CdnRawcIntegrator *integrator,
                               CdnRawcNetwork    *network,
                               ValueType         *data,
                               ValueType          t,
                               ValueType          dt)
{
	if (integrator && integrator->diff)
	{
		integrator->diff (integrator, network, data, t, dt);
	}

	network->post (data, t + dt, dt);
}
