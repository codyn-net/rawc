#include "cdn-rawc-integrator-runge-kutta.h"
#include <string.h>

static void diff (CdnRawcIntegrator *integrator,
                  CdnRawcNetwork    *network,
                  void              *data,
                  ValueType          t,
                  ValueType          dt);

static CdnRawcIntegratorRungeKutta integrator_class = {
	{
		NULL,
		diff,
		CDN_RAWC_INTEGRATOR_RUNGE_KUTTA_ORDER
	}
};

CdnRawcIntegrator *
cdn_rawc_integrator_runge_kutta ()
{
	return (CdnRawcIntegrator *)&integrator_class;
}

static void
update (CdnRawcNetwork    *network,
        void              *data,
        uint32_t           n,
        double             factor)
{
	ValueType *current_state;
	ValueType *current_deriv;
	ValueType *next_deriv;
	ValueType *prev_deriv;
	ValueType *stored_state;
	uint32_t num;
	uint32_t i;

	num = network->states.end - network->states.start;

	if (num == 0)
	{
		return;
	}

	current_state = network->get_states (data);
	current_deriv = network->get_derivatives (data);

	// Original states are always stored in the first extra data segment
	stored_state = network->get_states (network->get_nth (data, 1));

	next_deriv = network->get_derivatives (network->get_nth (data, n + 1));
	prev_deriv = network->get_derivatives (network->get_nth (data, n));

	for (i = 0; i < num; ++i)
	{
		// Store state
		next_deriv[i] = current_deriv[i];

		// Prepare next state
		current_state[i] = stored_state[i] + factor * current_deriv[i];
	}
}

static void
update_total (CdnRawcNetwork *network,
              void           *data,
              ValueType       dt)
{
	ValueType *current_state;
	ValueType *current_deriv;
	ValueType *stored_state;
	ValueType *k1;
	ValueType *k2;
	ValueType *k3;
	ValueType *k4;
	uint32_t num;
	uint32_t i;
	double f1;
	double f2;

	num = network->states.end - network->states.start;

	if (num == 0)
	{
		return;
	}

	current_state = network->get_states (data);
	current_deriv = network->get_derivatives (data);

	// Original states are always stored in the first extra data segment
	stored_state = network->get_states (network->get_nth (data, 1));

	k1 = network->get_derivatives (network->get_nth (data, 1));
	k2 = network->get_derivatives (network->get_nth (data, 2));
	k3 = network->get_derivatives (network->get_nth (data, 3));
	k4 = current_deriv;

	f1 = dt / 6.0;
	f2 = dt / 3.0;

	for (i = 0; i < num; ++i)
	{
		current_state[i] = stored_state[i] +
		                   f1 * k1[i] +
		                   f2 * k2[i] +
		                   f2 * k3[i] +
		                   f1 * k4[i];
	}
}

static void
diff (CdnRawcIntegrator *integrator,
      CdnRawcNetwork    *network,
      void              *data,
      ValueType          t,
      ValueType          dt)
{
	uint32_t i;
	double hdt = 0.5 * dt;

	// First, store original states in the next data segment
	memcpy (network->get_states (network->get_nth (data, 1)),
	        network->get_states (data),
	        sizeof(ValueType) * (network->states.end - network->states.start));

	// Then, store derivatives, K1, which are already computed before this
	// function is called (see cdn_rawc_integrator_step) and update
	// current states for the next diff
	update (network, data, 0, hdt);

	// Calculate next diff, K2
	network->prediff (data);
	network->diff (data, t + hdt, hdt);

	// Store derivatives for K2
	update (network, data, 1, hdt);

	// Calculate next diff, K3
	network->prediff (data);
	network->diff (data, t + hdt, hdt);

	// Store derivatives for K3
	update (network, data, 2, dt);

	// Calculate next diff, K4
	network->prediff (data);
	network->diff (data, t + dt, hdt);

	// Update total derivative
	update_total (network, data, dt);
}
