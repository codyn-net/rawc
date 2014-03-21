# Programs
CC = gcc

UNAME = $(shell uname)

# Compiler flags
CFLAGS ?=
LDFLAGS ?=

WARNINGS = 					\
	inline					\
	missing-prototypes			\
	error-implicit-function-declaration	\
	strict-prototypes			\
	shadow					\
	unused-function				\
	unused-variable				\
	return-type				\
	no-unused-value		\
	no-incompatible-pointer-types-discards-qualifiers

spbench_CFLAGS = -I. $(addprefix -W,$(WARNINGS)) -DValueType=double -std=c99 -O3
spbench_LDFLAGS = -lm

spbench_ARCH_CFLAGS =
spbench_ARCH_LDFLAGS =

ifeq ($(UNAME),Darwin)
spbench_CFLAGS += -DPLATFORM_OSX

ifeq ($(findstring -arch,$(CFLAGS)),)
spbench_ARCH_CFLAGS += -arch i386 -arch x86_64
spbench_ARCH_LDFLAGS += -arch i386 -arch x86_64
endif

endif

# Enable debug symbols if defined
DEBUG =

spbench_CFLAGS += -DENABLE_BLAS -DENABLE_LAPACK

ifeq ($(UNAME),Darwin)
spbench_LDFLAGS += -framework Accelerate
else
spbench_LDFLAGS += -lcblas -llapack
endif

ifeq ($(UNAME),Linux)
lsb = $(shell lsb_release -i -s)

ifeq ($(lsb),Ubuntu)
spbench_CFLAGS += -I/usr/include/atlas
endif
endif

ifneq ($(DEBUG),)
spbench_CFLAGS += -g -O0
else
spbench_CFLAGS += -O3
endif

# Library sources
SOURCES = spbench.c
OBJECTS = $(SOURCES:%.c=.o/%.o)

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

all: spbench

spbench: $(OBJECTS)
	$(call vecho,CC,$@) $(CC) -o $@ $^ $(spbench_LDFLAGS) $(LDFLAGS) $(spbench_ARCH_LDFLAGS)

.o/%.o: %.c
	$(call vecho,CC,$@) mkdir -p $(dir $@); $(CC) -o $@ -c $< $(spbench_CFLAGS) $(CFLAGS) $(spbench_ARCH_CFLAGS)

%.S: %.c
	$(call vecho,CC,$@) $(CC) -o $@ -S $< $(spbench_CFLAGS) $(CFLAGS)

clean:
	$(call vecho,RM,objects) rm -f $(OBJECTS) *.S && \
	$(call veecho,RM,spbench) rm -f spbench

.PHONY : all clean
