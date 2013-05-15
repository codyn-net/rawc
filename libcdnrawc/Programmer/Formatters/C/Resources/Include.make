# Programs
CC = gcc
AR = ar
RANLIB = ranlib
LIBTOOL = libtool

UNAME = $(shell uname)

ifeq ($(UNAME),Darwin)
SHARED_EXT = dylib
else
SHARED_EXT = so
endif

# Compiler flags
CFLAGS ?=
LDFLAGS ?=

WARNINGS = 				\
	inline				\
	missing-prototypes		\
	error-implicit-function-declaration	\
	strict-prototypes		\
	shadow				\
	unused-function		\
	unused-variable

${NAME}_CFLAGS = -I. $(addprefix -W,$(WARNINGS)) -DValueType=${valuetype} ${cflags} -DENABLE_MALLOC -DENABLE_META_LOOKUP -std=c99
${NAME}_LDFLAGS = -lm ${libs}

ifeq ($(UNAME),Darwin)
${NAME}_CFLAGS += -DPLATFORM_OSX

ifeq ($(findstring -arch,$(CFLAGS)),)
${NAME}_CFLAGS += -arch i386 -arch x86_64
${NAME}_LDFLAGS += -arch i386 -arch x86_64
endif

endif

# Enable debug symbols if defined
DEBUG =

ENABLE_BLAS ?= ${enable_blas}
ENABLE_LAPACK ?= ${enable_lapack}

ifeq ($(ENABLE_BLAS),1)
${NAME}_CFLAGS += -DENABLE_BLAS

ifeq ($(UNAME),Darwin)
${NAME}_LDFLAGS += -framework Accelerate
else
${NAME}_LDFLAGS += -lcblas
endif

endif

ifeq ($(ENABLE_LAPACK),1)
${NAME}_CFLAGS += -DENABLE_LAPACK

ifeq ($(UNAME),Darwin)
${NAME}_LDFLAGS += -framework vecLib
else
${NAME}_LDFLAGS += -llapack
endif

ifeq ($(UNAME),Linux)
lsb = $(shell lsb_release -i -s)

ifeq ($(lsb),Ubuntu)
${NAME}_CFLAGS += -I/usr/include/atlas
endif
endif
endif

ifneq ($(DEBUG),)
${NAME}_CFLAGS += -g -O0
else
${NAME}_CFLAGS += -O3
endif

# Library sources
SOURCES = ${SOURCES}
HEADERS = ${HEADERS}

SHARED_OBJECTS = $(SOURCES:%.c=.o/%_shared.o)
STATIC_OBJECTS = $(SOURCES:%.c=.o/%_static.o)

V =

ifneq ($(V),)
vecho =
veecho =
onull =
else
vecho = @echo [$1] $2;
veecho = echo [$1] $2;
onull = >/dev/null
endif

all: static shared

static: lib${name}.a
shared: lib${name}.$(SHARED_EXT)

lib${name}.a: $(STATIC_OBJECTS)
	$(call vecho,LIBTOOL,$@) 							\
	$(LIBTOOL) --mode=link gcc -o $@ $^ $(ST_LIBS) $(onull)

lib${name}.$(SHARED_EXT): $(SHARED_OBJECTS)
	$(call vecho,CC,$@) $(CC) -shared -o $@ $^ $(SH_LIBS) $(${NAME}_LDFLAGS) $(LDFLAGS)

.o/%_static.o: %.c
	$(call vecho,CC,$@) mkdir -p $(dir $@); $(CC) -o $@ -c $< $(${NAME}_CFLAGS) $(CFLAGS)

.o/%_shared.o: %.c
	$(call vecho,CC,$@) mkdir -p $(dir $@); $(CC) -o $@ -c $< -fPIC $(${NAME}_CFLAGS) $(CFLAGS)

clean:
	$(call vecho,RM,objects) rm -f $(SHARED_OBJECTS) $(STATIC_OBJECTS) && \
	$(call veecho,RM,lib${name}) rm -f lib${name}.$(SHARED_EXT) lib${name}.a

.PHONY : static shared clean
