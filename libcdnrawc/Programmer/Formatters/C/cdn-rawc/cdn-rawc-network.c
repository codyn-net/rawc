#include "cdn-rawc-network.h"

#include <stdlib.h>
#include <string.h>

void
cdn_rawc_network_prepare (CdnRawcNetwork *network,
                          void           *data,
                          ValueType       t)
{
	network->prepare (data, t);
}

void
cdn_rawc_network_init (CdnRawcNetwork *network,
                       void           *data,
                       ValueType       t)
{
	network->init (data, t);
}

void
cdn_rawc_network_reset (CdnRawcNetwork *network,
                        void           *data,
                        ValueType       t)
{
	network->reset (data, t);
}

void
cdn_rawc_network_pre (CdnRawcNetwork *network,
                      void           *data,
                      ValueType       t,
                      ValueType       dt)
{
	network->pre (data, t, dt);
}

void
cdn_rawc_network_diff (CdnRawcNetwork *network,
                       void            *data,
                       ValueType       t,
                       ValueType       dt)
{
	network->diff (data, t, dt);
}

void
cdn_rawc_network_post (CdnRawcNetwork *network,
                       void           *data,
                       ValueType       t,
                       ValueType       dt)
{
	network->post (data, t, dt);
}

void
cdn_rawc_network_events_update (CdnRawcNetwork *network,
                                void           *data)
{
	network->events_update (data);
}

void
cdn_rawc_network_events_post_update (CdnRawcNetwork *network,
                                     void           *data)
{
	network->events_post_update (data);
}

void
cdn_rawc_network_events_fire (CdnRawcNetwork *network,
                              void           *data)
{
	network->events_fire (data);
}

ValueType *
cdn_rawc_network_get_data (CdnRawcNetwork *network,
                           void           *data)
{
	return network->get_data (data);
}

ValueType *
cdn_rawc_network_get_states (CdnRawcNetwork *network,
                             void           *data)
{
	return network->get_states (data);
}

ValueType *
cdn_rawc_network_get_derivatives (CdnRawcNetwork *network,
                                  void           *data)
{
	return network->get_derivatives (data);
}

void *
cdn_rawc_network_get_nth (CdnRawcNetwork *network,
                          void           *data,
                          uint32_t        nth)
{
	return network->get_nth (data, nth);
}

CdnRawcDimension const *
cdn_rawc_network_get_dimension (CdnRawcNetwork *network,
                                void           *data,
                                uint32_t        i)
{
	return network->get_dimension (network->dimensions, i);
}

uint32_t
cdn_rawc_network_get_data_size (CdnRawcNetwork *network)
{
	return network->data_size;
}

uint32_t
cdn_rawc_network_get_data_count (CdnRawcNetwork *network)
{
	return network->data_count;
}

uint8_t
cdn_rawc_network_get_type_size (CdnRawcNetwork *network)
{
	return network->type_size;
}

static uint8_t
compare_names (char const *name, char const *cmpto, int len)
{
	if (len == -1)
	{
		return strcmp (name, cmpto) == 0 ? 1 : 0;
	}
	else
	{
		return (strlen (cmpto) == len && strncmp (name, cmpto, len) == 0) ? 1 : 0;
	}
}

int32_t
cdn_rawc_network_find_variable (CdnRawcNetwork *network,
                                char const     *name)
{
	uint32_t nodepos;

	nodepos = 1;

	// Only support simple . syntax
	do
	{
		char const *dotpos;
		int len = -1;
		uint32_t child;

		// Check if we are still in range
		if (nodepos >= network->meta.nodes_size)
		{
			return 0;
		}

		dotpos = strchr (name, '.');

		if (dotpos != NULL)
		{
			len = name - dotpos;
		}

		// Lookup the corresponding child in 'nodepos'
		child = network->meta.nodes[nodepos].first_child;
		nodepos = 0;

		while (child > 0)
		{
			CdnRawcChildMeta const *cmeta;

			cmeta = &network->meta.children[child];

			if (cmeta->is_node)
			{
				if (compare_names (name, network->meta.nodes[cmeta->index].name, len))
				{
					nodepos = cmeta->index;
					break;
				}
			}
			else
			{
				if (compare_names (name, network->meta.states[cmeta->index].name, len))
				{
					if (len == -1)
					{
						return (int32_t)network->meta.states[cmeta->index].index;
					}
				}
			}

			child = cmeta->next;
		}
	} while (1);

	return 0;
}

#ifdef ENABLE_MALLOC
void *
cdn_rawc_network_alloc (CdnRawcNetwork *network,
                        uint32_t        order)
{
	return malloc (network->size * (network->event_refinement + order));
}

void
cdn_rawc_network_free (void *ptr)
{
	free (ptr);
}
#endif
