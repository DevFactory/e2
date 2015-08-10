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
    member val Switch = Switch()
    member this.Policy = blueprint
    member this.Plan = Planner.InitialPlan(this.Policy)
    
    member this.InitServers() = 
        this.Servers.Add(Server(16, IPAddress.Parse("192.168.0.1")))
        this.Servers.Add(Server(16, IPAddress.Parse("192.168.0.2")))
        this.Servers.Add(Server(16, IPAddress.Parse("192.168.0.3")))
        this.Servers.Add(Server(16, IPAddress.Parse("192.168.0.4")))
        for server in this.Servers do
            let firstHopLB = LoadBalancer(false)
            server.LB.Add(firstHopLB)
            // PPort -> SW
            server.PPortIn.NextModules.Add(server.Switch)
            // SW -> PPort / Firsthop LB
            server.Switch.NextModules.Add(server.PPortOut)
            server.Switch.NextModules.Add(firstHopLB)
    
    member this.InitNF() = 
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
            let dmac = PhysicalAddress.Parse("06" + vnf.Id.ToString("D10"))
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
                    let dmac = PhysicalAddress.Parse("06" + v.Id.ToString("D10"))
                    lb.ReplicaDMAC.Add(dmac)
