#ifndef __${BASENAME}_RUN_H__
#define __${BASENAME}_RUN_H__

#include "${basename}.h"

CDN_RAWC_BEGIN_DECLS

void               cdn_rawc_${name}_init       (ValueType t);
void               cdn_rawc_${name}_prepare    (ValueType t);
void               cdn_rawc_${name}_reset      (ValueType t);
void               cdn_rawc_${name}_step       (ValueType t, ValueType dt);

ValueType          cdn_rawc_${name}_get        (CdnRawc${Name}State index);
void               cdn_rawc_${name}_set        (CdnRawc${Name}State index,
                                                ValueType       value);

ValueType         *cdn_rawc_${name}_data       (void);
CdnRawcIntegrator *cdn_rawc_${name}_integrator (void);
CdnRawcDimension const *cdn_rawc_${name}_get_dimension (uint32_t i);

CDN_RAWC_END_DECLS

#endif /* __${BASENAME}_RUN_H__ */
