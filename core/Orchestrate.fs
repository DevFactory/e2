module E2.Orchestrate

open System
open System.Collections.Generic
open System.Net
open System.Net.NetworkInformation
open log4net

open Graph
open Placement
open Resources

type State = {
    Graph: Graph;
    Hosts: List<Host>
    Switch: Switch;
}

let handleResponse(resp : Response) = 
    match resp.code with
    | 0 -> ()
    //| -1 -> () // Not Implemented
    | _ -> failwith (sprintf "Error code: %d, Message: %s" resp.code resp.msg)
    
let handleSwitchResponse code = 
    match code with
    | 0 -> ()
    | x -> failwith (sprintf "Error code: %d" x)

type Orchestrator(conf : string) =
    let log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

    let state = { 
        Graph = Graph(); 
        Hosts = List<Host>();
        Switch = Switch([ 17; 18; 19; 20 ], IPAddress.Parse("127.0.0.1"))
    }

    // Initialize Graph
    do conf |> (Parser.Parse) |> (state.Graph.LoadFromParseState)
    
    // Initialize Hosts
    let makeSpec addr cores port =
        { Address = addr; Cores = cores; SwitchPort = port }
    let specs = [
        makeSpec "c34.millennium.berkeley.edu" 15 46;
        makeSpec "c35.millennium.berkeley.edu" 15 46;
    ]

    do specs |> Seq.iter (fun spec ->
        let h = Host(spec)
        state.Hosts.Add(h)
        state.Switch.Port.Add(h, spec.SwitchPort))
    
    do Place state.Graph state.Hosts

    let setup (h: Host) (i: Instance) =
        let inc = ModuleVPortInc()
        let out = ModuleVPortOut()
        let vp = ModuleVPortStruct(i)
        let cl = ModuleClassifier()
        h.OptionalModules.AddRange([inc; out; vp; cl])

        h.Switch.NextModules.Add(out)
        h.Switch.Entries.Add(i.GetAddress())

        out.NextModules.Add(vp)
        vp.NextModules.Add(inc)
        inc.NextModules.Add(cl)

        let node = state.Graph.Nodes |> Seq.filter (fun n -> n.Instances.Contains(i)) |> Seq.exactlyOne
        let next_edges = state.Graph.OutEdge node
        let prev_edges = state.Graph.InEdge node

        if Seq.isEmpty next_edges then
            let final_lb = ModuleLoadBalancer(true)
            h.OptionalModules.Add(final_lb)

            cl.Filters.Add("true")
            cl.NextModules.Add(final_lb)

            // Intentionally avoid cycles
            // final_lb.NextModules.Add(h.Switch)
        else
            for e in next_edges do
                let node' = e.Target
                let lb = ModuleLoadBalancer(false)
                h.OptionalModules.Add(lb)

                cl.Filters.Add("true")
                cl.NextModules.Add(lb)

                // Intentionally avoid cycles
                // lb.NextModules.Add(h.Switch)
                lb.Destinations.AddRange(node'.Instances |> Seq.map (fun j -> j.GetAddress()))

        if Seq.isEmpty prev_edges then
            for h' in state.Hosts do
                h'.LB.Destinations.Add(i.GetAddress())

        state.Switch.L2.Add(i.GetAddress(), h)
    do state.Hosts |> Seq.iter (fun h -> h.VFI |> Seq.iter (fun i -> setup h i))

    member this.ConfigHost(h: Host) = 
        let rec configure (m: Module) = 
            match m with
            | :? ModuleClassifier -> ()
            | :? ModuleLoadBalancer -> ()
            | :? ModuleSwitch -> ()
            | :? ModuleVPortInc -> ()
            | :? ModuleVPortOut -> ()
            | :? ModuleVPortStruct -> ()
            | :? ModulePPortInc -> ()
            | :? ModulePPortOut -> ()
            | _ -> failwith "Unknown module type."
            m.NextModules |> Seq.iter configure
        configure h.PPortInc
    
    member this.ConfigSwitch() = 
        ()
    
    member this.MonitorHost(h: Host) = 
        ()

    member this.Loop() = 
        ()