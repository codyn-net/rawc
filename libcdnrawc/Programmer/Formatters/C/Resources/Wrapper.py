import ctypes

lib = ctypes.cdll.LoadLibrary("lib${name}.so")

valuetype = ctypes.c_${valuetype}

# Bind data structures
class CdnRawcRange(ctypes.Structure):
    _fields_ = [('start', ctypes.c_uint32),
                ('end', ctypes.c_uint32)]

class CdnRawcNetwork(ctypes.Structure):
    pass

class CdnRawcIntegrator(ctypes.Structure):
    pass

# Callback signatures
NetworkFuncT = ctypes.CFUNCTYPE(None, ctypes.POINTER(valuetype), valuetype)
NetworkFuncTDT = ctypes.CFUNCTYPE(None, ctypes.POINTER(valuetype), valuetype, valuetype)

IntegratorFunc = ctypes.CFUNCTYPE(None, ctypes.POINTER(CdnRawcIntegrator), ctypes.POINTER(CdnRawcNetwork), ctypes.POINTER(valuetype), valuetype, valuetype)

CdnRawcNetwork._fields_ = [('prepare', NetworkFuncT),
                ('init', NetworkFuncT),
                ('reset', NetworkFuncT),
                ('pre', NetworkFuncTDT),
                ('post', NetworkFuncTDT),
                ('states', CdnRawcRange),
                ('derivatives', CdnRawcRange),
                ('data_size', ctypes.c_uint32)]

CdnRawcIntegrator._fields_ = [('step', IntegratorFunc),
                ('diff', IntegratorFunc),
                ('data_size', ctypes.c_uint32)]

# Bind API using ctypes
cdn_rawc_network = lib.cdn_rawc_${name}_network
cdn_rawc_network.restype = ctypes.POINTER(CdnRawcNetwork)

# Default integrator
cdn_rawc_integrator = lib.cdn_rawc_${name}_integrator
cdn_rawc_integrator.restype = ctypes.POINTER(CdnRawcIntegrator)

cdn_rawc_integrator_euler = lib.cdn_rawc_integrator_euler
cdn_rawc_integrator_euler.restype = ctypes.POINTER(CdnRawcIntegrator)

cdn_rawc_integrator_runge_kutta = lib.cdn_rawc_integrator_runge_kutta
cdn_rawc_integrator_runge_kutta.restype = ctypes.POINTER(CdnRawcIntegrator)

cdn_rawc_integrator_step = lib.cdn_rawc_integrator_step
cdn_rawc_integrator_step.argtypes = [ctypes.POINTER(CdnRawcIntegrator), ctypes.POINTER(CdnRawcNetwork), ctypes.POINTER(valuetype), valuetype, valuetype]

cdn_rawc_init = lib.cdn_rawc_${name}_init
cdn_rawc_init.argtypes = [valuetype]

cdn_rawc_prepare = lib.cdn_rawc_${name}_prepare
cdn_rawc_prepare.argtypes = [valuetype]

cdn_rawc_reset = lib.cdn_rawc_${name}_reset
cdn_rawc_reset.argtypes = [valuetype]

network = cdn_rawc_network()

# Pythonic bindings
class Network:
    def __init__(self):
        self.network = cdn_rawc_network()
        self.set_integrator(Integrator(cdn_rawc_integrator()))

    @property
    def data_size(self):
        return self.network.contents.data_size

    def set_integrator(self, integrator):
        self.integrator = integrator

        # Create enough data
        self.data = (ctypes.POINTER(valuetype) * (self.data_size * self.integrator.data_size))()
        self.dataptr = ctypes.cast(self.data, ctypes.POINTER(valuetype))

    def init(self, t):
        self.network.contents.init(self.dataptr, t)

    def prepare(self, t):
        self.network.contents.prepare(self.dataptr, t)

    def reset(self, t):
        self.network.contents.reset(self.dataptr, t)

    def step(self, t, dt):
        self.integrator.step(self, t, dt)

class Integrator:
    def __init__(self, integrator):
        self.integrator = integrator

    @property
    def data_size(self):
        return self.integrator.contents.data_size

    def step(self, network, t, dt):
        cdn_rawc_integrator_step(self.integrator, network.network, network.dataptr, valuetype(t), valuetype(dt))

class Euler(Integrator):
    def __init__(self):
        Integrator.__init__(self, cdn_rawc_integrator_euler())

class RungeKutta(Integrator):
    def __init__(self):
        Integrator.__init__(self, cdn_rawc_integrator_runge_kutta())

__all__ = ['Network', 'Integrator', 'Euler', 'RungeKutta']

# vi:ts=4:et
