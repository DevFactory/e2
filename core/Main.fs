module E2.Main

open System
open System.Collections.Generic
open CookComputing.XmlRpc

let example = """
VPN vpn;
NAT nat;
Firewall fw;

TC {
    fw -> nat;
    nat -> vpn;
}

"""

[<EntryPoint>]
let main args = 
    let mgr = Orchestrator example
    mgr.InitServer()
    mgr.Init()
    mgr.Apply()
    0