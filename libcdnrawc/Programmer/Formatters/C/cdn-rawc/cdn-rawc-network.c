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

CdnRawcEventValue *
cdn_rawc_network_event_get_value (CdnRawcNetwork *network,
                                  ValueType      *data,
                                  uint32_t        i)
{
	return network->event_get_value (data, i);
}

void
cdn_rawc_network_events_update (CdnRawcNetwork *network,
                                ValueType      *data)
{
	network->events_update (data);
}

uint8_t
cdn_rawc_network_get_event_active (CdnRawcNetwork *network,
                                   ValueType      *data,
                                   uint32_t        i)
{
	return network->event_active (data, i);
}

void
cdn_rawc_network_event_fire (CdnRawcNetwork *network,
                             ValueType      *data,
                             uint32_t        i)
{
	network->event_fire (data, i);
}
