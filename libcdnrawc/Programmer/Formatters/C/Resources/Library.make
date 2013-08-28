${include:Cdn.RawC.Programmer.Formatters.C.Resources.Include.make}

SH_LIBS ?=
ST_LIBS ?=

# Check first to see if we have pkg-config setup
CDN_RAWC_VERSION = $(shell pkg-config --modversion cdn-rawc-1.0 2>/dev/null)

ifeq ($(CDN_RAWC_VERSION),)

# Check for the framework on OS X
ifeq ($(UNAME),Darwin)

fw = /Library/Frameworks/Codyn.framework

ifeq ($(wildcard $(fw)),)

$(error could not find Codyn.framework)

else

${NAME}_CFLAGS += -I$(fw)/Headers/cdn-rawc-1.0
SH_LIBS += -L$(fw)/Resources/mono/lib -lcdnrawc-1.0.0
ST_LIBS += $(fw)/Resources/mono/lib/libcdnrawc-1.0.a

endif

else

$(error could not find cdn-rawc-1.0)

endif

else

${NAME}_CFLAGS += $(shell pkg-config --cflags cdn-rawc-1.0)
SH_LIBS += $(shell pkg-config --libs cdn-rawc-1.0)
ST_LIBS += $(shell pkg-config --variable=staticlibs cdn-rawc-1.0)

endif


ifeq ($(UNAME),Darwin)
SH_LIBS += -dynamiclib -fPIC
else
SH_LIBS += -shared -fPIC
endif