module E2.Main

open System
open System.Collections.Generic

let example = """
Proxy proxy;
NAT nat;
Firewall fw;
Ports cp;
Ports ep;

TC {
    cp["dst port 80"] -> proxy;
    cp["!(dst port 80)"] -> nat;
    proxy -> nat;
    nat -> fw;
    fw["fw safe"] -> ep;
}


TC {
    ep -> nat;
    nat["src port 80"] -> proxy;
    nat["!(src port 80)"] -> fw;
    proxy -> fw;
    fw["fw safe"] -> cp;
}
"""

type Server(cores : int) = 
    interface IServer with
        member this.AvailableCores = 10.0

[<EntryPoint>]
let main args = 
    let mgr = Orchestrator example
    mgr.InitServers()
    mgr.InitNF()
    0