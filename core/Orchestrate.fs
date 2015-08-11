namespace E2

open System
open System.Collections.Generic
open System.Net
open System.Net.NetworkInformation

type Orchestrator(conf : string) = 
    let state = Parser.Parse conf
    let blueprint = Policy()
    do blueprint.LoadPolicyState state
    member val Servers = List<Server>()
    member val ToR = ToRSwitch()
    member this.Policy = blueprint
    member this.Plan = Planner.InitialPlan(this.Policy)
    
    member this.InitServer () = 
        let servers = [("192.168.0.1", 16, 0);
                       ("192.168.0.2", 16, 1);
                       ("192.168.0.3", 16, 2);
                       ("192.168.0.4", 16, 3)]

        for (ip, cores, port) in servers do
            let server = Server(cores, IPAddress.Parse(ip))
            this.Servers.Add(server)
            this.ToR.Port.Add(server, port)

        for server in this.Servers do
            let firstHopLB = LoadBalancer(false)
            server.LB.Add(firstHopLB)
            // PPort -> SW
            server.PPortIn.NextModules.Add(server.Switch)
            // SW -> PPort / Firsthop LB
            server.Switch.NextModules.Add(server.PPortOut)
            server.Switch.NextModules.Add(firstHopLB)
    
    member this.Init () = 
        Planner.Scale this.Policy this.Plan
        let scheme = Placement.Place this.Plan this.Servers
        for entry in scheme do
            let server = entry.Value
            let vnf = entry.Key
            // Add vNF
            server.NF.Add(vnf)
            // Add vPortIn
            let vpin = VPortIn("sn" + string vnf.Id)
            server.VPortIn.Add(vpin)
            // Add vPortOut
            let vpout = VPortOut("sn" + string vnf.Id)
            server.VPortOut.Add(vpout)
            server.Switch.NextModules.Add(vpout)
            let dmac = PhysicalAddress.Parse("06" + vnf.Id.ToString("X10"))
            server.Switch.DMAC.Add(dmac)
            // Add CL 
            let cl = Classifier()
            server.CL.Add(cl)
            vpin.NextModules.Add(cl)
            // Enumerate all next hop NF
            let nextHopEdges = vnf.Parent |> (this.Policy :> IPolicy).OutEdges
            for e in nextHopEdges do
                // Make a new LB for each next hop NF
                let lb = LoadBalancer(false)
                cl.Filters.Add(e.Tag.Filter)
                cl.NextModules.Add(lb)
                lb.NextModules.Add(server.Switch)
                // Config LB for all vNFs
                let nextHopNF = e.Target
                let nextHopvNFs = this.Plan.FindPlanVertices nextHopNF
                for v in nextHopvNFs do
                    let dmac = PhysicalAddress.Parse("06" + v.Id.ToString("X10"))
                    lb.ReplicaDMAC.Add(dmac)
            // Finally set up L2 table 
            this.ToR.L2.Add(dmac, server)

    member this.ApplyServer (server : Server) = 
        // Run vNF
        let idleNF = server.NF |> Seq.filter (fun v -> not v.IsPlaced)
            
        // TODO: run idleNF on server
            
        idleNF |> Seq.iter (fun v -> (v.IsPlaced <- true))

        // TODO: 
        // 1. flush SoftNIC modules
        // 2. Setup PPort
        // 3. Setup E2Switch
        // 4. Setup VPorts
        // 5. Setup CL and LBs

    member this.ApplySwitch () = 
        // TODO: 
        // Setup L2
        for entry in this.ToR.L2 do
            let dmac = entry.Key
            let server = entry.Value
            let port = this.ToR.Port.[server]
            printfn "%A -> %d" dmac port
        ()

    member this.Apply () =
        this.Servers |> Seq.iter this.ApplyServer
        this.ApplySwitch ()