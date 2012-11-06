#ifndef __CDN_RAWC_TYPES_H__
#define __CDN_RAWC_TYPES_H__

#include <stdint.h>
#include <cdn-rawc/cdn-rawc-macros.h>

CDN_RAWC_BEGIN_DECLS

#ifndef ValueType
#define ValueType double
#endif

typedef struct
{
	uint32_t start;
	uint32_t end;
} CdnRawcRange;

typedef struct
{
	// Name of the state
	char const *name;

	// Index into network.meta.nodes
	uint32_t parent;
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

typedef enum
{
	CDN_RAWC_EVENT_STATE_TYPE_LESS,
	CDN_RAWC_EVENT_STATE_TYPE_LESS_OR_EQUAL,
	CDN_RAWC_EVENT_STATE_TYPE_GREATER,
	CDN_RAWC_EVENT_STATE_TYPE_GREATER_OR_EQUAL,
	CDN_RAWC_EVENT_STATE_TYPE_EQUAL,
	CDN_RAWC_EVENT_STATE_TYPE_AND,
	CDN_RAWC_EVENT_STATE_TYPE_OR,
} CdnRawcEventStateType;

typedef struct
{
	ValueType previous_value;
	ValueType current_value;
	ValueType distance;
} CdnRawcEventValue;

typedef struct
{
	CdnRawcEventStateType type;
	ValueType approximation;

	uint32_t left;
	uint32_t right;
} CdnRawcEvent;

typedef struct
{
	void (*prepare) (ValueType *data, ValueType t);
	void (*init) (ValueType *data, ValueType t);
	void (*reset) (ValueType *data, ValueType t);
	void (*pre)  (ValueType *data, ValueType t, ValueType dt);
	void (*prediff) (ValueType *data);
	void (*diff) (ValueType *data, ValueType t, ValueType dt);
	void (*post) (ValueType *data, ValueType t, ValueType dt);

	void (*events_update) (ValueType *data);

	uint8_t (*event_active) (ValueType *data,
	                         uint32_t   i);

	void (*event_fire) (ValueType *data,
	                    uint32_t   i);

	CdnRawcEventValue * (*event_get_value) (ValueType *data,
	                                        uint32_t   i);

	CdnRawcRange states;
	CdnRawcRange derivatives;
	CdnRawcRange event_values;

	uint32_t data_size;
	uint8_t type_size;

	uint32_t events_size;
	uint32_t events_size_all;

	CdnRawcEvent const *events;

	CdnRawcNetworkMeta meta;
} CdnRawcNetwork;

typedef struct _CdnRawcIntegrator CdnRawcIntegrator;

struct _CdnRawcIntegrator
{
	void (*step) (CdnRawcIntegrator *integrator,
	              CdnRawcNetwork    *network,
	              ValueType         *data,
	              ValueType          t,
	              ValueType          dt);

	void (*diff) (CdnRawcIntegrator *integrator,
	              CdnRawcNetwork    *network,
	              ValueType         *data,
	              ValueType          t,
	              ValueType          dt);

	uint32_t data_size;
};

CDN_RAWC_END_DECLS

#endif /* __CDN_RAWC_TYPES_H__ */

