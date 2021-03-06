

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = $(CSC)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Cdn.RawC.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =
BUILD_DIR = bin/Debug

LIBCDNRAWC_SHARP_DLL_MDB_SOURCE=bin/Debug/Cdn.RawC.dll.mdb
LIBCDNRAWC_SHARP_DLL_MDB=$(BUILD_DIR)/Cdn.RawC.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = $(CSC)
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Cdn.RawC.dll
ASSEMBLY_MDB =
COMPILE_TARGET = library
PROJECT_REFERENCES =
BUILD_DIR = bin/Release

LIBCDNRAWC_SHARP_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(LIBCDNRAWC_SHARP_DLL_MDB) \
	Cdn.RawC.dll.config

LINUX_PKGCONFIG = \
	$(LIBCDNRAWC_SHARP_PC)


RESGEN=resgen2

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG)

FILES = \
	Binder.cs \
	State.cs \
	ConstraintState.cs \
	DerivativeState.cs \
	DelayedState.cs \
	EventActionState.cs \
	EventSetState.cs \
	EventNodeState.cs \
	Knowledge.cs \
	Exception.cs \
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
	Tree/Embedding.cs \
	Tree/Filters/IFilter.cs \
	Tree/Filters/Optimal.cs \
	Tree/Filters/Default.cs \
	Programmer/APIFunction.cs \
	Programmer/DependencyFilter.cs \
	Programmer/DependencyGraph.cs \
	Programmer/DependencyGroup.cs \
	Programmer/Function.cs \
	Programmer/Formatters/C/InitialValueTranslator.cs \
	Programmer/Formatters/C/Options.cs \
	Programmer/Formatters/C/NumberTranslator.cs \
	Programmer/Formatters/C/InstructionTranslator.cs \
	Programmer/Formatters/C/Context.cs \
	Programmer/Formatters/C/ComputationNodeTranslator.cs \
	Programmer/Formatters/C/C.cs \
	Programmer/Formatters/C/Lapack.cs \
	Programmer/Formatters/IFormatter.cs \
	Programmer/Formatters/CLike/InitialValueTranslator.cs \
	Programmer/Formatters/CLike/Options.cs \
	Programmer/Formatters/CLike/InstructionTranslator.cs \
	Programmer/Formatters/CLike/Context.cs \
	Programmer/Formatters/CLike/ComputationNodeTranslator.cs \
	Programmer/Formatters/CLike/CLike.cs \
	Programmer/Formatters/JavaScript/Options.cs \
	Programmer/Formatters/JavaScript/JavaScript.cs \
	Programmer/Formatters/JavaScript/InitialValueTranslator.cs \
	Programmer/Formatters/JavaScript/InstructionTranslator.cs \
	Programmer/Formatters/JavaScript/Context.cs \
	Programmer/Formatters/JavaScript/ComputationNodeTranslator.cs \
	Programmer/Formatters/JavaScript/NumberTranslator.cs \
	Programmer/DataTable.cs \
	Programmer/Computation/Block.cs \
	Programmer/Computation/IBlock.cs \
	Programmer/Computation/INode.cs \
	Programmer/Computation/IncrementDelayedCounters.cs \
	Programmer/Computation/CopyTable.cs \
	Programmer/Computation/Comment.cs \
	Programmer/Computation/ZeroMemory.cs \
	Programmer/Computation/Empty.cs \
	Programmer/Computation/Loop.cs \
	Programmer/Computation/CallAPI.cs \
	Programmer/Computation/Assignment.cs \
	Programmer/Computation/StateConditional.cs \
	Programmer/Computation/InitializeDelayHistory.cs \
	Programmer/Computation/Rand.cs \
	Programmer/Computation/EventProgram.cs \
	Programmer/Program.cs \
	Programmer/Instructions/IInstruction.cs \
	Programmer/Instructions/Variable.cs \
	Programmer/Instructions/Function.cs \
	Programmer/Instructions/SparseOperator.cs \
	Programmer/Instructions/State.cs \
	Programmer/Options.cs \
	AssemblyInfo.cs \
	Plugins/Plugins.cs \
	Plugins/IOptions.cs \
	Plugins/Attributes.cs \
	Options.cs \
	Generator.cs \
	Sort.cs \
	Log.cs \
	Config.cs \
	Profile.cs \
	Validator.cs \
	Asciifyer.cs \
	DynamicVisitor.cs \
	Sparsity.cs \
	SparsityBenchmarker.cs

DATA_FILES =

RESOURCES = \
	Resources/SparsityBench.make,Cdn.RawC.Resources.SparsityBench.make \
	\
	Programmer/Formatters/C/Resources/Library.make,Cdn.RawC.Programmer.Formatters.C.Resources.Library.make \
	Programmer/Formatters/C/Resources/Standalone.make,Cdn.RawC.Programmer.Formatters.C.Resources.Standalone.make \
	Programmer/Formatters/C/Resources/Include.make,Cdn.RawC.Programmer.Formatters.C.Resources.Include.make \
	Programmer/Formatters/C/Resources/RunSource.c,Cdn.RawC.Programmer.Formatters.C.Resources.RunSource.c \
	Programmer/Formatters/C/Resources/RunHeader.h,Cdn.RawC.Programmer.Formatters.C.Resources.RunHeader.h \
	Programmer/Formatters/C/cdn-rawc/cdn-rawc-math.h,Cdn.RawC.Programmer.Formatters.C.Resources.cdn-rawc-math.h \
	Programmer/Formatters/C/cdn-rawc/cdn-rawc-macros.h,Cdn.RawC.Programmer.Formatters.C.Resources.cdn-rawc-macros.h \
	\
	Programmer/Formatters/JavaScript/Resources/Cdn.js,Cdn.RawC.Programmer.Formatters.JavaScript.Resources.Cdn.js \
	Programmer/Formatters/JavaScript/Resources/Cdn.Utils.js,Cdn.RawC.Programmer.Formatters.JavaScript.Resources.Cdn.Utils.js \
	Programmer/Formatters/JavaScript/Resources/Cdn.Math.js,Cdn.RawC.Programmer.Formatters.JavaScript.Resources.Cdn.Math.js \
	Programmer/Formatters/JavaScript/Resources/Cdn.Integrators.js,Cdn.RawC.Programmer.Formatters.JavaScript.Resources.Cdn.Integrators.js \
	Programmer/Formatters/JavaScript/Resources/Cdn.Integrators.Euler.js,Cdn.RawC.Programmer.Formatters.JavaScript.Resources.Cdn.Integrators.Euler.js \
	Programmer/Formatters/JavaScript/Resources/Cdn.Integrators.RungeKutta.js,Cdn.RawC.Programmer.Formatters.JavaScript.Resources.Cdn.Integrators.RungeKutta.js

EXTRAS = \
	codyn-rawc-sharp.pc.in \
	Cdn.RawC.dll.config

REFERENCES =  \
	System \
	System.Core \
	$(CODYN_SHARP_LIBS) \
	$(GLIB_SHARP_LIBS)

DLL_REFERENCES =

CLEANFILES = $(LIBCDNRAWC_SHARP_DLL_MDB) $(LINUX_PKGCONFIG)

LIBCDNRAWC_SHARP_PC = $(BUILD_DIR)/codyn-rawc-sharp-@LIBCDNRAWC_SHARP_API_VERSION@.pc
LIBCDNRAWC_SHARP_API_PC = codyn-rawc-sharp-@LIBCDNRAWC_SHARP_API_VERSION@.pc

pc_files = $(LIBCDNRAWC_SHARP_API_PC)

include $(srcdir)/Makefile.include

$(eval $(call emit-deploy-wrapper,LIBCDNRAWC_SHARP_PC,$(LIBCDNRAWC_SHARP_API_PC)))

$(LIBCDNRAWC_SHARP_API_PC): codyn-rawc-sharp.pc
	cp $< $@

$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref) && \
	cp $(srcdir)/Cdn.RawC.dll.config $(dir $(ASSEMBLY))

.NOTPARALLEL:
