#define CDN_MATH_REQUIRES_TAN
#define ValueType double

#include "cdn-rawc-math.c"
#include <stdio.h>

int
main (int argc, char *argv[])
{
	printf ("%g\n", CDN_MATH_TAN (2.3));
	return 0;
}
