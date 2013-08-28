if (!('euler' in Cdn.Integrators)
{

(function(Cdn) {
	// Define euler integrator
	Cdn.Integrators['euler'] = Cdn.Integrator.Extend(function() {});

	Cdn.Integrators['euler'].prototype.diff = function(network, t, dt) {
		var states = network.constructor.states;
		var derivatives = network.constructor.derivatives;

		var num = states.end - states.start;
		var data = network.data();

		var state = states.start;
		var derivative = derivatives.start;

		for (var i = 0; i < num; ++i)
		{
			var dim = network.constructor.dimensions[state];

			if (dim.size == 1)
			{
				data[state] += dt * data[derivative];
			}
			else
			{
				for (var j = 0; j < dim.size; ++j)
				{
					data[state][j] += dt * data[derivative][j];
				}
			}
		}
	};
})(Cdn);

};
