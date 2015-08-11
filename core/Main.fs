module E2.Main

open System
open System.Collections.Generic
open CookComputing.XmlRpc

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

[<EntryPoint>]
let main args = 
//    let mgr = Orchestrator example
//    mgr.InitServer()
//    mgr.Init()
//    mgr.Apply()
    let channel = ServerChannel(System.Net.IPAddress.Parse("127.0.0.1"))
    0