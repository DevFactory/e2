module E2.Main

open E2.Orchestrate

let example = """
vpn vpn0;
nat nat0;
firewall fw0;
ids ids0;

TC {
    fw0 -> ids0;
    fw0 -> nat0;
    ids0 -> nat0;
    nat0 -> vpn0;
}

"""

[<EntryPoint>]
let main args = 
    let mgr = Orchestrator example
    0