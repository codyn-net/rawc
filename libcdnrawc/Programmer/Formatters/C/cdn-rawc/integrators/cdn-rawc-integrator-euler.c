#include "cdn-rawc-integrator-euler.h"

static void diff (CdnRawcIntegrator *integrator,
                  CdnRawcNetwork    *network,
                  double            *data,
                  double             t,
                  double             dt);

static void step (CdnRawcIntegrator *integrator,
                  CdnRawcNetwork    *network,
                  double            *data,
                  double             t,
                  double             dt);

static CdnRawcIntegratorEuler integrator = {
	{
		0,
		diff
	}
};

CdnRawcIntegrator *
cdn_rawc_integrator_euler ()
{
	return (CdnRawcIntegrator *)&integrator;
}

static void
diff (CdnRawcIntegrator *integrator,
      CdnRawcNetwork    *network,
      double            *data,
      double             t,
      double             dt)
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