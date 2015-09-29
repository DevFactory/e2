namespace E2

open System.Collections.Generic
open System.Net.NetworkInformation
open System.Net

type Module() = 
    member val Id = Identifier.GetId()
    member val NextModules = List<Module>()

type Classifier() = 
    inherit Module()
    member val Filters = List<string>()

type LoadBalancer(isLastHop : bool, target : IPolicyVertex option) = 
    inherit Module()
    member val IsLastHop = isLastHop
    member val Target = target with get, set
    member val ReplicaDMAC = List<PhysicalAddress>()

type Switch() = 
    inherit Module()
    member val DMAC = List<PhysicalAddress>()

type VPortIn() = 
    inherit Module()

type VPortOut() = 
    inherit Module()

type VPortStruct(nf : IPlanVertex) = 
    inherit Module()
    member val NF = nf

type PPortIn() = 
    inherit Module()

type PPortOut() = 
    inherit Module()

type PPortStruct() = 
    inherit Module()

type Server(totalCores : int, addr : string) = 
    member val Id = Identifier.GetId()
    member val TotalCores = totalCores
    member val Address = addr
    member val VPortIn = List<VPortIn>()
    member val VPortOut = List<VPortOut>()
    member val VPort = List<VPortStruct>()
    member val FirstHopLB = LoadBalancer(false, None)
    member val LB = List<LoadBalancer>()
    member val CL = List<Classifier>()
    member val PPortIn = PPortIn()
    member val PPortOut = PPortOut()
    member val PPort = PPortStruct()
    member val Switch = Switch()
    member val Channel = ServerChannel(addr, 5555)
    member val Cores = Queue<int>([1..totalCores])
    member val NF = Dictionary<IPlanVertex, int>()

type ToRSwitch(ingressPort: int list, ip : IPAddress) = 
    member val L2 = List<PhysicalAddress * Server>()
    member val Port = Dictionary<Server, int>()
    member val Channel = SwitchChannel(IPEndPoint(ip, 2000))
    member val IngressPort = ingressPort