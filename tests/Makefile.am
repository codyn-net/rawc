test_cdn_files = 		\
	dependencies.cdn	\
	events.cdn		\
	events_multiple.cdn	\
	events_notrans.cdn	\
	functions.cdn		\
	indices.cdn		\
	integrate.cdn		\
	matrix.cdn		\
	simplemath.cdn		\
	vertcat.cdn

test_names = $(test_cdn_files:.cdn=.test)

TESTS = $(test_names)

$(test_names): runtest
	$(AM_V_GEN) \
	echo "#!/bin/bash" > "$@"; \
	echo "srcdir=$(srcdir)" >> "$@"; \
	echo "files=${@:%.test=%.cdn}" >> "$@"; \
	cat runtest >> "$@"; \
	chmod +x "$@";

EXTRA_DIST = runtest $(test_cdn_files)
CLEANFILES = $(test_names)

.NOTPARALLEL:
