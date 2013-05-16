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

ValueType
cdn_rawc_network_get_default_timestep (CdnRawcNetwork *network)
{
	return network->default_timestep;
}

#ifdef ENABLE_META_LOOKUP
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

static char const *
skip_ws (char const *s)
{
	while (s && *s && isspace (*s))
	{
		++s;
	}

	return s;
}

static char const *
extract_next_part (char const  *s,
                   int         *len,
                   char const **next_s)
{
	char const *ret = NULL;

	s = skip_ws (s);

	*len = 0;
	*next_s = NULL;

	if (!*s)
	{
		return NULL;
	}

	if (*s == '"')
	{
		ret = ++s;

		// go until the next double quote
		while (*s && *s != '"')
		{
			++s;
		}

		if (!*s)
		{
			return NULL;
		}

		*len = s - ret - 1;

		// Skip over the double quote
		++s;
	}
	else
	{
		ret = s;

		// Read until the next dot or space
		while (*s && *s != '.' && isspace (*s))
		{
			++s;
		}

		*len = s - ret - 1;
	}

	s = skip_ws (s);

	if (*s == '.')
	{
		// Next start
		*next_s = s + 1;
	}
	else if (*s)
	{
		// Error
		*len = 0;
		return NULL;
	}

	return ret;
}

static uint32_t
rawc_find_child (CdnRawcNetwork *network,
                 uint32_t        root,
                 char const     *name)
{
	// Only support simple . syntax
	do
	{
		char const *next_name;
		int len = -1;
		uint32_t child;

		// Check if we are still in range
		if (root >= network->meta.nodes_size)
		{
			return 0;
		}

		name = extract_next_part (name, &len, &next_name);

		if (name == NULL)
		{
			return 0;
		}

		// Lookup the corresponding child in 'root'
		child = network->meta.nodes[root].first_child;
		root = 0;

		while (child > 0)
		{
			CdnRawcChildMeta const *cmeta;

			cmeta = &network->meta.children[child];

			if (cmeta->is_node)
			{
				if (compare_names (name, network->meta.nodes[cmeta->index].name, len))
				{
					if (!next_name)
					{
						return child;
					}

					root = cmeta->index;
					break;
				}
			}
			else
			{
				if (compare_names (name, network->meta.states[cmeta->index].name, len))
				{
					// Check for the last item
					if (!next_name)
					{
						return child;
					}
				}
			}

			child = cmeta->next;
		}

		if (!next_name)
		{
			break;
		}

		name = next_name;
	} while (1);

	return 0;
}

int32_t
cdn_rawc_network_find_variable (CdnRawcNetwork *network,
                                char const     *name)
{
	uint32_t child;

	child = rawc_find_child (network, 1, name);

	if (child == 0 || network->meta.children[child].is_node)
	{
		return -1;
	}
	else
	{
		return (int32_t)network->meta.states[network->meta.children[child].index].index;
	}
}

uint32_t
cdn_rawc_network_meta_find_variable (CdnRawcNetwork *network,
                                     uint32_t        root,
                                     char const     *name)
{
	uint32_t child;

	child = rawc_find_child (network, 1, name);

	if (child == 0 || network->meta.children[child].is_node)
	{
		return 0;
	}
	else
	{
		return network->meta.children[child].index;
	}
}

uint32_t
cdn_rawc_network_meta_find_node (CdnRawcNetwork *network,
                                 uint32_t        root,
                                 char const     *name)
{
	uint32_t child;

	child = rawc_find_child (network, 1, name);

	if (child == 0 || !network->meta.children[child].is_node)
	{
		return 0;
	}
	else
	{
		return network->meta.children[child].index;
	}
}
#endif

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
