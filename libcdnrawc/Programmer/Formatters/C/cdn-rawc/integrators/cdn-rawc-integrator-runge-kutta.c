#include "cdn-rawc-integrator-runge-kutta.h"

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

static CdnRawcIntegratorRungeKutta integrator = {
	{
		step,
		diff
	}
};

CdnRawcIntegrator *
cdn_rawc_integrator_runge_kutta ()
{
	return (CdnRawcIntegrator *)&integrator;
}

static void
update (CdnRawcNetwork *network,
        double         *data,
        uint32_t        order,
        double          norm)
{
	uint32_t i;
	uint32_t offset;
	uint32_t num;

	double *states;
	double *stateswr;
	double *derivatives;

	offset = (order - 1) * network->data_size;

	states = data + network->states.start;
	derivatives = data + network->derivatives.start + offset;

	stateswr = data + network->states.start + offset + network->data_size;

	num = network->states.end - network->states.start;

	for (i = 0; i < num; ++i)
	{
		stateswr[i] = states[i] + norm * derivatives[i];
	}
}
static void
step (CdnRawcIntegrator *integrator,
      CdnRawcNetwork    *network,
      double            *data,
      double             t,
      double             dt)
{
	double hdt = 0.5 * dt;

	// Precompute step
	network->pre (data, t, hdt);

	// Compute first diff
	network->diff (data, t, hdt);

	diff (integrator, network, data, t, dt);
}

static void
diff (CdnRawcIntegrator *integrator,
      CdnRawcNetwork    *network,
      double            *data,
      double             t,
      double             dt)
{
	uint32_t i;
	double *states;
	double *stateswr;
	double *derivatives;
	double hdt;

	states = data + network->states.start;
	derivatives = data + network->derivatives.start;

	hdt = 0.5 *dt;

	// K1: dy_1 = df(y_0, t, 0.5 * dt)
	//      y_1 = y_0 + dy_1 * 0.5 * dt
	// note first diff is already computed here
	update (network, data, 1, hdt);

	// K2: dy_2 = df(y_1, t + 0.5 * dt, 0.5 * dt)
	//      y_2 = y_0 + dy_2 * 0.5 * dt
	network->diff (data + network->data_size, t + hdt, hdt);
	update (network, data, 2, hdt);

	// K3: dy_3 = df(y_2, t + 0.5 * dt, 0.5 * dt)
	//      y_3 = y_0 + dy_3 * dt
	network->diff (data, t + hdt, hdt);
	update (network, data, 3, dt);

	// K4: dy_4 = df(y_3, t + dt, dt)
	network->diff (data, t + dt, dt);

	// TODO stuff

	network->post (data, t + dt, dt);
}
