#ifndef __CDN_RAWC_TYPES_H__
#define __CDN_RAWC_TYPES_H__

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifndef ValueType
#define ValueType double
#endif

typedef struct
{
	uint32_t start;
	uint32_t end;
	uint32_t stride;
} CdnRawcRange;

typedef struct
{
	uint16_t rows;
	uint16_t columns;
} CdnRawcDimension;

typedef struct
{
	// Name of the state
	char const *name;

	// Index into network.meta.nodes
	uint32_t parent;

	// Index into data
	uint32_t index;
} CdnRawcStateMeta;

typedef struct
{
	// Index into network.meta.nodes
	uint32_t parent;
	uint8_t is_node;

	// Index into:
	//   1) network.meta.nodes if .is_node
	//   2) network.meta.states if !.is_node
	uint32_t index;

	// Index into network.meta.children (or 0 if there is no next child)
	uint32_t next;
} CdnRawcChildMeta;

typedef struct
{
	// Name of the node
	char const *name;

	// Index into network.meta.nodes
	uint32_t parent;

	// Index into network.meta.children (or 0 if node has no children)
	uint32_t first_child;
} CdnRawcNodeMeta;

typedef struct
{
	uint32_t t;
	uint32_t dt;

	char const *name;

	// List of metadata per state
	CdnRawcStateMeta const *states;
	uint32_t states_size;

	// List of nodes in the network
	CdnRawcNodeMeta const *nodes;
	uint32_t nodes_size;

	// Linked lists of node children
	CdnRawcChildMeta const *children;
	uint32_t children_size;
} CdnRawcNetworkMeta;

typedef struct
{
	ValueType previous;
	ValueType current;
	ValueType distance;
} CdnRawcEventValue;

typedef struct
{
	ValueType *data;
} CdnRawcData;

typedef struct
{
	void     (*prepare)           (void        *data,
	                               ValueType    t);

	void     (*init)              (void        *data,
	                               ValueType    t);

	void     (*reset)             (void        *data,
	                               ValueType    t);

	void     (*pre)               (void        *data,
	                               ValueType    t,
	                               ValueType    dt);

	void     (*prediff)           (void        *data);

	void     (*diff)              (void        *data,
	                               ValueType    t,
	                               ValueType    dt);

	void     (*post)              (void        *data,
	                               ValueType    t,
	                               ValueType    dt);

	void    (*events_update)      (void        *data);
	void    (*events_post_update) (void        *data);

	void    (*events_fire)        (void        *data);

	ValueType *(*get_data)        (void        *data);
	ValueType *(*get_states)      (void        *data);
	ValueType *(*get_derivatives) (void        *data);
	void      *(*get_nth)         (void        *data,
	                               uint32_t     nth);

	uint32_t (*get_events_active_size) (void     *data);
	uint32_t (*get_events_active)      (void     *data,
	                                    uint32_t  i);

	CdnRawcEventValue *(*get_events_value) (void     *data,
	                                        uint32_t  i);

	CdnRawcDimension const *(*get_dimension) (CdnRawcDimension const *dimensions,
	                                          uint32_t                i);

	CdnRawcRange states;
	CdnRawcRange derivatives;
	CdnRawcRange event_values;

	CdnRawcDimension const *dimensions;

	uint32_t size;
	uint32_t data_size;
	uint32_t data_count;

	uint8_t event_refinement;
	uint8_t type_size;

	ValueType minimum_timestep;

	CdnRawcNetworkMeta meta;
} CdnRawcNetwork;

typedef struct _CdnRawcIntegrator CdnRawcIntegrator;

struct _CdnRawcIntegrator
{
	void (*step) (CdnRawcIntegrator *integrator,
	              CdnRawcNetwork    *network,
	              void              *data,
	              ValueType          t,
	              ValueType          dt);

	void (*diff) (CdnRawcIntegrator *integrator,
	              CdnRawcNetwork    *network,
	              void              *data,
	              ValueType          t,
	              ValueType          dt);

	uint32_t order;
};

#ifdef __cplusplus
}
#endif

#endif /* __CDN_RAWC_TYPES_H__ */

