lib_LTLIBRARIES = libcdnrawc-1.0.la

SOURCE_FILES = \
	cdn-rawc-integrator.c

HEADER_FILES = \
	cdn-rawc.h \
	cdn-rawc-types.h \
	cdn-rawc-integrator.h

INTEGRATOR_SOURCE_FILES = \
	integrators/cdn-rawc-integrator-euler.c \
	integrators/cdn-rawc-integrator-runge-kutta.c

INTEGRATOR_HEADER_FILES = \
	integrators/cdn-rawc-integrator-euler.h \
	integrators/cdn-rawc-integrator-runge-kutta.h

libcdnrawc_1_0_la_CFLAGS = \
	-I $(srcdir)/../

libcdnrawc_1_0_la_SOURCES = \
	$(SOURCE_FILES) \
	$(INTEGRATOR_SOURCE_FILES)

libcdnrawc_1_0_includedir = $(includedir)/cdn-rawc-1.0/cdn-rawc
libcdnrawc_1_0_include_HEADERS = \
	$(HEADER_FILES)

libcdnrawc_1_0_integrators_includedir = $(includedir)/cdn-rawc-1.0/cdn-rawc/integrators
libcdnrawc_1_0_integrators_include_HEADERS = \
	$(INTEGRATOR_HEADER_FILES)

srcdatadir = $(datadir)/cdn-rawc-1.0/src/cdn-rawc
srcdata_DATA = \
	$(SOURCE_FILES) \
	$(HEADER_FILES)

srcintegratorsdatadir = $(datadir)/cdn-rawc-1.0/src/cdn-rawc/integrators
srcintegratorsdata_DATA = \
	$(INTEGRATOR_SOURCE_FILES) \
	$(INTEGRATOR_HEADER_FILES)

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = \
	cdn-rawc-1.0.pc

cdn-rawc-1.0.pc: cdn-rawc.pc
	cp cdn-rawc.pc cdn-rawc-1.0.pc
