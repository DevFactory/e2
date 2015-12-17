module Host

open System.Collections.Generic
open System.Net.NetworkInformation
open System.Net
open Graph
open Bess
open Module

type HostSpec = {
    Address: IPAddress;
    Cores: int;
    SwitchPort: int;
}

type Host(spec: HostSpec) =
    let inc = PPortInc()
    let out = PPortOut()
    let port = PPort()
    let switch = Switch()
    let lb = LoadBalancer(false)
    let affinity = AffinityTracker()
    let mux = Mux()

    do port.NextModules.Add(inc)
    do inc.NextModules.Add(mux)
    do mux.NextModules.Add(switch)
    do mux.NextModules.Add(lb)
    do switch.NextModules.Add(out)
    do out.NextModules.Add(port)

    member val Bess = Bess(spec.Address, 10514)

    member val FreeCores = spec.Cores with get, set
    member val VFI = List<Instance>() 

    member this.PPort = port
    member this.PPortInc = inc
    member this.PPortOut = out
    member this.Switch = switch
    member this.LB = lb
    member this.AffinityTracker = affinity
    member this.Mux = mux
    
    member val OptionalModules = List<Module>()
    
