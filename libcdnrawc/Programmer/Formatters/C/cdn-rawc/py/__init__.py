import ctypes

valuetype = ctypes.c_double
valuetypeptr = ctypes.POINTER(valuetype)

# Bind data structures
class CdnRawcRange(ctypes.Structure):
    _fields_ = [('start', ctypes.c_uint32),
                ('end', ctypes.c_uint32)]

class CdnRawcNetwork(ctypes.Structure):
    pass

class CdnRawcIntegrator(ctypes.Structure):
    pass

class CdnRawcNetworkMeta(ctypes.Structure):
    _fields_ = [('t', ctypes.c_uint32),
                ('dt', ctypes.c_uint32),
                ('name', ctypes.c_char_p)]

# Callback signatures
NetworkFuncT = ctypes.CFUNCTYPE(None, valuetypeptr, valuetype)
NetworkFuncTDT = ctypes.CFUNCTYPE(None, valuetypeptr, valuetype, valuetype)

IntegratorFunc = ctypes.CFUNCTYPE(None, ctypes.POINTER(CdnRawcIntegrator), ctypes.POINTER(CdnRawcNetwork), valuetypeptr, valuetype, valuetype)

CdnRawcNetwork._fields_ = [('prepare', NetworkFuncT),
                           ('init', NetworkFuncT),
                           ('reset', NetworkFuncT),
                           ('pre', NetworkFuncTDT),
                           ('diff', NetworkFuncTDT),
                           ('post', NetworkFuncTDT),
                           ('states', CdnRawcRange),
                           ('derivatives', CdnRawcRange),
                           ('data_size', ctypes.c_uint32),
                           ('meta', CdnRawcNetworkMeta)]

CdnRawcIntegrator._fields_ = [('step', IntegratorFunc),
                              ('diff', IntegratorFunc),
                              ('data_size', ctypes.c_uint32)]

class API:
    def __init__(self, lib, name):
        # Raw CdnRawcIntegrator API
        self.lib = lib
        self.name = name

        # Try loading integrators
        for integrator in ['euler', 'runge_kutta']:
            fullname = 'cdn_rawc_integrator_' + integrator

            try:
                ptr = getattr(lib, fullname)
            except:
                continue

            ptr.restype = ctypes.POINTER(CdnRawcIntegrator)
            setattr(self, fullname, ptr)

        self.cdn_rawc_integrator_step = lib.cdn_rawc_integrator_step
        self.cdn_rawc_integrator_step.argtypes = [ctypes.POINTER(CdnRawcIntegrator),
                                                  ctypes.POINTER(CdnRawcNetwork),
                                                  valuetypeptr,
                                                  valuetype,
                                                  valuetype]

        # Raw CdnRawcNetwork API
        self.cdn_rawc_network_init = lib.cdn_rawc_network_init
        self.cdn_rawc_network_init.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                               valuetypeptr,
                                               valuetype]

        self.cdn_rawc_network_prepare = lib.cdn_rawc_network_prepare
        self.cdn_rawc_network_prepare.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                  valuetypeptr,
                                                  valuetype]

        self.cdn_rawc_network_reset = lib.cdn_rawc_network_reset
        self.cdn_rawc_network_reset.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                valuetypeptr,
                                                valuetype]

        self.cdn_rawc_network_pre = lib.cdn_rawc_network_pre
        self.cdn_rawc_network_pre.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                              valuetypeptr,
                                              valuetype,
                                              valuetype]

        self.cdn_rawc_network_post = lib.cdn_rawc_network_post
        self.cdn_rawc_network_post.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                               valuetypeptr,
                                               valuetype,
                                               valuetype]

        self.cdn_rawc_network_diff = lib.cdn_rawc_network_diff
        self.cdn_rawc_network_diff.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                               valuetypeptr,
                                               valuetype,
                                               valuetype]

        # Network specific API
        # Get the network spec for this network
        self.cdn_rawc_network = getattr(lib, 'cdn_rawc_' + name + '_network')
        self.cdn_rawc_network.restype = ctypes.POINTER(CdnRawcNetwork)

        # Get the default integrator for the network
        self.cdn_rawc_integrator = getattr(lib, 'cdn_rawc_' + name + '_integrator')
        self.cdn_rawc_integrator.restype = ctypes.POINTER(CdnRawcIntegrator)

    def Network(self):
        return Network(self)

    def Integrator(self, name):
        sym = 'cdn_rawc_integrator_' + name

        if hasattr(self, sym):
            return Integrator(getattr(self, sym)())
        else:
            return None

    def Euler(self):
        return self.Integrator('euler')

    def RungeKutta(self):
        return self.Integrator('runge_kutta')

def load(name, libname=None):
    if not libname:
        libname = "lib" + name + ".so"

    lib = ctypes.cdll.LoadLibrary(libname)
    return API(lib, name)

# Pythonic bindings
class Network:
    def __init__(self, api):
        self.api = api

        self.network = api.cdn_rawc_network()
        self.set_integrator(Integrator(api.cdn_rawc_integrator()))

    class DataIter:
        def __init__(self, network, slic=None):
            self.network = network

            ds = self.network.data_size

            if slic:
                self.slice = slic

                def clip(lower, upper, val):
                    return min(max(val, lower), upper)

                self.slice = [clip(0, ds, x) for x in self.slice]
            else:
                self.slice = (0, self.network.data_size)

            self.ptr = self.slice[0]

        def __len__(self):
            return int(self.slice[1] - self.ptr)

        def __getitem__(self, i):
            return self.network[self.ptr + i]

        def __setitem__(self, i, val):
            self.network[self.ptr + i] = val

        def __iter__(self):
            return self

        def next(self):
            if self.ptr >= self.slice[1]:
                raise StopIteration
            else:
                ret = self.network.data[self.ptr]
                self.ptr += 1

                return ret

    def __iter__(self):
        return Network.DataIter(self)

    def __len__(self):
        return int(self.data_size)

    def __getitem__(self, i):
        if i < 0 or i >= self.data_size:
            raise IndexError
        else:
            return self.data[i]

    def __setitem__(self, i, val):
        if i < 0 or i >= self.data_size:
            raise IndexError
        else:
            self.data[i] = val

    @property
    def states(self):
        return Network.DataIter(self, (self.network.contents.states.start,
                                       self.network.contents.states.end))

    @property
    def derivatives(self):
        return Network.DataIter(self, (self.network.contents.derivatives.start,
                                       self.network.contents.derivatives.end))

    @property
    def data_size(self):
        return self.network.contents.data_size

    @property
    def name(self):
        return self.network.contents.meta.name

    @property
    def t(self):
        return self.data[self.network.contents.meta.t]

    @property
    def dt(self):
        return self.data[self.network.contents.meta.dt]

    def set_integrator(self, integrator):
        self.integrator = integrator

        # Create enough data
        self.data = (valuetype * (self.data_size * self.integrator.data_size))()
        self.dataptr = ctypes.cast(self.data, valuetypeptr)

    def init(self, t):
        self.api.cdn_rawc_network_init(self.network, self.dataptr, t)

    def prepare(self, t):
        self.api.cdn_rawc_network_prepare(self.network, self.dataptr, t)

    def reset(self, t):
        self.api.cdn_rawc_network_reset(self.network, self.dataptr, t)

    def pre(self, t, dt):
        self.api.cdn_rawc_network_pre(self.network, self.dataptr, t, dt)

    def post(self, t, dt):
        self.api.cdn_rawc_network_post(self.network, self.dataptr, t, dt)

    def diff(self, t, dt):
        self.api.cdn_rawc_network_diff(self.network, self.dataptr, t, dt)

    def step(self, dt):
        self.api.cdn_rawc_integrator_step(self.integrator.integrator,
                                          self.network,
                                          self.dataptr,
                                          self.t,
                                          dt)

    def step_diff(self, dt):
        self.api.cdn_rawc_integrator_step_diff(self.integrator.integrator,
                                               self.network,
                                               self.dataptr,
                                               self.t,
                                               dt)

class Integrator:
    def __init__(self, integrator):
        self.integrator = integrator

    @property
    def data_size(self):
        return self.integrator.contents.data_size

__all__ = ['Network', 'Integrator', 'Euler', 'RungeKutta']

# vi:ts=4:et
