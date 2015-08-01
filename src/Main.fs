module E2.Main

open System

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
    let state = Parser.Parse example
    let policy = Policy() 
    policy.LoadPolicyState(state)
    let plan = Plan()
    plan.FromPolicyGraph(policy)
    0