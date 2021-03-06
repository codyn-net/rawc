#ifndef __CDN_RAWC_NETWORK_H__
#define __CDN_RAWC_NETWORK_H__

#include <cdn-rawc/cdn-rawc-types.h>

void cdn_rawc_network_prepare         (CdnRawcNetwork *network,
                                       void           *data,
                                       ValueType       t);

void cdn_rawc_network_init            (CdnRawcNetwork *network,
                                       void           *data,
                                       ValueType       t);

void cdn_rawc_network_reset           (CdnRawcNetwork *network,
                                       void           *data,
                                       ValueType       t);

void cdn_rawc_network_update          (CdnRawcNetwork *network,
                                       void           *data,
                                       ValueType       t);

void cdn_rawc_network_pre             (CdnRawcNetwork *network,
                                       void           *data,
                                       ValueType       t,
                                       ValueType       dt);

void cdn_rawc_network_diff            (CdnRawcNetwork *network,
                                       void           *data,
                                       ValueType       t,
                                       ValueType       dt);

void cdn_rawc_network_post            (CdnRawcNetwork *network,
                                       void           *data,
                                       ValueType       t,
                                       ValueType       dt);

void cdn_rawc_network_events_update   (CdnRawcNetwork *network,
                                       void           *data);

void cdn_rawc_network_events_post_update (CdnRawcNetwork *network,
                                          void           *data);

void cdn_rawc_network_events_fire      (CdnRawcNetwork *network,
                                        void           *data);

uint8_t cdn_rawc_network_get_terminated (CdnRawcNetwork *network,
                                        void           *data);

ValueType *cdn_rawc_network_get_data        (CdnRawcNetwork *network,
                                             void           *data);

ValueType *cdn_rawc_network_get_states      (CdnRawcNetwork *network,
                                             void           *data);

ValueType *cdn_rawc_network_get_derivatives (CdnRawcNetwork *network,
                                             void           *data);

void *cdn_rawc_network_get_nth              (CdnRawcNetwork *network,
                                             void           *data,
                                             uint32_t        nth);

CdnRawcDimension const *
      cdn_rawc_network_get_dimension        (CdnRawcNetwork *network,
                                             uint32_t        i);

ValueType cdn_rawc_network_get_default_timestep (CdnRawcNetwork *network);
ValueType cdn_rawc_network_get_minimum_timestep (CdnRawcNetwork *network);

#ifdef ENABLE_META_LOOKUP
int32_t cdn_rawc_network_find_variable      (CdnRawcNetwork *network,
                                             char const     *name);

uint32_t cdn_rawc_network_meta_find_variable (CdnRawcNetwork *network,
                                              uint32_t        root,
                                              char const     *name);

uint32_t cdn_rawc_network_meta_find_node     (CdnRawcNetwork *network,
                                              uint32_t        root,
                                              char const     *name);
#endif

uint8_t cdn_rawc_network_get_type_size      (CdnRawcNetwork *network);
uint32_t cdn_rawc_network_get_data_size     (CdnRawcNetwork *network);
uint32_t cdn_rawc_network_get_data_count    (CdnRawcNetwork *network);

#ifdef ENABLE_MALLOC
void *cdn_rawc_network_alloc                (CdnRawcNetwork *network, uint32_t order);
void  cdn_rawc_network_free                 (void *ptr);
#endif

#endif /* __CDN_RAWC_NETWORK_H__ */

