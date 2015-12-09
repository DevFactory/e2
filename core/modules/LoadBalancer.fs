namespace Module

open System.Collections.Generic
open System.Net.NetworkInformation

type LoadBalancer(isLastHop : bool) = 
    inherit Module()
    member val IsLastHop = isLastHop
    member val Destinations = List<PhysicalAddress>()