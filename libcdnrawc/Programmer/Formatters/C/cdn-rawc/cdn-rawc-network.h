#ifndef __CDN_RAWC_NETWORK_H__
#define __CDN_RAWC_NETWORK_H__

#include <cdn-rawc/cdn-rawc-types.h>

void cdn_rawc_network_prepare         (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       ValueType       t);

void cdn_rawc_network_init            (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       ValueType       t);

void cdn_rawc_network_reset           (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       ValueType       t);

void cdn_rawc_network_pre             (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       ValueType       t,
                                       ValueType       dt);

void cdn_rawc_network_diff            (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       ValueType       t,
                                       ValueType       dt);

void cdn_rawc_network_post            (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       ValueType       t,
                                       ValueType       dt);

void cdn_rawc_network_events_update   (CdnRawcNetwork *network,
                                       ValueType      *data);

uint8_t cdn_rawc_network_event_active (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       uint32_t        i);

void cdn_rawc_network_event_fire      (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       uint32_t        i);

CdnRawcEventValue *
     cdn_rawc_network_event_get_value (CdnRawcNetwork *network,
                                       ValueType      *data,
                                       uint32_t        i);


#endif /* __CDN_RAWC_NETWORK_H__ */

