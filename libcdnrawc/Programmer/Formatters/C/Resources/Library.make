${include:Cdn.RawC.Programmer.Formatters.C.Resources.Include.make}

CDN_RAWC_VERSION = $(shell pkg-config --modversion cdn-rawc-1.0)

ifeq ($(CDN_RAWC_VERSION),)
$(error could not find cdn-rawc-1.0)
endif

${NAME}_CFLAGS += $(shell pkg-config --cflags cdn-rawc-1.0)

SH_LIBS = -shared -fPIC $(shell pkg-config --libs cdn-rawc-1.0)
ST_LIBS = $(shell pkg-config --variable=staticlibs cdn-rawc-1.0)