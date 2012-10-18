#ifndef __CDN_RAWC_NETWORK_H__
#define __CDN_RAWC_NETWORK_H__

#include <cdn-rawc/cdn-rawc-types.h>

void cdn_rawc_network_prepare (CdnRawcNetwork *network,
                               ValueType      *data,
                               ValueType       t);

void cdn_rawc_network_init    (CdnRawcNetwork *network,
                               ValueType      *data,
                               ValueType       t);

void cdn_rawc_network_reset   (CdnRawcNetwork *network,
                               ValueType      *data,
                               ValueType       t);

void cdn_rawc_network_pre     (CdnRawcNetwork *network,
                               ValueType      *data,
                               ValueType       t,
                               ValueType       dt);

void cdn_rawc_network_diff    (CdnRawcNetwork *network,
                               ValueType      *data,
                               ValueType       t,
                               ValueType       dt);

void cdn_rawc_network_post    (CdnRawcNetwork *network,
                               ValueType      *data,
                               ValueType       t,
                               ValueType       dt);

ValueType *cdn_rawc_network_alloc (CdnRawcNetwork    *network,
                                   CdnRawcIntegrator *integrator);

void cdn_rawc_network_free        (ValueType *data);

#endif /* __CDN_RAWC_NETWORK_H__ */

