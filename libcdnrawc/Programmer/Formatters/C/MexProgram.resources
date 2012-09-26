#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>

#include "mex.h"

typedef struct
{
	int id;
	char const *name;
} StateItem;

static StateItem states_map[] =
{
${statemap}
};

#define NUM_STATES (sizeof (states_map) / sizeof (StateItem))

static int
lookup_monitor (char const *name)
{
	int i;
	
	for (i = 0; i < NUM_STATES; ++i)
	{
		if (strcmp (states_map[i].name, name) == 0)
		{
			return states_map[i].id;
		}
	}
	
	return -1;
}

static void
set_input_states (int nrhs,
                  mxArray const *prhs[],
                  int next)
{
	for (; next < nrhs; next += 2)
	{
		if (!mxIsChar (prhs[next]))
		{
			mexPrintf ("Expecting a state name for argument %d\n", next);
			mexErrMsgTxt ("Invalid arguments");
		}

		char *name = mxArrayToString (prhs[next]);

		if (next + 1 >= nrhs || !mxIsNumeric (prhs[next + 1]))
		{
			mexPrintf ("Expecting a value for state '%s'\n", name);
			mxFree (name);
			mexErrMsgTxt ("Invalid arguments");
		}

		int id = lookup_monitor (name);

		if (id == -1)
		{
			mexPrintf ("Couldn't find state '%s'\n", name);
			mxFree (name);
			mexErrMsgTxt ("Invalid arguments");
		}

		${name}_set (id, mxGetScalar (prhs[next + 1]));
		
		mxFree (name);
	}
}

static int
get_monitor_count (mxArray const *monitor_cells)
{
	if (mxIsCell (monitor_cells))
	{
		if (mxGetM (monitor_cells) * mxGetN (monitor_cells) == 0)
		{
			mexErrMsgTxt ("No monitor specified");
		}

		if (mxGetM (monitor_cells) > 1 && mxGetN (monitor_cells) > 1)
		{
			mexErrMsgTxt ("The monitor list must have one dimension");
		}

		return mxGetM (monitor_cells) > 1 ? mxGetM (monitor_cells) : mxGetN (monitor_cells);
	}

	if (!mxIsChar (monitor_cells))
	{
		mexErrMsgTxt ("Invalid value for monitors");
	}

	return 1;
}

static unsigned int
get_seed (int nrhs,
          mxArray const *prhs[],
          int *next)
{
	unsigned int seed;

	if (nrhs > *next && mxIsNumeric (prhs[*next]))
	{
		seed = mxGetScalar (prhs[*next]);
		++*next;
	}
	else
	{
		FILE *file = fopen("/dev/urandom", "r");
		int n_read = fread (&seed, sizeof (seed), 1, file);
		fclose (file);

		if (n_read != 1)
		{
			mexErrMsgTxt ("Couldn't read seed from /dev/urandom");
		}
	}

	return seed;
}

static int *
get_monitors (mxArray const *monitor_cells,
              int n_monitors)
{
	int *monitors = mxCalloc (n_monitors, sizeof (int));

	int i;
	for (i = 0; i < n_monitors; ++i)
	{
		char *name = mxArrayToString (mxIsCell (monitor_cells) ? mxGetCell (monitor_cells, i) : monitor_cells);

		/* Lookup in table */
		int id = lookup_monitor (name);
		
		if (id == -1)
		{
			mxFree (monitors);
			mexPrintf ("Could not find state to monitor: %s\n", name);
			mxFree (name);
			mexErrMsgTxt ("Couldn't find all monitors\n");
		}
		
		monitors[i] = id;

		mxFree (name);
	}

	return monitors;
}

void
mexFunction (int nlhs,
             mxArray *plhs[],
             int nrhs,
             mxArray const *prhs[])
{
	enum
	{
		PARAM_FROM,
		PARAM_TIMESTEP,
		PARAM_TO,
		PARAM_MONITORS,
		N_PARAMS
	};

	/* if no input and zero or one output, return cell array of all available states */
	if (nlhs <= 1 && nrhs == 0)
	{
		plhs[0] = mxCreateCellMatrix(NUM_STATES, 1);

		int i;

		for (i = 0; i < NUM_STATES; ++i)
		{
			mxSetCell (plhs[0], i, mxCreateString (states_map[i].name));
		}

		return;
	}

	if (nrhs < N_PARAMS)
	{
		mexErrMsgTxt ("Wrong number of input arguments. Call with no arguments to get a list of available states. Otherwise:\n"
		              "INPUTS:\n"
		              "\tStart time\n"
		              "\tTimestep\n"
		              "\tEnd time\n"
		              "\tState, or cell array of states, to monitor\n"
		              "\tSeed (optional)\n"
		              "\tState, Value, ...\n"
		              "OUTPUTS:\n"
		              "\tArray of monitored values, one row per timestep\n"
		              "\tSeed\n");
	}

	if (nlhs > 2)
	{
		mexErrMsgTxt ("Too many output arguments (max 2)");
	}

	double from = mxGetScalar (prhs[PARAM_FROM]);
	double timestep = mxGetScalar (prhs[PARAM_TIMESTEP]);
	double to = mxGetScalar (prhs[PARAM_TO]);
	
	if (to - (from + timestep) >= to - from)
	{
		mexErrMsgTxt ("Invalid simulation range specified");
	}

	mxArray const *monitor_cells = prhs[PARAM_MONITORS];
	int next = N_PARAMS;
	unsigned int seed = get_seed (nrhs, prhs, &next);

	int n_monitors = get_monitor_count (monitor_cells);
	int *monitors = get_monitors (monitor_cells, n_monitors);
	srand (seed);
	
	${name}_initialize (from);

	set_input_states (nrhs, prhs, next);

	to += timestep / 2;

	int n_steps = (to - from) / timestep + 1;
	int step;

	plhs[0] = mxCreateDoubleMatrix (n_steps, n_monitors, mxREAL);
	double *output = mxGetPr (plhs[0]);

	for (step = 0; step < n_steps; ++step)
	{
		int i;
		
		for (i = 0; i < n_monitors; ++i)
		{
			output[n_steps * i + step] = ${name}_get (monitors[i]);
		}

		${name}_step (timestep);
	}

	if (nlhs > 1)
	{
		plhs[1] = mxCreateDoubleScalar (seed);
	}

	mxFree (monitors);
}
