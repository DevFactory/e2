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

type LoadBalancer(isLastHop : bool) = 
    inherit Module()
    member val IsLastHop = isLastHop
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

type Server(totalCores : int, ip : IPAddress) = 
    member val Id = Identifier.GetId()
    member val TotalCores = totalCores
    member val IPAddress = ip
    member val NF = List<IPlanVertex>()
    member val VPortIn = List<VPortIn>()
    member val VPortOut = List<VPortOut>()
    member val VPort = List<VPortStruct>()
    member val FirstHopLB = LoadBalancer(false)
    member val LB = List<LoadBalancer>()
    member val CL = List<Classifier>()
    member val PPortIn = PPortIn()
    member val PPortOut = PPortOut()
    member val PPort = PPortStruct()
    member val Switch = Switch()
    member val Channel = ServerChannel(IPEndPoint(ip, 5555))
    member this.AvailableCores = float (this.TotalCores - this.NF.Count)

type ToRSwitch(ingressPort: int list, ip : IPAddress) = 
    member val L2 = Dictionary<PhysicalAddress, Server>()
    member val Port = Dictionary<Server, int>()
    member val Channel = SwitchChannel(IPEndPoint(ip, 2000))
    member val IngressPort = ingressPort