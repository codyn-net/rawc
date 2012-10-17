#include "${basename}_run.h"

#include <cdn-rawc/integrators/cdn-rawc-integrator-${integrator}.h>

static ValueType data[CDN_RAWC_${NAME}_DATA_SIZE * CDN_RAWC_INTEGRATOR_${INTEGRATOR}_DATA_SIZE];

void
cdn_rawc_${name}_reset (ValueType t)
{
	CdnRawcNetwork *network;

	network = cdn_rawc_${name}_network ();

	network->reset (data, t);
}

void
cdn_rawc_${name}_init (ValueType t)
{
	CdnRawcNetwork *network;

	network = cdn_rawc_${name}_network ();

	network->init (data, t);
}

void
cdn_rawc_${name}_prepare (ValueType t)
{
	CdnRawcNetwork *network;

	network = cdn_rawc_${name}_network ();

	network->prepare (data, t);
}

void
cdn_rawc_${name}_step (ValueType t, ValueType dt)
{
	CdnRawcNetwork *network;
	CdnRawcIntegrator *integrator;

	network = cdn_rawc_${name}_network ();
	integrator = cdn_rawc_${name}_integrator ();

	cdn_rawc_integrator_step (integrator,
	                          network,
	                          data,
	                          t,
	                          dt);
}

CdnRawcIntegrator *
cdn_rawc_${name}_integrator (void)
{
	return cdn_rawc_integrator_${integrator} ();
}

ValueType
cdn_rawc_${name}_get (CdnRawc${Name}State index)
{
	return data[index];
}

void
cdn_rawc_${name}_set (CdnRawc${Name}State index,
                      ValueType value)
{
	data[index] = value;
}

ValueType *
cdn_rawc_${name}_data (void)
{
	return data;
}
