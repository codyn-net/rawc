#include "cdn-rawc-integrator-euler.h"
#include <stdio.h>

static void diff (CdnRawcIntegrator *integrator,
                  CdnRawcNetwork    *network,
                  void              *data,
                  ValueType          t,
                  ValueType          dt);

static CdnRawcIntegratorEuler integrator_class = {
	{
		0,
		diff,
		CDN_RAWC_INTEGRATOR_EULER_ORDER
	}
};

CdnRawcIntegrator *
cdn_rawc_integrator_euler ()
{
	return (CdnRawcIntegrator *)&integrator_class;
}

static void
diff (CdnRawcIntegrator *integrator,
      CdnRawcNetwork    *network,
      void              *data,
      ValueType          t,
      ValueType          dt)
{
	ValueType *states;
	ValueType *derivatives;
	uint32_t i;
	uint32_t num;

	states = network->get_states (data);
	derivatives = network->get_derivatives (data);

	num = network->states.end - network->states.start;

	for (i = 0; i < num; ++i)
	{
		states[i] = states[i] + derivatives[i] * dt;
	}
}
