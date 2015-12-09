namespace Module

open System.Collections.Generic
open System.Net.NetworkInformation

type Switch() = 
    inherit Module()
    member val Entries = List<PhysicalAddress>()