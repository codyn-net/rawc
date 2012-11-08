#include "cdn-rawc-integrator.h"
#include <string.h>

void
cdn_rawc_integrator_step (CdnRawcIntegrator *integrator,
                          CdnRawcNetwork    *network,
                          void              *data,
                          ValueType          t,
                          ValueType          dt)
{
	ValueType *current_state = 0;
	ValueType *stored_state = 0;

	if (integrator && integrator->step)
	{
		integrator->step (integrator, network, data, t, dt);
		return;
	}

	// Precompute step
	network->pre (data, t, dt);

	if (network->event_refinement)
	{
		// Store state in last data
		void *storage = network->get_nth (data, integrator->order);

		current_state = network->get_data (data);
		stored_state = network->get_data (storage);

		memcpy (stored_state, current_state, network->data_size);
	}

	while (1)
	{
		// Compute diff
		network->diff (data, t, dt);

		cdn_rawc_integrator_step_diff (integrator,
		                               network,
		                               data,
		                               t,
		                               dt);

		// Check for events
		if (cdn_rawc_integrator_process_events (integrator,
		                                        network,
		                                        data,
		                                        &dt) == CDN_RAWC_INTEGRATOR_EVENT_RESULT_OK)
		{
			break;
		}
		else
		{
			memcpy (current_state,
			        stored_state,
			        network->data_size);
		}
	}
}

CdnRawcIntegratorEventResult
cdn_rawc_integrator_process_events (CdnRawcIntegrator *integrator,
                                    CdnRawcNetwork    *network,
                                    void              *data,
                                    ValueType         *dt)
{
	uint32_t num;
	uint32_t event;
	CdnRawcEventValue *event_value;

	network->events_update (data);

	num = network->get_events_active_size (data);

	if (num == 0)
	{
		network->events_post_update (data);
		return CDN_RAWC_INTEGRATOR_EVENT_RESULT_OK;
	}

	event = network->get_events_active (data, 0);
	event_value = network->get_events_value (data, event);

	if (*dt <= network->minimum_timestep ||
	    event_value->distance >= (1 - 1e-9))
	{
		// Fire all events
		uint32_t i;

		network->events_fire (data);

		network->events_post_update (data);
		return CDN_RAWC_INTEGRATOR_EVENT_RESULT_OK;
	}

	*dt = event_value->distance * *dt;

	if (*dt < network->minimum_timestep)
	{
		*dt = network->minimum_timestep;
	}

	return CDN_RAWC_INTEGRATOR_EVENT_RESULT_REFINE;
}

void
cdn_rawc_integrator_step_diff (CdnRawcIntegrator *integrator,
                               CdnRawcNetwork    *network,
                               void              *data,
                               ValueType          t,
                               ValueType          dt)
{
	if (integrator && integrator->diff)
	{
		integrator->diff (integrator, network, data, t, dt);
	}

	network->post (data, t + dt, dt);
}
