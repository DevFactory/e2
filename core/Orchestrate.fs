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
open Scale

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

    member this.InitPlacement () = 
        Place state.Graph state.Hosts

    member this.AfterPlacement () =
        let stitch_modules (h: Host) (i: Instance) = 
            let inc = VPortInc()
            let out = VPortOut()
            let port = VPort(i)
            let cl = Classifier()
            h.OptionalModules.AddRange([inc; out; port; cl])

            h.Switch.NextModules.Add(out)
            h.Switch.Entries.Add(i.GetAddress())

            out.NextModules.Add(port)
            port.NextModules.Add(inc)
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
                final_lb.NextModules.Add(h.AffinityTracker)
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
                    lb.NextModules.Add(h.AffinityTracker)

            if Seq.isEmpty prev_edges then
                for h' in state.Hosts do
                    h'.LB.Destinations.Add(i.GetAddress())

            state.Switch.L2.Add(i.GetAddress(), h)

        state.Hosts |> Seq.iter (fun h ->
            let instances = h.VFI |> Seq.filter (fun i -> i.Status = Assigned)
            instances |> Seq.iter (fun i -> stitch_modules h i))

    member this.InvokeNetworkFunctions(h: Host) =
        let functions = h.VFI |> Seq.filter (fun i -> i.Status = Assigned)
        // TODO: Invoke functions
        ()

    member this.InitModules(h: Host) =
        let ensure b = if not b then failwith "Operation failed."

        let rec configure (current: Module) (indent: string) =
            match current with
            | :? Classifier ->
                printfn "%s%s" indent "cl"
                //h.Bess.CreateModule("classifier", current.Id.ToString()) |> ignore
            | :? LoadBalancer -> 
                printfn "%s%s" indent "lb"
                //h.Bess.CreateModule("loadbalancer", current.Id.ToString()) |> ignore
            | :? Switch -> 
                printfn "%s%s" indent "sw"
                //h.Bess.CreateModule("switch", current.Id.ToString()) |> ignore
            | :? VPortInc -> 
                printfn "%s%s" indent "vpin"
                //h.Bess.CreateModule("PortInc", current.Id.ToString()) |> ignore
            | :? VPortOut -> 
                printfn "%s%s" indent "vpout"
                //h.Bess.CreateModule("PortOut", current.Id.ToString()) |> ignore
            | :? VPort ->
                printfn "%s%s" indent "vp"
                //h.Bess.CreatePort("VPort", current.Id.ToString()) |> ignore
            | :? PPortInc ->
                printfn "%s%s" indent "ppin"
                //h.Bess.CreateModule("PortInc", current.Id.ToString()) |> ignore
            | :? PPortOut ->
                printfn "%s%s" indent "ppout"
                //h.Bess.CreateModule("PortOut", current.Id.ToString()) |> ignore
            | :? PPort -> 
                printfn "%s%s" indent "pp"
                //h.Bess.CreatePort("PMD", current.Id.ToString()) |> ignore
            | :? Mux ->
                printfn "%s%s" indent "mux"
            | :? AffinityTracker ->
                printfn "%s%s" indent "affinity"
            | _ -> 
                failwith "Unknown module type."

            current.IsCreated <- true

            let connect (cur: Module) (next: Module) i = 
                //h.Bess.ConnectModules(cur.Id.ToString(), next.Id.ToString(), i)
                ()

            current.NextModules |> Seq.iteri (fun i next -> connect current next (int64 i))

            current.NextModules |> Seq.filter (fun next -> not (next.IsCreated))
                                |> Seq.iter (fun next -> configure next (indent + "  "))
        
        let init () = 
            h.Bess.Connect() |> ensure
            h.Bess.PauseAll() |> ignore
        
        let finish () = 
            h.Bess.ResumeAll() |> ignore

        configure h.PPort ""

    member this.ConfigSwitch() =
        ()

    member this.MonitorHost(h: Host) =
        let testVPort (m: Module) = 
            match m with
            | :? VPort -> true
            | _ -> false
        let vports = h.OptionalModules |> Seq.filter testVPort |> Seq.map (fun m -> m :?> VPort)

        let query (vport : VPort) = 
            let associated_instance = vport.NF
            let vport_name = vport.Id.ToString()
            let stat = h.Bess.GetPortStats vport_name
            let in_edge = state.Graph.InInstanceEdge associated_instance
            let out_edge = state.Graph.OutInstanceEdge associated_instance
            // TODO: update in_edge and out_edge according to stat
            ()

        vports |> Seq.iter query

    member this.UpdateModules(h: Host) = 
        ()

    member this.Run() =
        this.InitPlacement()

        this.AfterPlacement()

        // Init BESS modules on each host
        state.Hosts |> Seq.iter this.InitModules
       
        // Configure the physical switch
        this.ConfigSwitch()

        // Run network functions on each host
        state.Hosts |> Seq.iter this.InvokeNetworkFunctions

        // Continously monitor the performance of each vport
        let rec loop () = 
            state.Hosts |> Seq.iter this.MonitorHost
            state.Graph |> Scale 

            loop()
        ()
        //loop()