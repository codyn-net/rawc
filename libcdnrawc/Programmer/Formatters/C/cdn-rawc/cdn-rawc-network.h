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

ValueType *cdn_rawc_network_get_data        (CdnRawcNetwork *network,
                                             void           *data);

ValueType *cdn_rawc_network_get_states      (CdnRawcNetwork *network,
                                             void           *data);

ValueType *cdn_rawc_network_get_derivatives (CdnRawcNetwork *network,
                                             void           *data);

void *cdn_rawc_network_get_nth              (CdnRawcNetwork *network,
                                             void           *data,
                                             uint32_t        nth);

#endif /* __CDN_RAWC_NETWORK_H__ */

