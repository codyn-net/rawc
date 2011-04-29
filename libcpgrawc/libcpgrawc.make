

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Cpg.RawC.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =
BUILD_DIR = bin/Debug

LIBCPG_RAWC_SHARP_DLL_MDB_SOURCE=bin/Debug/Cpg.RawC.dll.mdb
LIBCPG_RAWC_SHARP_DLL_MDB=$(BUILD_DIR)/Cpg.RawC.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Cpg.RawC.dll
ASSEMBLY_MDB =
COMPILE_TARGET = library
PROJECT_REFERENCES =
BUILD_DIR = bin/Release

LIBCPG_RAWC_SHARP_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(LIBCPG_RAWC_SHARP_DLL_MDB)

LINUX_PKGCONFIG = \
	$(LIBCPG_RAWC_SHARP_PC)


RESGEN=resgen2

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG)

FILES = \
	State.cs \
	Knowledge.cs \
	CommandLine/OptionGroup.cs \
	CommandLine/OptionException.cs \
	CommandLine/OptionAttribute.cs \
	CommandLine/Options.cs \
	Tree/Node.cs \
	Tree/Dot.cs \
	Tree/NodePath.cs \
	Tree/Collectors/Result.cs \
	Tree/Collectors/Valiente.cs \
	Tree/Collectors/ICollector.cs \
	Tree/Collectors/Default.cs \
	Tree/SortedList.cs \
	Tree/Expression.cs \
	Tree/Instructions/Embedding.cs \
	Tree/Embedding.cs \
	Tree/Filters/IFilter.cs \
	Tree/Filters/Optimal.cs \
	Tree/Filters/Default.cs \
	Programmer/Function.cs \
	Programmer/Formatters/C/InitialValueTranslator.cs \
	Programmer/Formatters/C/C.cs \
	Programmer/Formatters/C/ComputationNodeTranslator.cs \
	Programmer/Formatters/C/NumberTranslator.cs \
	Programmer/Formatters/C/Context.cs \
	Programmer/Formatters/C/DynamicVisitor.cs \
	Programmer/Formatters/C/Options.cs \
	Programmer/Formatters/C/InstructionTranslator.cs \
	Programmer/Formatters/IFormatter.cs \
	Programmer/DataTable.cs \
	Programmer/Computation/IComputationNode.cs \
	Programmer/Computation/CopyTable.cs \
	Programmer/Computation/Comment.cs \
	Programmer/Computation/ZeroTable.cs \
	Programmer/Computation/Empty.cs \
	Programmer/Computation/Loop.cs \
	Programmer/Computation/Assignment.cs \
	Programmer/Computation/Addition.cs \
	Programmer/Program.cs \
	Programmer/Instructions/Variable.cs \
	Programmer/Instructions/Function.cs \
	Programmer/Options.cs \
	AssemblyInfo.cs \
	Plugins/Plugins.cs \
	Plugins/IOptions.cs \
	Plugins/Attributes.cs \
	Options.cs \
	Generator.cs \
	Sort.cs

DATA_FILES =

RESOURCES =

EXTRAS = \
	cpg-rawc-sharp.pc.in

REFERENCES =  \
	System \
	System.Xml \
	System.Data \
	$(CPG_NETWORK_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS)

DLL_REFERENCES =

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG)

LIBCPG_RAWC_SHARP_PC = $(BUILD_DIR)/cpg-rawc-sharp-@LIBCPG_RAWC_SHARP_API_VERSION@.pc
LIBCPG_RAWC_SHARP_API_PC = cpg-rawc-sharp-@LIBCPG_RAWC_SHARP_API_VERSION@.pc

pc_files = $(LIBCPG_RAWC_SHARP_API_PC)

include $(srcdir)/Makefile.include

$(eval $(call emit-deploy-wrapper,LIBCPG_RAWC_SHARP_PC,$(LIBCPG_RAWC_SHARP_API_PC)))

$(LIBCPG_RAWC_SHARP_API_PC): cpg-rawc-sharp.pc
	cp $< $@

$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
