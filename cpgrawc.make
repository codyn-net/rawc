

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:3 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/cpgrawc.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES = libcpgrawc/bin/Debug/Cpg.RawC.dll
BUILD_DIR = bin/Debug

RAWC_EXE_MDB_SOURCE=bin/Debug/cpgrawc.exe.mdb
RAWC_EXE_MDB=$(BUILD_DIR)/cpgrawc.exe.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/cpgrawc.exe
ASSEMBLY_MDB =
COMPILE_TARGET = exe
PROJECT_REFERENCES = libcpgrawc/bin/Release/Cpg.RawC.dll
BUILD_DIR = bin/Release

RAWC_EXE_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(RAWC_EXE_MDB)

BINARIES = \
	$(RAWC)


RESGEN=resgen2

all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES)

FILES = \
	Main.cs \
	AssemblyInfo.cs

DATA_FILES =

RESOURCES =

EXTRAS = \
	cpgrawc.in

REFERENCES = \
	$(CPG_NETWORK_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS)

DLL_REFERENCES =

CLEANFILES = $(PROGRAMFILES) $(BINARIES)

include $(top_srcdir)/Makefile.include
RAWC = $(BUILD_DIR)/cpgrawc

$(eval $(call emit-deploy-wrapper,RAWC,cpgrawc,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)

install-data-hook:
	for ASM in $(INSTALLED_ASSEMBLIES) $(PROJECT_REFERENCES); do \
		$(INSTALL) -c -m 0755 $$ASM $(DESTDIR)$(pkglibdir); \
		! test -f $$ASM.mdb || $(INSTALL) -c -m 0755 $$ASM.mdb $(DESTDIR)$(pkglibdir); \
	done;

uninstall-hook:
	for ASM in $(INSTALLED_ASSEMBLIES) $(PROJECT_REFERENCES); do \
		rm -f $(DESTDIR)$(pkglibdir)/`basename $$ASM`; \
		rm -f $(DESTDIR)$(pkglibdir)/`basename $$ASM`.mdb; \
	done;
