test_cdn_files = 		\
	dependencies.cdn	\
	events.cdn		\
	multiple_events.cdn	\
	functions.cdn		\
	indices.cdn		\
	integrate.cdn		\
	matrix.cdn		\
	simplemath.cdn

test_names = $(test_cdn_files:.cdn=.test)

TESTS = $(test_names)

$(test_names):
	$(AM_V_GEN) \
	echo "#!/bin/bash" > "$@"; \
	echo "srcdir=$(srcdir)" >> "$@"; \
	echo "files=${@:%.test=%.cdn}" >> "$@"; \
	cat runtest >> "$@"; \
	chmod +x "$@";

EXTRA_DIST = runtest $(test_cdn_files)
CLEANFILES = $(test_names)

.NOTPARALLEL:
