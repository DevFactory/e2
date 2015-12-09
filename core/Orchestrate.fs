module Orchestrate

open System
open System.Collections.Generic
open System.Net
open System.Net.NetworkInformation

open Graph
open Placement
open Module
open Host
open HardwareSwitch

type State = {
    Graph: Graph;
    Hosts: List<Host>
    Switch: HardwareSwitch;
}

type Orchestrator(conf : string) =
    let state = {
        Graph = Graph();
        Hosts = List<Host>();
        Switch = HardwareSwitch([ 17; 18; 19; 20 ], IPAddress.Parse("127.0.0.1"))
    }

    // Initialize Graph
    do conf |> (Parser.Parse) |> (state.Graph.LoadFromParseState)

    // Initialize Hosts
    let makeSpec addr cores port =
        { Address = addr; Cores = cores; SwitchPort = port }
    let specs = [
        makeSpec (IPAddress.Parse("127.0.0.1")) 15 46;
    ]

    do specs |> Seq.iter (fun spec ->
        let h = Host(spec)
        state.Hosts.Add(h)
        state.Switch.Port.Add(h, spec.SwitchPort))

    do Place state.Graph state.Hosts

    let setup (h: Host) (i: Instance) =
        let inc = VPortInc()
        let out = VPortOut()
        let vp = VPort(i)
        let cl = Classifier()
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
            let final_lb = LoadBalancer(true)
            h.OptionalModules.Add(final_lb)

            cl.Filters.Add("true")
            cl.NextModules.Add(final_lb)

            // Intentionally avoid cycles
            // final_lb.NextModules.Add(h.Switch)
        else
            for e in next_edges do
                let node' = e.Target
                let lb = LoadBalancer(false)
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

    member this.InvokeNetworkFunctions(h: Host) =
        let functions = h.VFI |> Seq.filter (fun i -> i.Status = Assigned)
        // TODO: Invoke functions
        ()

    member this.InitModules(h: Host) =
        let ensure b = if not b then failwith "Operation failed."

        let rec configure (m: Module) =
            match m with
            | :? Classifier -> ()
            | :? LoadBalancer -> ()
            | :? Switch -> ()
            | :? VPortInc -> ()
            | :? VPortOut -> ()
            | :? VPort -> ()
            | :? PPortInc -> ()
            | :? PPortOut -> ()
            | :? PPort -> ()
            | _ -> failwith "Unknown module type."
            m.NextModules |> Seq.iter configure

        h.Bess.Connect() |> ensure
        h.Bess.PauseAll() |> ignore // TODO: Shouldn't ignore return value?

        configure h.PPortInc

    member this.ConfigSwitch() =
        ()

    member this.MonitorHost(h: Host) =
        ()

    member this.Loop() =
        ()
