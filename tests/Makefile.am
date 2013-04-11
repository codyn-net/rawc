test_cdn_files = $(wildcard *.cdn)
test_names = $(foreach i,$(test_cdn_files),${i:%.cdn=%.test})

TESTS = $(test_names)

$(test_names):
	$(AM_V_GEN) \
	echo "#!/bin/bash" > "$@"; \
	echo "srcdir=$(srcdir)" >> "$@"; \
	echo "files=${@:%.test=%.cdn}" >> "$@"; \
	cat runtest >> "$@"; \
	chmod +x "$@";

EXTRA_DIST = runtests $(test_cdn_files)
CLEANFILES = $(test_names)

.NOTPARALLEL:
