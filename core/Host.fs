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
    let pp_inc = PPortInc()
    let pp_out = PPortOut()
    let sw = Switch()
    let lb = LoadBalancer(false)

    do pp_inc.NextModules.Add(sw)
    do sw.NextModules.Add(pp_out)
    do sw.NextModules.Add(lb)

    // member val Channel = ServerChannel(spec.Address, 5555)
    member val Bess = Bess(spec.Address, 10514)

    member val FreeCores = spec.Cores with get, set
    member val VFI = List<Instance>() 
    
    member this.PPortInc = pp_inc
    member this.PPortOut = pp_out
    member this.Switch = sw
    member this.LB = lb
    
    member val OptionalModules = List<Module>()
    
