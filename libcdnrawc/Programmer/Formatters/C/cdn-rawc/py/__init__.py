import ctypes

valuetype = ctypes.c_double
valuetypeptr = ctypes.POINTER(valuetype)

# Bind data structures
class CdnRawcRange(ctypes.Structure):
    _fields_ = [('start', ctypes.c_uint32),
                ('end', ctypes.c_uint32),
                ('stride', ctypes.c_uint32)]

class CdnRawcDimension(ctypes.Structure):
    _fields_ = [('rows', ctypes.c_uint16),
                ('columns', ctypes.c_uint16)]

    @property
    def size(self):
        return self.rows * self.columns

    def __repr__(self):
        return '<{0} instance at 0x{1:x}, .rows={2}, .columns={3}>'.format(self.__class__.__name__,
                                                                           id(self),
                                                                           self.rows,
                                                                           self.columns)

class CdnRawcNetwork(ctypes.Structure):
    pass

class CdnRawcIntegrator(ctypes.Structure):
    pass

class CdnRawcStateMeta(ctypes.Structure):
    _fields_ = [
        ('name', ctypes.c_char_p),
        ('parent', ctypes.c_uint32),
        ('index', ctypes.c_uint32),
    ]

class CdnRawcNodeMeta(ctypes.Structure):
    _fields_ = [
        ('name', ctypes.c_char_p),
        ('parent', ctypes.c_uint32),
        ('first_child', ctypes.c_uint32),
        ('first_template', ctypes.c_uint32),
    ]

class CdnRawcChildMeta(ctypes.Structure):
    _fields_ = [
        ('parent', ctypes.c_uint32),
        ('is_node', ctypes.c_uint8),
        ('index', ctypes.c_uint32),
        ('next', ctypes.c_uint32),
    ]

class CdnRawcTemplateMeta(ctypes.Structure):
    _fields_ = [
        ('name', ctypes.c_char_p),
        ('parent', ctypes.c_uint32),
        ('next', ctypes.c_uint32),
    ]

class CdnRawcNetworkMeta(ctypes.Structure):
    _fields_ = [
        ('t', ctypes.c_uint32),
        ('dt', ctypes.c_uint32),
        ('name', ctypes.c_char_p),

        ('states', ctypes.POINTER(CdnRawcStateMeta)),
        ('states_size', ctypes.c_uint32),

        ('nodes', ctypes.POINTER(CdnRawcNodeMeta)),
        ('nodes_size', ctypes.c_uint32),

        ('children', ctypes.POINTER(CdnRawcChildMeta)),
        ('children_size', ctypes.c_uint32),

        ('templates', ctypes.POINTER(CdnRawcTemplateMeta)),
        ('templates_size', ctypes.c_uint32),
    ]

# Callback signatures
NetworkFuncT = ctypes.CFUNCTYPE(None, ctypes.c_void_p, valuetype)
NetworkFuncTDT = ctypes.CFUNCTYPE(None, ctypes.c_void_p, valuetype, valuetype)
NetworkFuncData = ctypes.CFUNCTYPE(None, ctypes.c_void_p)
NetworkFuncValueGetter = ctypes.CFUNCTYPE(valuetypeptr, ctypes.c_void_p)

IntegratorFunc = ctypes.CFUNCTYPE(None, ctypes.POINTER(CdnRawcIntegrator), ctypes.POINTER(CdnRawcNetwork), ctypes.c_void_p, valuetype, valuetype)

CdnRawcNetwork._fields_ = [('prepare', NetworkFuncT),
                           ('init', NetworkFuncT),
                           ('reset', NetworkFuncT),
                           ('pre', NetworkFuncTDT),
                           ('prediff', NetworkFuncData),
                           ('diff', NetworkFuncTDT),
                           ('post', NetworkFuncTDT),

                           ('events_update', NetworkFuncData),
                           ('events_post_update', NetworkFuncData),
                           ('events_fire', NetworkFuncData),

                           ('get_data', NetworkFuncValueGetter),
                           ('get_states', NetworkFuncValueGetter),
                           ('get_derivatives', NetworkFuncValueGetter),
                           ('get_nth', ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p, ctypes.c_uint32)),

                           ('get_events_active_size', ctypes.CFUNCTYPE(ctypes.c_uint32, ctypes.c_void_p)),
                           ('get_events_active', ctypes.CFUNCTYPE(ctypes.c_uint32, ctypes.c_void_p, ctypes.c_uint32)),
                           ('get_events_value', ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p, ctypes.c_uint32)),

                           ('get_dimension', ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.POINTER(CdnRawcDimension), ctypes.c_uint32)),

                           ('states', CdnRawcRange),
                           ('derivatives', CdnRawcRange),
                           ('event_values', CdnRawcRange),

                           ('dimensions', ctypes.c_void_p),

                           ('size', ctypes.c_uint32),
                           ('data_size', ctypes.c_uint32),
                           ('data_count', ctypes.c_uint32),

                           ('event_refinement', ctypes.c_uint8),
                           ('type_size', ctypes.c_uint8),

                           ('minimum_timestep', valuetype),

                           ('meta', CdnRawcNetworkMeta)]

CdnRawcIntegrator._fields_ = [('step', IntegratorFunc),
                              ('diff', IntegratorFunc),
                              ('order', ctypes.c_uint32)]

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
                                                  ctypes.c_void_p,
                                                  valuetype,
                                                  valuetype]

        # Raw CdnRawcNetwork API
        self.cdn_rawc_network_init = lib.cdn_rawc_network_init
        self.cdn_rawc_network_init.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                               ctypes.c_void_p,
                                               valuetype]

        self.cdn_rawc_network_prepare = lib.cdn_rawc_network_prepare
        self.cdn_rawc_network_prepare.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                  ctypes.c_void_p,
                                                  valuetype]

        self.cdn_rawc_network_reset = lib.cdn_rawc_network_reset
        self.cdn_rawc_network_reset.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                ctypes.c_void_p,
                                                valuetype]

        self.cdn_rawc_network_pre = lib.cdn_rawc_network_pre
        self.cdn_rawc_network_pre.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                              ctypes.c_void_p,
                                              valuetype,
                                              valuetype]

        self.cdn_rawc_network_post = lib.cdn_rawc_network_post
        self.cdn_rawc_network_post.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                               ctypes.c_void_p,
                                               valuetype,
                                               valuetype]

        self.cdn_rawc_network_diff = lib.cdn_rawc_network_diff
        self.cdn_rawc_network_diff.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                               ctypes.c_void_p,
                                               valuetype,
                                               valuetype]

        self.cdn_rawc_network_get_data = lib.cdn_rawc_network_get_data
        self.cdn_rawc_network_get_data.restype = valuetypeptr
        self.cdn_rawc_network_get_data.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                   ctypes.c_void_p]

        self.cdn_rawc_network_get_states = lib.cdn_rawc_network_get_states
        self.cdn_rawc_network_get_states.restype = valuetypeptr
        self.cdn_rawc_network_get_states.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                     ctypes.c_void_p]

        self.cdn_rawc_network_get_derivatives = lib.cdn_rawc_network_get_derivatives
        self.cdn_rawc_network_get_derivatives.restype = valuetypeptr
        self.cdn_rawc_network_get_derivatives.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                          ctypes.c_void_p]

        self.cdn_rawc_network_get_dimension = lib.cdn_rawc_network_get_dimension
        self.cdn_rawc_network_get_dimension.restype = ctypes.POINTER(CdnRawcDimension)
        self.cdn_rawc_network_get_dimension.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                          ctypes.c_uint32]

        self.cdn_rawc_network_alloc = lib.cdn_rawc_network_alloc
        self.cdn_rawc_network_alloc.restype = ctypes.c_void_p
        self.cdn_rawc_network_alloc.argtypes = [ctypes.POINTER(CdnRawcNetwork), ctypes.c_uint32]

        self.cdn_rawc_network_free = lib.cdn_rawc_network_free
        self.cdn_rawc_network_free.argtypes = [ctypes.c_void_p]

        self.cdn_rawc_network_find_variable = lib.cdn_rawc_network_find_variable
        self.cdn_rawc_network_find_variable.restype = ctypes.c_int32
        self.cdn_rawc_network_find_variable.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                        ctypes.c_char_p]

        self.cdn_rawc_network_meta_find_variable = lib.cdn_rawc_network_meta_find_variable
        self.cdn_rawc_network_meta_find_variable.restype = ctypes.c_uint32
        self.cdn_rawc_network_meta_find_variable.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                             ctypes.c_uint32,
                                                             ctypes.c_char_p]

        self.cdn_rawc_network_meta_find_node = lib.cdn_rawc_network_meta_find_node
        self.cdn_rawc_network_meta_find_node.restype = ctypes.c_uint32
        self.cdn_rawc_network_meta_find_node.argtypes = [ctypes.POINTER(CdnRawcNetwork),
                                                         ctypes.c_uint32,
                                                         ctypes.c_char_p]

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
    def __init__(self, api, libname=None):
        if isinstance(api, API):
            self.api = api
        else:
            self.api = load(api, libname)

        self.network = self.api.cdn_rawc_network()
        self.set_integrator(Integrator(self.api.cdn_rawc_integrator()))

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
    def data(self):
        return self.api.cdn_rawc_network_get_data(self.network, self.storage)

    @property
    def data_size(self):
        return self.network.contents.data_size

    @property
    def size(self):
        return self.network.contents.size

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
        self.storage = self.api.cdn_rawc_network_alloc(self.network, self.integrator.order)

    def init(self, t=0):
        self.api.cdn_rawc_network_init(self.network, self.storage, t)

    def prepare(self, t=0):
        self.api.cdn_rawc_network_prepare(self.network, self.storage, t)

    def reset(self, t=0):
        self.api.cdn_rawc_network_reset(self.network, self.storage, t)

    def pre(self, t, dt):
        self.api.cdn_rawc_network_pre(self.network, self.storage, t, dt)

    def post(self, t, dt):
        self.api.cdn_rawc_network_post(self.network, self.storage, t, dt)

    def diff(self, t, dt):
        self.api.cdn_rawc_network_diff(self.network, self.storage, t, dt)

    def step(self, dt):
        self.api.cdn_rawc_integrator_step(self.integrator.integrator,
                                          self.network,
                                          self.storage,
                                          self.t,
                                          dt)

    def step_diff(self, dt):
        self.api.cdn_rawc_integrator_step_diff(self.integrator.integrator,
                                               self.network,
                                               self.storage,
                                               self.t,
                                               dt)

    def get_dimension(self, i):
        return self.api.cdn_rawc_network_get_dimension(self.network, i).contents

class Integrator:
    def __init__(self, integrator):
        self.integrator = integrator

    @property
    def order(self):
        return self.integrator.contents.order

__all__ = ['Network', 'Integrator', 'Euler', 'RungeKutta']

# vi:ts=4:et
