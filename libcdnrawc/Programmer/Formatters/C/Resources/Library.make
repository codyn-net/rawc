${include:Cdn.RawC.Programmer.Formatters.C.Resources.Include.make}

fw = /Library/Frameworks/Codyn.framework

ifeq ($(wildcard $(fw)),)
CDN_RAWC_VERSION = $(shell pkg-config --modversion cdn-rawc-1.0)

ifeq ($(CDN_RAWC_VERSION),)
$(error could not find cdn-rawc-1.0)
endif

${NAME}_CFLAGS += $(shell pkg-config --cflags cdn-rawc-1.0)
SH_LIBS = -shared -fPIC $(shell pkg-config --libs cdn-rawc-1.0)
else
${NAME}_CFLAGS += -I$(fw)/Headers/cdn-rawc-1.0
SH_LIBS = -dynamiclib -fPIC -L$(fw)/Resources/mono/lib -lcdnrawc-1.0.0
endif

ST_LIBS = $(shell pkg-config --variable=staticlibs cdn-rawc-1.0)
