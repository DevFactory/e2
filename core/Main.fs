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

type Server(cores: int) = 
    interface IServer with
        member this.AvailableCores = 10.0

[<EntryPoint>]
let main args = 
    let state = Parser.Parse example
    let policy = Policy() 
    policy.LoadPolicyState(state)
    let plan = Plan()
    plan.FromPolicyGraph(policy)

    printfn "%s" ((policy :> IVisualizable).Visualize())
    printfn "%s" ((plan :> IVisualizable).Visualize())

    let placement = Placement() :> IPlacement
    let servers = new List<IServer>()
    servers.Add(new Server(16))

    let dict = placement.Place plan servers
    for entry in dict do 
        printfn "nf: %A, server: %A" entry.Key.Id entry.Value

    0