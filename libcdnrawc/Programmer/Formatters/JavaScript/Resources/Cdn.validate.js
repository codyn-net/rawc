#!/usr/bin/env node

var filename = process.argv[2];

var t = parseFloat(process.argv[3]);
var dt = parseFloat(process.argv[4]);
var tend = parseFloat(process.argv[5]);

var Cdn = {};
require(filename);

function validate(network)
{
	var n = new network();
	n.reset(t);

	// Start making steps
	while (t <= tend + dt)
	{
		// Write values

		// Step
		n.step(t, dt);
	}
}

for (var network in Cdn.Networks)
{
	validate(network);
	break;
}
