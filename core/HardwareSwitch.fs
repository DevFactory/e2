module HardwareSwitch

open System.Net.NetworkInformation
open System.Net
open System.Collections.Generic
open Host

type HardwareSwitch(ingressPort: int list, ip : IPAddress) = 
    member val L2 = List<PhysicalAddress * Host>()
    member val Port = Dictionary<Host, int>()
    member val IngressPort = ingressPort