if (!Cdn.Integrators)
{
(function(Cdn) {
	Cdn.Integrators = {}

	Cdn.Integrator = function() {
	};

	Cdn.Integrator.Extend = function(f) {
		var ret = f;

		for (var item in Cdn.Integrator.prototype)
		{
			ret.prototype[item] = Cdn.Integrator.prototype[item];
		}

		return ret;
	};

	Cdn.Integrator.prototype.step = function(network, t, dt) {
		// Precompute step
		network.pre(t, dt);

		var stored_state = null;

		if (network.constructor.event_refinement)
		{
			// Store state
			stored_state = network.data().slice(0);
		}

		while (true)
		{
			// Compute the derivatives
			network.diff(t, dt);

			// Run specific integrator step diff
			this.step_diff(network, t, dt);

			// Check if any events have occurred
			var ndt = this.process_events(network, dt);

			if (ndt === null)
			{
				// No events to be processed
				break;
			}

			// Copy state back and try again with ndt as dt
			network.data(stored_state);
			dt = ndt;
		}
	};

	Cdn.Integrator.prototype.step_diff = function(network, t, dt) {
		this.diff(network, t, dt);
		network.post(t + dt, dt);
	};

	Cdn.Integrator.prototype.diff = function(network, t, dt) {
	};

	Cdn.Integrator.prototype.process_events = function (network, t, dt) {
		network.events_update();

		var num = network.events_active_size();

		if (num == 0)
		{
			network.events_post_update();
			return null;
		}

		var event = network.events_active(0);
		var event_value = network.events_value(event);

		if (dt < network.constructor.minimum_timestep ||
		    event_value.distance >= (1 - 1e-9))
		{
			network.events_fire();
			network.events_post_update();
			return null;
		}

		var ndt = event_value.distance * dt;

		if (ndt < network.minimum_timestep)
		{
			ndt = network.minimum_timestep;
		}

		return ndt;
	};
})(Cdn);
}
