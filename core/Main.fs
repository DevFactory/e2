module E2.Main

open System
open System.Collections.Generic
open CookComputing.XmlRpc

let example = """
vpn vpn0;
nat nat0;
firewall fw0;

TC {
    fw0 -> nat0;
    nat0 -> vpn0;
}

"""

[<EntryPoint>]
let main args = 
    let mgr = Orchestrator example
    mgr.InitServer()
    mgr.Init()
    mgr.Apply()
    0