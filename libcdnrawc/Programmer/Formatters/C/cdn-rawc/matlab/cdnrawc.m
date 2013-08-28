classdef cdnrawc < handle
	properties(Access=private)
		integrator
		network
		data
	end

	properties
		libname
		name
		t
		dt
		states
		derivatives
	end

	methods
		function obj = cdnrawc(name, libname, headerloc)
			% Try to load the lib using 'name'
			if nargin < 2
				libname = ['lib' name '.so'];
			end

			lname = ['lib' name];

			if libisloaded(lname)
				unloadlibrary(lname);
			end

			if nargin < 3
				[status, result] = system('pkg-config --variable=includedir cdn-rawc-1.0');

				if status ~= 0
					error('Could not find cdn-rawc support headers');
				end

				headerloc = fullfile(result(1:end-1), 'cdn-rawc-1.0');
			end

			% Generate temporary header file including needed wrapper
			% header files
			tmpf = [tempname '.h'];
			fid = fopen(tmpf, 'w');

			fprintf(fid, '#include <cdn-rawc/cdn-rawc-types.h>\n');
			fprintf(fid, '#include <cdn-rawc/cdn-rawc-network.h>\n');
			fprintf(fid, '#include <cdn-rawc/cdn-rawc-integrator.h>\n\n');

			fprintf(fid, '#include <cdn-rawc/integrators/cdn-rawc-integrator-euler.h>\n');
			fprintf(fid, '#include <cdn-rawc/integrators/cdn-rawc-integrator-runge-kutta.h>\n\n');

			fprintf(fid, 'CdnRawcNetwork *cdn_rawc_%s_network ();\n', name);
			fprintf(fid, 'CdnRawcIntegrator *cdn_rawc_%s_integrator ();\n\n', name);

			fclose(fid);

			warning off 'MATLAB:loadlibrary:FunctionNotFound';

			% Load library with this temporary header file
			[~, ~] = loadlibrary(libname, tmpf,...
			                     'includepath', headerloc,...
			                     'addheader', 'cdn-rawc/cdn-rawc-types',...
			                     'addheader', 'cdn-rawc/cdn-rawc-network',...
			                     'addheader', 'cdn-rawc/cdn-rawc-integrator',...
			                     'addheader', 'cdn-rawc/integrators/cdn-rawc-integrator-euler',...
			                     'addheader', 'cdn-rawc/integrators/cdn-rawc-integrator-runge-kutta');

			warning on 'MATLAB:loadlibrary:FunctionNotFound';

			delete(tmpf);

			obj.libname = lname;
			obj.name = name;

			obj.network = obj.cdn_rawc_network();
			obj.set_integrator(obj.cdn_rawc_integrator());
		end

		function set_integrator(obj, integrator)
			obj.integrator = integrator;

			% Allocate enough data
			num = zeros(obj.network.value.data_size * obj.integrator.value.data_size, 1);

			obj.data = libpointer('doublePtr', num);
		end

		function ret = get.states(obj)
			range = obj.network.value.states;
			ret = obj.slice((range.start + 1:range.end));
		end

		function ret = get.derivatives(obj)
			range = obj.network.value.derivatives;
			ret = obj.slice((range.start + 1:range.end));
		end

		function ret = slice(obj, sub)
			ret	= zeros(size(sub));

			for i = 1:numel(sub)
				ret(i) = obj.value(sub(i));
			end
		end

		function varargout = subsref(obj, s)
			switch s(1).type
				case '.'
					[varargout{1:nargout}] = builtin('subsref', obj, s);
				case '()'
					% Slice using first arg
					sub = s.subs{1};
					varargout{1} = obj.slice(sub);
				case '{}'
					error('cdnrawc:subsref', 'Not a supported subscripted reference');
			end
		end

		function ret = end(obj)
			ret = obj.network.value.data_size;
		end

		function ret = length(obj)
			ret = obj.network.value.data_size;
		end

		function ret = value(obj, idx)
			upper = length(obj);

			if idx < 1 || idx > obj.network.value.data_size
				error('Index out of bounds %d must be within [1, %d]', idx, upper);
			end

			ptr = obj.data + (idx - 1);
			setdatatype(ptr, 'doublePtr', 1, 1);
			ret = ptr.value;
		end

		function value = get.t(obj)
			value = obj.value(obj.network.value.meta.t + 1);
		end

		function value = get.dt(obj)
			value = obj.value(obj.network.value.meta.dt + 1);
		end

		function init(obj, t)
			obj.cdn_rawc_network_init(t);
		end

		function prepare(obj, t)
			obj.cdn_rawc_network_prepare(t);
		end

		function reset(obj, t)
			obj.cdn_rawc_network_reset(t);
		end

		function diff(obj, t)
			obj.cdn_rawc_network_diff(t);
		end

		function step(obj, dt)
			obj.cdn_rawc_integrator_step(obj.t, dt);
		end

		function step_diff(obj, dt)
			obj.cdn_rawc_integrator_step_diff(obj.t, dt);
		end
	end

	methods(Access=private)
		function out = cdn_rawc_integrator(obj)
			out = calllib(obj.libname, ['cdn_rawc_' obj.name '_integrator']);
		end

		function out = cdn_rawc_network(obj)
			out = calllib(obj.libname, ['cdn_rawc_' obj.name '_network']);
		end

		function cdn_rawc_network_init(obj, t)
			calllib(obj.libname, 'cdn_rawc_network_init', obj.network, obj.data, t);
		end

		function cdn_rawc_network_prepare(obj, t)
			calllib(obj.libname, 'cdn_rawc_network_prepare', obj.network, obj.data, t);
		end

		function cdn_rawc_network_reset(obj, t)
			calllib(obj.libname, 'cdn_rawc_network_reset', obj.network, obj.data, t);
		end

		function cdn_rawc_network_diff(obj, t)
			calllib(obj.libname, 'cdn_rawc_network_diff', obj.network, obj.data, t);
		end

		function cdn_rawc_integrator_step(obj, t, dt)
			calllib(obj.libname, 'cdn_rawc_integrator_step', obj.integrator, obj.network, obj.data, t, dt);
		end

		function cdn_rawc_integrator_step_diff(obj, t, dt)
			calllib(obj.libname, 'cdn_rawc_integrator_step_diff', obj.integrator, obj.network, obj.data, t, dt);
		end
	end
end
