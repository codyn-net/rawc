#include "cdn-rawc-integrator-euler.h"

static void diff (CdnRawcIntegrator *integrator,
                  CdnRawcNetwork    *network,
                  ValueType         *data,
                  ValueType          t,
                  ValueType          dt);

static CdnRawcIntegratorEuler integrator_class = {
	{
		0,
		diff,
		CDN_RAWC_INTEGRATOR_EULER_DATA_SIZE
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
      ValueType         *data,
      ValueType          t,
      ValueType          dt)
{
	ValueType *states;
	ValueType *derivatives;
	uint32_t i;
	uint32_t num;

	states = data + network->states.start;
	derivatives = data + network->derivatives.start;

	num = network->states.end - network->states.start;

	for (i = 0; i < num; ++i)
	{
		states[i] += dt * derivatives[i];
	}
}
