# Programs
CC = gcc
AR = ar
RANLIB = ranlib

# Compiler flags
CFLAGS =
LDFLAGS =

WARNINGS = 				\
	inline				\
	missing-prototypes		\
	implicit-function-declaration	\
	strict-prototypes		\
	shadow				\
	unused-function		\
	unused-variable

${NAME}_CFLAGS = -I. $(addprefix -W,$(WARNINGS)) -DValueType=${valuetype} ${cflags} -DENABLE_MALLOC
${NAME}_LDFLAGS = -lm ${libs}

# Enable debug symbols if defined
DEBUG =

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
else
vecho = @echo [$1] $2;
veecho = echo [$1] $2;
endif

all: static shared

static: lib${name}.a
shared: lib${name}.so

lib${name}.a: $(STATIC_OBJECTS)
	$(call vecho,AR,$@) 							\
	([ ! -z "$(ST_LIBS)" ] && cp $(ST_LIBS) $@);	\
	$(AR) r $@ $^ 2>/dev/null && 					\
	$(RANLIB) $@

lib${name}.so: $(SHARED_OBJECTS)
	$(call vecho,CC,$@) $(CC) -shared -o $@ $^ $(SH_LIBS) $(${NAME}_LDFLAGS) $(LDFLAGS)

.o/%_static.o: %.c
	$(call vecho,CC,$@) mkdir -p $(dir $@); $(CC) -o $@ -c $< $(${NAME}_CFLAGS) $(CFLAGS)

.o/%_shared.o: %.c
	$(call vecho,CC,$@) mkdir -p $(dir $@); $(CC) -o $@ -c $< -fPIC $(${NAME}_CFLAGS) $(CFLAGS)

clean:
	$(call vecho,RM,objects) rm -f $(SHARED_OBJECTS) $(STATIC_OBJECTS) && \
	$(call veecho,RM,lib${name}) rm -f lib${name}.so lib${name}.a

.PHONY : static shared clean
