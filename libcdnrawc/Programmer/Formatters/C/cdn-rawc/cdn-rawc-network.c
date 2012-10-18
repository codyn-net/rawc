#include "cdn-rawc-network.h"

#include <stdlib.h>

void
cdn_rawc_network_prepare (CdnRawcNetwork *network,
                          ValueType      *data,
                          ValueType       t)
{
	network->prepare (data, t);
}

void
cdn_rawc_network_init (CdnRawcNetwork *network,
                       ValueType      *data,
                       ValueType       t)
{
	network->init (data, t);
}

void
cdn_rawc_network_reset (CdnRawcNetwork *network,
                        ValueType      *data,
                        ValueType       t)
{
	network->reset (data, t);
}

void
cdn_rawc_network_pre (CdnRawcNetwork *network,
                      ValueType      *data,
                      ValueType       t,
                      ValueType       dt)
{
	network->pre (data, t, dt);
}

void
cdn_rawc_network_diff (CdnRawcNetwork *network,
                       ValueType      *data,
                       ValueType       t,
                       ValueType       dt)
{
	network->diff (data, t, dt);
}

void
cdn_rawc_network_post (CdnRawcNetwork *network,
                       ValueType      *data,
                       ValueType       t,
                       ValueType       dt)
{
	network->post (data, t, dt);
}

ValueType *
cdn_rawc_network_alloc (CdnRawcNetwork    *network,
                        CdnRawcIntegrator *integrator)
{
	return malloc (sizeof (ValueType) * network->data_size * integrator->data_size);
}

void
cdn_rawc_network_free (ValueType *data)
{
	free (data);
}
