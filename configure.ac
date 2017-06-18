dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT(cdn-rawc, 3.6.3)
AM_INIT_AUTOMAKE([foreign subdir-objects tar-ustar dist-xz no-dist-gzip -Wno-portability])

AM_SILENT_RULES([yes])

AC_CONFIG_MACRO_DIR([m4])
AM_MAINTAINER_MODE

AC_PROG_CC

PKG_PROG_PKG_CONFIG

AM_PATH_PYTHON([2.7], [enable_python2=yes], [enable_python2=no])
AM_CONDITIONAL([ENABLE_PYTHON2], [test x$enable_python2 = xyes])

AC_PATH_PROG([PYTHON3], [python3], no)

if test "x$PYTHON3" != "xno"; then
	AC_SUBST([PYTHON3_PREFIX], ['${prefix}'])

	if test "x$prefix" = xNONE; then
		am_py3_prefix=$ac_default_prefix
	else
		am_py3_prefix=$prefix
	fi

	am_cv_python3_version=`$PYTHON3 -c "import sys; sys.stdout.write(sys.version[[:3]])"`

	AC_SUBST([PYTHON3_VERSION], [$am_cv_python3_version])

	python3dir=`$PYTHON3 -c "from distutils import sysconfig; print(sysconfig.get_python_lib(prefix='$am_py3_prefix'))"`

	case $python3dir in
	$am_py_prefix*)
		am__strip_prefix=`echo "$am_py3_prefix" | sed 's|.|.|g'`
		python3dir=`echo "$python3dir" | sed "s,^$am__strip_prefix,$PYTHON3_PREFIX,"`
		;;
	*)
		case $am_py_prefix in
			/usr|/System*) ;;
		*)
			python3dir=$PYTHON3_PREFIX/lib/python$PYTHON3_VERSION/site-packages
		;;
		esac
	;;
	esac

	AC_SUBST(python3dir)
	enable_python3=yes
else
	enable_python3=no
fi

AM_CONDITIONAL([ENABLE_PYTHON3], [test x$enable_python3 = xyes])

LIBCDNRAWC_SHARP_API_VERSION=3.0
AC_SUBST(LIBCDNRAWC_SHARP_API_VERSION)

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

LT_INIT

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

AC_PROG_INSTALL

AC_PATH_PROGS(CSC, dmcs gmcs, no)

if test "x$CSC" = "xno"; then
	AC_MSG_ERROR([gmcs or dmcs not found])
fi

AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=YES]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
	CFLAGS="$CFLAGS -O2 -g"
	RUN_DEBUG="--debug "
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=NO]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
	CFLAGS="$CFLAGS -O3"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_DEBUG, true)
	enable_debug=yes
	RUN_DEBUG="--debug "
	CFLAGS="$CFLAGS -O2 -g"
fi

AC_SUBST(RUN_DEBUG)

AC_ARG_WITH([framework],
             AS_HELP_STRING([--with-framework=path],[Enable build for OS X framework (default: no)]),
              [with_framework=$withval],
              [with_framework=no])

if test "x$with_framework" != "xno"; then
OLDPKG_CONFIG="$PKG_CONFIG"
PKG_CONFIG="$PKG_CONFIG --define-variable=prefix=$with_framework"
fi

dnl package checks, common for all configs
PKG_CHECK_MODULES([CODYN_SHARP], [codyn-sharp-3.0])

dnl find assembly paths for unstable installed assemblies that will be
dnl copied into monoskel installation directory for local usage with the app
UNSTABLE_INSTALL_ASSEMBLIES="codyn-sharp-3.0"
INSTALLED_ASSEMBLIES="`$PKG_CONFIG --variable=Libraries $UNSTABLE_INSTALL_ASSEMBLIES`"

if test "x$with_framework" != "xno"; then
PKG_CONFIG="$OLDPKG_CONFIG"
fi

PKG_CHECK_MODULES([GLIB_SHARP], [glib-sharp-2.0])

AC_SUBST(INSTALLED_ASSEMBLIES)

dnl package checks, per config


AC_CONFIG_FILES([
cdn-rawc
Makefile
libcdnrawc/Makefile
libcdnrawc/Config.cs
libcdnrawc/codyn-rawc-sharp.pc
libcdnrawc/Programmer/Formatters/C/cdn-rawc/Makefile
libcdnrawc/Programmer/Formatters/C/cdn-rawc/cdn-rawc.pc
completion/Makefile
m4/Makefile
tests/Makefile
])

AC_OUTPUT
