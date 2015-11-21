module E2.Resources

open System.Collections.Generic
open System.Net.NetworkInformation
open System.Net
open Graph

type Module() = 
    member val Id = Identifier.GetId()
    member val NextModules = List<Module>()

type ModuleClassifier() = 
    inherit Module()
    member val Filters = List<string>()

type ModuleLoadBalancer(isLastHop : bool) = 
    inherit Module()
    member val IsLastHop = isLastHop
    member val Destinations = List<PhysicalAddress>()

type ModuleSwitch() = 
    inherit Module()
    member val Entries = List<PhysicalAddress>()

type ModuleVPortInc() = 
    inherit Module()

type ModuleVPortOut() = 
    inherit Module()

type ModuleVPortStruct(nf : Instance) = 
    inherit Module()
    member val NF = nf

type ModulePPortInc() = 
    inherit Module()

type ModulePPortOut() = 
    inherit Module()

type HostSpec = {
    Address: string;
    Cores: int;
    SwitchPort: int;
}

type Host(spec: HostSpec) =
    let pp_inc = ModulePPortInc()
    let pp_out = ModulePPortOut()
    let sw = ModuleSwitch()
    let lb = ModuleLoadBalancer(false)

    do pp_inc.NextModules.Add(sw)
    do sw.NextModules.Add(pp_out)
    do sw.NextModules.Add(lb)

    member val Channel = ServerChannel(spec.Address, 5555)

    member val FreeCores = spec.Cores with get, set
    member val VFI = List<Instance>() 
    
    member this.PPortInc = pp_inc
    member this.PPortOut = pp_out
    member this.Switch = sw
    member this.LB = lb
    
    member val OptionalModules = List<Module>()
    

type Switch(ingressPort: int list, ip : IPAddress) = 
    member val L2 = List<PhysicalAddress * Host>()
    member val Port = Dictionary<Host, int>()
    member val Channel = SwitchChannel(IPEndPoint(ip, 2000))
    member val IngressPort = ingressPort