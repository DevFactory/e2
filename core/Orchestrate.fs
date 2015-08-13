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
    member val ToR = ToRSwitch(IPAddress.Parse("127.0.0.1"))
    member this.Policy = blueprint
    member this.Plan = Planner.InitialPlan(this.Policy)
    
    member this.InitServer() = 
        printfn "Init servers..."
        //        let servers = [("192.168.0.1", 16, 0);
        //                       ("192.168.0.2", 16, 1);
        //                       ("192.168.0.3", 16, 2);
        //                       ("192.168.0.4", 16, 3)]
        let servers = [ ("127.0.0.1", 16, 1) ]
        for (ip, cores, port) in servers do
            let server = Server(cores, IPAddress.Parse(ip))
            this.Servers.Add(server)
            this.ToR.Port.Add(server, port)
        for server in this.Servers do
            // PPort -> SW
            server.PPortIn.NextModules.Add(server.Switch)
            // Firsthop LB -> SW
            server.FirstHopLB.NextModules.Add(server.Switch)
            // SW -> PPort / Firsthop LB
            server.Switch.NextModules.Add(server.PPortOut)
            server.Switch.NextModules.Add(server.FirstHopLB)
    
    member this.Init() = 
        //Planner.Scale this.Policy this.Plan
        // FIXME: Assume only one first-hop NF
        let firstHopVNF = 
            this.Plan.Vertices |> Seq.filter (fun vnf -> 
                                      vnf
                                      |> this.Plan.InEdges
                                      |> Seq.isEmpty)
        for server in this.Servers do
            let dmacs = firstHopVNF |> Seq.map (fun vnf -> PhysicalAddress.Parse("06" + vnf.Id.ToString("X10")))
            server.FirstHopLB.ReplicaDMAC.AddRange(dmacs)
        let scheme = Placement.Place this.Plan this.Servers
        for entry in scheme do
            let server = entry.Value
            let vnf = entry.Key
            let dmac = PhysicalAddress.Parse("06" + vnf.Id.ToString("X10"))
            // Add vNF
            server.NF.Add(vnf)
            // Add vPort
            let vp = VPortStruct(vnf)
            server.VPort.Add(vp)
            // Add vPortIn
            let vpin = VPortIn()
            server.VPortIn.Add(vpin)
            // Add vPortOut
            let vpout = VPortOut()
            server.VPortOut.Add(vpout)
            server.Switch.NextModules.Add(vpout)
            server.Switch.DMAC.Add(dmac)
            vpout.NextModules.Add(vp)
            vp.NextModules.Add(vpin)
            // Add CL 
            let cl = Classifier()
            server.CL.Add(cl)
            vpin.NextModules.Add(cl)
            // Enumerate all next hop NF
            let nextHopEdges = vnf.Parent |> (this.Policy :> IPolicy).OutEdges
            for e in nextHopEdges do
                // Make a new LB for each next hop NF
                let lb = LoadBalancer(false)
                lb.NextModules.Add(server.Switch)
                cl.Filters.Add(e.Tag.Filter)
                cl.NextModules.Add(lb)
                // Config LB for all vNFs
                let nextHopNF = e.Target
                let nextHopvNFs = this.Plan.FindPlanVertices nextHopNF
                for v in nextHopvNFs do
                    let dmac = PhysicalAddress.Parse("06" + v.Id.ToString("X10"))
                    lb.ReplicaDMAC.Add(dmac)
            // Finally set up L2 table 
            this.ToR.L2.Add(dmac, server)
    
    // Will refactor using BFS. The code should be much more succinct.
    member this.ApplyServer(server : Server) = 
        let HandleResponse (resp : Response) =
            match resp.code with
            | 0 -> ()
            | -1 -> () // Not Implemented
            | _ -> failwith (sprintf "Error code: %d, Message: %s" resp.code resp.msg)

        server.Channel.Agent.LaunchSoftNIC([| 0 |]) |> HandleResponse
        // TODO: 
        // 1. flush SoftNIC modules
        // 2. Setup PPort
        server.Channel.Agent.CreatePPort("pport") |> HandleResponse
        server.Channel.Agent.CreateModule("PortOut", string server.PPortOut.Id) |> HandleResponse
        server.Channel.Agent.CreateModule("PortInc", string server.PPortIn.Id) |> HandleResponse
        // TODO: configure PortOut and PortInc
        // 3. Setup E2Switch
        server.Channel.Agent.CreateModule("E2Switch", string server.Switch.Id) |> HandleResponse
        server.Channel.Agent.ConnectModule(string server.PPortIn.Id, 0, string server.Switch.Id) |> HandleResponse
        server.Channel.Agent.ConnectModule(string server.Switch.Id, 0, string server.PPortOut.Id) |> HandleResponse
        // Setup first-hop LB
        server.Channel.Agent.CreateModule("E2LB", string server.FirstHopLB.Id) |> HandleResponse
        server.Channel.Agent.ConnectModule(string server.Switch.Id, 1, string server.FirstHopLB.Id) |> HandleResponse
        server.Channel.Agent.ConnectModule(string server.FirstHopLB.Id, 0, string server.Switch.Id) |> HandleResponse
        // TODO: configure first-hop LB
        // 4. Setup VPorts
        for vout in server.VPortOut do
            let vp = vout.NextModules.[0]
            let vin = vp.NextModules.[0]
            server.Channel.Agent.CreateVPort(string vp.Id) |> HandleResponse
            server.Channel.Agent.CreateModule("PortInc", string vin.Id) |> HandleResponse
            server.Channel.Agent.CreateModule("PortOut", string vout.Id) |> HandleResponse
            // TODO: configure PortOut and PortInc
            let gate = server.Switch.NextModules.IndexOf(vout) + 2
            server.Channel.Agent.ConnectModule(string server.Switch.Id, gate, string vout.Id) |> HandleResponse
            server.Channel.Agent.ConnectModule(string vin.Id, 0, string server.Switch.Id) |> HandleResponse
            // 5. Setup CL
            for m in vin.NextModules do
                let cl = m :?> Classifier
                let gate = vin.NextModules.IndexOf(m)
                server.Channel.Agent.CreateModule("E2Classifier", string cl.Id) |> HandleResponse
                server.Channel.Agent.ConnectModule(string vin.Id, gate, string cl.Id) |> HandleResponse
                // TODO: configure CL
                // 6. Setup LB
                for m in cl.NextModules do
                    let lb = m :?> LoadBalancer
                    let gate = cl.NextModules.IndexOf(m)
                    server.Channel.Agent.CreateModule("E2LB", string lb.Id) |> HandleResponse
                    server.Channel.Agent.ConnectModule(string cl.Id, gate, string lb.Id) |> HandleResponse
        // TODO: configure LB
        // Run vNF
        let idleNF = server.NF |> Seq.filter (fun v -> not v.IsPlaced)
        // Run idleNF on server
        for vnf in idleNF do
            let core = [| server.NF.IndexOf(vnf) + 1 |]
            let vp = 
                server.VPort
                |> Seq.filter (fun vp -> vp.NF = vnf)
                |> Seq.head
            server.Channel.Agent.LaunchNF(core, vnf.Parent.Type, string vp.Id, string vnf.Id) |> HandleResponse
            vnf.IsPlaced <- true
    
    member this.ApplySwitch() = 
        let HandleResponse code =
            match code with
            | 0 -> ()
            | x -> failwith (sprintf "Error code: %d" x)

        this.ToR.Channel.CleanTables ()

        // Setup L2 ACL
        this.ToR.L2 |> Seq.iteri (fun i entry ->
            let dmac = entry.Key
            let dmacFormat = BitConverter.ToString(dmac.GetAddressBytes()).Replace('-', ':')
            let server = entry.Value
            let port = this.ToR.Port.[server]
            let index = i + 1
            printfn "%A -> %d" dmac port
            this.ToR.Channel.Agent.ACLExpressionsAddRow(index, "DstMac", "ff:ff:ff:ff:ff:ff", dmacFormat) |> HandleResponse
            this.ToR.Channel.Agent.ACLActionsAddRow(index, "Redirect", string port) |> HandleResponse
            this.ToR.Channel.Agent.ACLRulesAddRow(index, index, index, "Ingress", "Enabled", 1) |> HandleResponse
        )
    
    member this.Apply() = 
        this.Servers |> Seq.iter this.ApplyServer
        this.ApplySwitch()
