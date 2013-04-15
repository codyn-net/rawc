#include "${basename}_run.h"

#include <cdn-rawc/integrators/cdn-rawc-integrator-${integrator_include}.h>

static CdnRawcNetwork${Name} data[CDN_RAWC_INTEGRATOR_${INTEGRATOR}_ORDER + CDN_RAWC_NETWORK_${NAME}_SPACE_FOR_EVENTS];

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
	return cdn_rawc_integrator_${integrator_type} ();
}

ValueType
cdn_rawc_${name}_get (CdnRawc${Name}State index)
{
	return data[0].data[index];
}

void
cdn_rawc_${name}_set (CdnRawc${Name}State index,
                      ValueType value)
{
	data[0].data[index] = value;
}

ValueType *
cdn_rawc_${name}_data (void)
{
	return data[0].data;
}

CdnRawcDimension const *
cdn_rawc_${name}_get_dimension (uint32_t i)
{
	CdnRawcNetwork *network;
	
	network = cdn_rawc_${name}_network ();
	
	return network->get_dimension (network->dimensions, i);
}