#ifndef __CDN_RAWC_TYPES_H__
#define __CDN_RAWC_TYPES_H__

#ifdef __cplusplus
#define CDN_RAWC_BEGIN_DECLS extern "C" {
#define CDN_RAWC_END_DECLS }
#else
#define CDN_RAWC_BEGIN_DECLS
#define CDN_RAWC_END_DECLS
#endif

#include <stdint.h>

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
	void (*clear) (ValueType *data);
	void (*init) (ValueType *data, ValueType t, ValueType dt);
	void (*pre)  (ValueType *data, ValueType t, ValueType dt);
	void (*diff) (ValueType *data, ValueType t, ValueType dt);
	void (*post) (ValueType *data, ValueType t, ValueType dt);

	CdnRawcRange states;
	CdnRawcRange derivatives;

	uint32_t data_size;

	struct
	{
		uint32_t t;
		uint32_t dt;

		// List of metadata per state
		CdnRawcStateMeta const *states;
		uint32_t states_size;

		// List of nodes in the network
		CdnRawcNodeMeta const *nodes;
		uint32_t nodes_size;

		// Linked lists of node children
		CdnRawcChildMeta const *children;
		uint32_t children_size;
	} meta;
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
};

CDN_RAWC_END_DECLS

#endif /* __CDN_RAWC_TYPES_H__ */

