namespace E2

open System
open System.Collections.Generic
open System.Net
open System.Net.NetworkInformation
open System.Threading
open log4net

type Orchestrator(conf : string) =    
    let log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)
     
    let HandleResponse(resp : Response) = 
        match resp.code with
        | 0 -> ()
        //| -1 -> () // Not Implemented
        | _ -> failwith (sprintf "Error code: %d, Message: %s" resp.code resp.msg)
    
    let HandleSwitchResponse code = 
        match code with
        | 0 -> ()
        | x -> failwith (sprintf "Error code: %d" x)
    
    let GetDMACString(dmac : PhysicalAddress) = BitConverter.ToString(dmac.GetAddressBytes()).Replace('-', ':')
    
    let LBArg (id : int) (lb : LoadBalancer) = 
        let arg = new CookComputing.XmlRpc.XmlRpcStruct()
        
        let dmacs = 
            lb.ReplicaDMAC
            |> Seq.map (fun dmac -> GetDMACString dmac)
            |> Seq.toArray
        arg.Add("is_end", lb.IsLastHop)
        arg.Add("server_id", id)
        arg.Add("dmacs", dmacs)
        arg
    
    let CLArg(cl : Classifier) = 
        cl.Filters
        |> Seq.map (fun c -> 1)
        |> Seq.toArray
    
    let SWArg(sw : Switch) = 
        let arg = new CookComputing.XmlRpc.XmlRpcStruct()
        let table = new CookComputing.XmlRpc.XmlRpcStruct()
        sw.DMAC
        |> Seq.mapi (fun i dmac -> (i + 2, GetDMACString dmac))
        |> Seq.iter (fun (port, dmac) -> table.Add(dmac, port))
        arg.Add("insert", false)
        arg.Add("table", table)
        arg

    let state = Parser.Parse conf
    let blueprint = Policy()
    do blueprint.LoadPolicyState state
    let plan = Planner.InitialPlan(blueprint)
    
    member val Servers = List<Server>()
    member val ToR = ToRSwitch([ 17; 18; 19; 20 ], IPAddress.Parse("127.0.0.1"))
    member this.Policy = blueprint :> IPolicy
    member this.Plan = plan
    member val CurrentPlacement = new Dictionary<IPlanVertex, Server>() with get, set
    
    member this.InitServer() = 
        log.Info("Init servers.")
        let servers = 
            [ ("c34.millennium.berkeley.edu", 15, 46); 
              ("c35.millennium.berkeley.edu", 15, 48);
              ("c38.millennium.berkeley.edu", 31, 42);
              ("c41.millennium.berkeley.edu", 19, 44) ]
        //let servers = [ ("127.0.0.1", 16, 1) ]
        for (addr, cores, port) in servers do
            let server = Server(cores, addr)
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
        log.Info("Init internal states.")

        this.CurrentPlacement <- Placement.Place this.Plan this.Servers false

        for kv in this.CurrentPlacement do
            let server = kv.Value
            let vertex = kv.Key
            let dmac = PhysicalAddress.Parse("06" + vertex.Id.ToString("X10"))
            log.DebugFormat("Allocate {0} on Server {1}.", vertex.Parent.Name, server.Id)

            // Add vNF
            let core = server.Cores.Dequeue()
            server.NF.Add(vertex, core)
            
            // Add vPort
            let vp = VPortStruct(vertex)
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
            let nextHopEdges = vertex.Parent |> this.Policy.OutEdges

            log.DebugFormat("{0} nexthops.", (nextHopEdges |> Seq.length))

            if Seq.isEmpty nextHopEdges then 
                let lb = LoadBalancer(true, None)
                server.LB.Add(lb)
                lb.NextModules.Add(server.Switch)
                cl.Filters.Add("true")
                cl.NextModules.Add(lb)
            else 
                for e in nextHopEdges do
                    // Config LB for all vNFs
                    let nextHopPolicyVertex = e.Target
                    let nextHopVertices = this.Plan.FindPlanVertices nextHopPolicyVertex
                    // Make a new LB for each next hop NF
                    let lb = LoadBalancer(false, Some nextHopPolicyVertex)
                    server.LB.Add(lb)
                    lb.NextModules.Add(server.Switch)
                    cl.Filters.Add(e.Tag.Filter)
                    cl.NextModules.Add(lb)
                    for v in nextHopVertices do
                        let dmac = PhysicalAddress.Parse("06" + v.Id.ToString("X10"))
                        lb.ReplicaDMAC.Add(dmac)

            // If first-hop NF, set up first-hop LB
            if vertex |> this.Plan.InEdges |> Seq.isEmpty then
                let AddFirstHopLBEntry (s : Server) =
                    s.FirstHopLB.ReplicaDMAC.Add(dmac)
                    s.FirstHopLB.Target <- Some vertex.Parent
                this.Servers |> Seq.iter AddFirstHopLBEntry

            // Finally set up L2 table 
            this.ToR.L2.Add(dmac, server)
    
    // TODO: Will refactor using BFS. The code should be much more succinct.
    member this.ApplyServer(server : Server) =
        // 1. flush SoftNIC modules
        log.InfoFormat("Flush server {0}'s config.", server.Address)
        log.Debug("Reset SoftNIC.")
        server.Channel.Agent.ResetSoftNIC() |> HandleResponse
        // 1.5 Launch SN
        log.Debug("Launch SoftNIC.")
        server.Channel.Agent.LaunchSoftNIC([| 0 |]) |> HandleResponse

        server.Channel.Agent.PauseSoftNIC() |> HandleResponse

        // 2. Setup PPort
        log.Debug("Create PPort.")
        server.Channel.Agent.CreatePPort(string server.PPort.Id) |> HandleResponse
        log.Debug("Create PPort's PortInc and PortOut modules.")
        server.Channel.Agent.CreateModuleEx("PortInc", string server.PPortIn.Id, string server.PPort.Id, true) 
        |> HandleResponse
        server.Channel.Agent.CreateModuleEx("PortOut", string server.PPortOut.Id, string server.PPort.Id, true) 
        |> HandleResponse
        // 3. Setup E2Switch
        log.Debug("Create E2Switch module.")
        server.Channel.Agent.CreateModuleEx("E2Switch", string server.Switch.Id, SWArg server.Switch, true) 
        |> HandleResponse
        log.Debug("Connect PPortPortInc[0] -> E2Switch.")
        server.Channel.Agent.ConnectModule(string server.PPortIn.Id, 0, string server.Switch.Id) |> HandleResponse
        log.Debug("Connect E2Switch[0] -> PPortPortOut.")
        server.Channel.Agent.ConnectModule(string server.Switch.Id, 0, string server.PPortOut.Id) |> HandleResponse
        // Setup first-hop LB
        log.Debug("Create the first-hop LB module.")
        server.Channel.Agent.CreateModuleEx
            ("E2LoadBalancer", string server.FirstHopLB.Id, LBArg server.Id server.FirstHopLB, true) |> HandleResponse
        log.Debug("Connect E2Switch[1] -> FirstHopLB.")
        server.Channel.Agent.ConnectModule(string server.Switch.Id, 1, string server.FirstHopLB.Id) |> HandleResponse
        log.Debug("Connect FirstHopLB[0] -> E2Switch.")
        server.Channel.Agent.ConnectModule(string server.FirstHopLB.Id, 0, string server.Switch.Id) |> HandleResponse
        // 4. Setup VPorts
        for vout in server.VPortOut do
            let vp = vout.NextModules.[0]
            let vin = vp.NextModules.[0]
            log.DebugFormat("Create VPort {0}.", vp.Id)
            server.Channel.Agent.CreateVPort(string vp.Id) |> HandleResponse
            log.DebugFormat("Create VPort {0}'s PortInc {1} and PortOut {2} modules.", vp.Id, vin.Id, vout.Id)
            server.Channel.Agent.CreateModuleEx("PortInc", string vin.Id, string vp.Id, true) |> HandleResponse
            server.Channel.Agent.CreateModuleEx("PortOut", string vout.Id, string vp.Id, true) |> HandleResponse
            let gate = server.Switch.NextModules.IndexOf(vout)
            log.DebugFormat("Connect E2Switch[{0}] -> PortOut {1}.", gate, vout.Id)
            server.Channel.Agent.ConnectModule(string server.Switch.Id, gate, string vout.Id) |> HandleResponse
            //printfn "Connect PortInc[0] -> E2Switch." 
            //server.Channel.Agent.ConnectModule(string vin.Id, 0, string server.Switch.Id) |> HandleResponse
            // 5. Setup CL
            for m in vin.NextModules do
                let cl = m :?> Classifier
                let gate = vin.NextModules.IndexOf(m)
                log.DebugFormat("Create E2Classifier {0} module.", cl.Id)
                server.Channel.Agent.CreateModuleEx("E2Classifier", string cl.Id, CLArg cl, true) |> HandleResponse
                log.DebugFormat("Connect PortInc[{0}] -> E2Classifier {1}.", gate, cl.Id)
                server.Channel.Agent.ConnectModule(string vin.Id, gate, string cl.Id) |> HandleResponse
                // 6. Setup LB
                for m in cl.NextModules do
                    let lb = m :?> LoadBalancer
                    let gate = cl.NextModules.IndexOf(m)
                    log.DebugFormat("Create E2LB {0} module.", lb.Id)
                    server.Channel.Agent.CreateModuleEx("E2LoadBalancer", string lb.Id, LBArg server.Id lb, true) 
                    |> HandleResponse
                    log.DebugFormat("Connect E2Classifier[{0}] -> E2LB {1}.", gate, lb.Id)
                    server.Channel.Agent.ConnectModule(string cl.Id, gate, string lb.Id) |> HandleResponse
                    log.DebugFormat("Connect E2LB[0] -> E2Switch.")
                    server.Channel.Agent.ConnectModule(string lb.Id, 0, string server.Switch.Id) |> HandleResponse
        
        // Run vNF 
        let idleNF = server.NF |> Seq.filter (fun kv -> kv.Key.State = Assigned)

        // Run idleNF on server
        for kv in idleNF do
            let vertex = kv.Key
            let core = [| kv.Value |]
            
            let vp = 
                server.VPort
                |> Seq.filter (fun vp -> vp.NF = vertex)
                |> Seq.exactlyOne
            log.DebugFormat("Launch NF {0} of {1} on vport {2} and core {3}.", vertex.Id, vertex.Parent.Type, vp.Id, core.[0])
            server.Channel.Agent.LaunchNF(core, vertex.Parent.Type, string vp.Id, string vertex.Id) |> HandleResponse
            vertex.State <- Placed

        server.Channel.Agent.ResumeSoftNIC() |> HandleResponse
    
    member this.ApplySwitch() = 
        log.Info("Flush ToR switch config.")
        log.Debug("Clean up previous ACL tables.")
        this.ToR.Channel.CleanTables()

        // Setup First-hop ACL
        log.Debug("Set up ACL tables for first-hop vNF.")
        let firstHopVNF = 
            this.Plan.Vertices |> Seq.filter (fun vnf -> 
                                      vnf
                                      |> this.Plan.InEdges
                                      |> Seq.isEmpty)
        
        let findServer vertex = 
            this.Servers
            |> Seq.filter (fun s -> s.NF.ContainsKey(vertex))
            |> Seq.exactlyOne
        
        let firstHopServers = firstHopVNF |> Seq.map findServer
        let firstHopPorts = firstHopServers |> Seq.map (fun s -> this.ToR.Port.[s]) |> Seq.distinct

        let numPorts = firstHopPorts |> Seq.length
        let numHashes = 4
        
        let factor = numHashes / numPorts
        
        let ingressPorts = 
            this.ToR.IngressPort
            |> Seq.map string
            |> String.concat ","
        
        let RedirectIngress index l4port port = 
            this.ToR.Channel.Agent.ACLExpressionsAddRow(index, "InPorts", "", ingressPorts) |> HandleSwitchResponse
            this.ToR.Channel.Agent.ACLExpressionsAddRow(index, "L4SrcPort", "65535", string l4port) |> HandleSwitchResponse
            this.ToR.Channel.Agent.ACLActionsAddRow(index, "Redirect", string port) |> HandleSwitchResponse
            this.ToR.Channel.Agent.ACLRulesAddRow(index, index, index, "Ingress", "Enabled", 2) |> HandleSwitchResponse
        
        // FIXME: this code look ugly
        firstHopPorts |> Seq.iteri (fun i port -> 
                             let low = i * factor
                             let high = (i + 1) * factor - 1
                             log.DebugFormat("L4 Src Port {0}-{1} -> Switch Port {2}...", low, high, port)
                             for j = low to high do
                                 let index = j + 1000
                                 RedirectIngress index j port
                             if i = numPorts - 1 then 
                                 if high + 1 < numHashes then 
                                     log.DebugFormat("L4 Src Port {0}-{1} -> Switch Port {2}...", (high + 1), (numHashes - 1), port)
                                     for j = high + 1 to numHashes - 1 do
                                         let index = j + 1000
                                         RedirectIngress index j port)
        // Setup L2 ACL
        log.Debug "Set up ACL tables for other vNF..."
        this.ToR.L2 
        |> Seq.iteri (fun i (dmac, server) -> 
               let dmacFormat = BitConverter.ToString(dmac.GetAddressBytes()).Replace('-', ':')
               let port = this.ToR.Port.[server]
               let index = i + 1
               log.DebugFormat("Destination MAC {0} -> Switch Port {1}...", dmacFormat, port)
               this.ToR.Channel.Agent.ACLExpressionsAddRow(index, "DstMac", "ff:ff:ff:ff:ff:ff", dmacFormat) 
               |> HandleSwitchResponse
               this.ToR.Channel.Agent.ACLActionsAddRow(index, "Redirect", string port) |> HandleSwitchResponse
               this.ToR.Channel.Agent.ACLRulesAddRow(index, index, index, "Ingress", "Enabled", 3) |> HandleSwitchResponse)

        this.ToR.Channel.Agent.ACLExpressionsAddRow(2000, "DstMac", "ff:00:00:00:00:00", "06:00:00:00:00:00") |> HandleSwitchResponse
        this.ToR.Channel.Agent.ACLActionsAddRow(2000, "Drop", "") |> HandleSwitchResponse
        this.ToR.Channel.Agent.ACLRulesAddRow(2000, 2000, 2000, "Ingress", "Enabled", 1) |> HandleSwitchResponse


    member this.Apply() = 
        this.Servers |> Seq.iter this.ApplyServer
        this.ApplySwitch()
    
    member this.Loop() = 
        let EnsureTrue b = if not b then failwith "True value expected!"
        
        let UpdateEdges (plan : IPlan) = 
            let nfstats (s : Server) = 
                let attachNF (stat : PortStat) = 
                    let vport = s.VPort |> Seq.filter (fun p -> string p.Id = stat.port) |> Seq.exactlyOne
                    let nf = vport.NF
                    (nf, stat.out_mpps * 1e6)
                
                let response = s.Channel.Agent.QueryVPortStats()
                assert (response.code = 0)
                let stats = response.result
                log.DebugFormat("Poll VPort stats on Server {0}. {1} VPorts in total.", s.Id, (Seq.length stats))
                stats |> Seq.map attachNF
            
            let update (nf : IPlanVertex) (totalpps : float) = 
                let prev = plan.InEdges nf
                let n = Seq.length prev
                nf.AggregatePacketsPerSeconds.Enqueue(totalpps)
                prev |> Seq.iter (fun e -> e.Tag.PacketsPerSecond <- totalpps / float n)
            
            log.Debug "Collect stats."
            let stats = 
                this.Servers
                |> Seq.map nfstats
                |> Seq.concat
            log.Debug "Update edge weights."
            stats |> Seq.iter (fun (nf, totalpps) -> update nf totalpps)
        
        let SetLoadBalancer (connect : bool) (src : IPlanVertex) (dst : IPlanVertex) = 
            let server = this.CurrentPlacement.[src]
            
            let lb = 
                server.LB
                |> Seq.filter (fun lb -> lb.Target = Some dst.Parent)
                |> Seq.exactlyOne
            
            let dmac = PhysicalAddress.Parse("06" + dst.Id.ToString("X10"))
            if connect then lb.ReplicaDMAC.Add(dmac)
            else lb.ReplicaDMAC.Remove(dmac) |> EnsureTrue
            // !!
            server.Channel.Agent.QueryModule(string lb.Id, LBArg server.Id lb) |> HandleResponse
        
        let ExecuteScaleUp(replica : KeyValuePair<IPlanVertex, Server>) = 
            // For each replica
            //    run it on a server
            //    set up vport, portinc and portout
            //    set up o entry on e2switch
            //    connect e2switch -> portout
            //    set up classfier
            //    connect portinc -> classifer
            //    set up lbs
            //        connect cl -> lb
            //        connect lb -> e2switch
            //    set up previous hop lbs / if first hop, set up ACLs
            let vertex = replica.Key
            let server = replica.Value

            assert (vertex.State = Assigned)
            assert (server.Cores.Count > 0)

            let port = server.Cores.Dequeue()
            server.NF.Add(vertex, port)
            let vport = VPortStruct(vertex)
            let vportInc = VPortIn()
            let vportOut = VPortOut()
            vportOut.NextModules.Add(vport)
            vport.NextModules.Add(vportInc)
            server.VPort.Add(vport)
            server.VPortIn.Add(vportInc)
            server.VPortOut.Add(vportOut)
            // !!
            server.Channel.Agent.CreateVPort(string vport.Id) |> HandleResponse
            server.Channel.Agent.CreateModuleEx("PortInc", string vportInc.Id, string vport.Id, true) |> HandleResponse
            server.Channel.Agent.CreateModuleEx("PortOut", string vportOut.Id, string vport.Id, true) |> HandleResponse
            server.Channel.Agent.LaunchNF
                ([| port |], vertex.Parent.Type, string vport.Id, string vertex.Id) 
            |> HandleResponse
            vertex.State <- Placed

            let dmac = PhysicalAddress.Parse("06" + vertex.Id.ToString("X10"))
            server.Switch.DMAC.Add(dmac)
            server.Switch.NextModules.Add(vportOut)
            // !!
            let gate = server.Switch.NextModules.IndexOf(vportOut)
            server.Channel.Agent.QueryModule(string server.Switch.Id, SWArg server.Switch) |> HandleResponse
            server.Channel.Agent.ConnectModule(string server.Switch.Id, gate, string vportOut.Id) |> HandleResponse
            let cl = Classifier()
            vportInc.NextModules.Add(cl)
            server.CL.Add(cl)
            let nextHopPolicyEdges = vertex.Parent |> this.Policy.OutEdges
            log.DebugFormat("{0} nexthops.", (nextHopPolicyEdges |> Seq.length))
            if Seq.isEmpty nextHopPolicyEdges then 
                let lb = LoadBalancer(true, None)
                server.LB.Add(lb)
                lb.NextModules.Add(server.Switch)
                cl.Filters.Add("true")
                cl.NextModules.Add(lb)
            else 
                for e in nextHopPolicyEdges do
                    let nextHopPolicyVertex = e.Target
                    let nextHopVertices = this.Plan.FindPlanVertices nextHopPolicyVertex
                    // Make a new LB for each next hop NF
                    let lb = LoadBalancer(false, Some nextHopPolicyVertex)
                    server.LB.Add(lb)
                    lb.NextModules.Add(server.Switch)
                    cl.Filters.Add(e.Tag.Filter)
                    cl.NextModules.Add(lb)
                    // Config LB for all vNFs
                    for v in nextHopVertices do
                        let dmac = PhysicalAddress.Parse("06" + v.Id.ToString("X10"))
                        lb.ReplicaDMAC.Add(dmac)
            // !!
            server.Channel.Agent.CreateModuleEx("E2Classifier", string cl.Id, CLArg cl, true) |> HandleResponse
            server.Channel.Agent.ConnectModule(string vportInc.Id, 0, string cl.Id) |> HandleResponse
            for m in cl.NextModules do
                let lb = m :?> LoadBalancer
                let gate = cl.NextModules.IndexOf(m)
                server.Channel.Agent.CreateModuleEx("E2LoadBalancer", string lb.Id, LBArg server.Id lb, true) 
                |> HandleResponse
                server.Channel.Agent.ConnectModule(string cl.Id, gate, string lb.Id) |> HandleResponse
                server.Channel.Agent.ConnectModule(string lb.Id, 0, string server.Switch.Id) |> HandleResponse
            this.ToR.L2.Add(dmac, server)

        let CrossConnect(replica : KeyValuePair<IPlanVertex, Server>) = 
            let vertex = replica.Key
            let server = replica.Value
            let dmac = PhysicalAddress.Parse("06" + vertex.Id.ToString("X10"))

            let prevHopEdges = vertex |> this.Plan.InEdges
            let prevHopVertices = prevHopEdges |> Seq.map (fun e -> e.Source)
            if not (Seq.isEmpty prevHopVertices) then 
                for pred in prevHopVertices do
                    SetLoadBalancer true pred vertex
            else
                let AddFirstHopLBEntry (s : Server) =
                    let lb = s.FirstHopLB
                    lb.ReplicaDMAC.Add(dmac)
                    s.Channel.Agent.QueryModule(string lb.Id, LBArg server.Id lb) |> HandleResponse
                this.Servers |> Seq.iter AddFirstHopLBEntry
                
        
        let ExecuteScaleDown(redundant : KeyValuePair<IPlanVertex, Server>) = 
            let vertex = redundant.Key
            let server = redundant.Value
            assert (vertex.State = Obsolete)
            
            let freeCore = server.NF.[vertex]
            server.NF.Remove(vertex) |> EnsureTrue
            server.Cores.Enqueue(freeCore)
            this.Plan.RemoveVertex vertex |> EnsureTrue
            this.CurrentPlacement.Remove(vertex) |> EnsureTrue
            // !!
            server.Channel.Agent.StopNF(string vertex.Id) |> HandleResponse

            let vport = 
                server.VPort
                |> Seq.filter (fun vp -> vp.NF = vertex)
                |> Seq.exactlyOne
            
            let vportInc = vport.NextModules.[0] :?> VPortIn
            
            let vportOut = 
                server.VPortOut
                |> Seq.filter (fun vpout -> vpout.NextModules.[0] = (vport :> Module))
                |> Seq.exactlyOne
            server.VPort.Remove(vport) |> EnsureTrue
            server.VPortIn.Remove(vportInc) |> EnsureTrue
            server.VPortOut.Remove(vportOut) |> EnsureTrue
            // !!
            server.Channel.Agent.DestroyModule(string vportInc.Id) |> HandleResponse
            server.Channel.Agent.DestroyModule(string vportOut.Id) |> HandleResponse
            server.Channel.Agent.DestroyPort(string vport.Id) |> HandleResponse
            let cl = vportInc.NextModules.[0] :?> Classifier
            server.CL.Remove(cl) |> EnsureTrue
            // !!
            server.Channel.Agent.DestroyModule(string cl.Id) |> HandleResponse
            for m in cl.NextModules do
                let lb = m :?> LoadBalancer
                server.LB.Remove(lb) |> EnsureTrue
                // !!
                server.Channel.Agent.DestroyModule(string lb.Id) |> HandleResponse

            let dmac = PhysicalAddress.Parse("06" + vertex.Id.ToString("X10"))
            // !!
            server.Switch.NextModules 
            |> Seq.iteri (fun gate _ -> server.Channel.Agent.DisconnectModule(string server.Switch.Id, gate) |> HandleResponse)

            server.Switch.DMAC.Remove(dmac) |> EnsureTrue
            server.Switch.NextModules.Remove(vportOut) |> EnsureTrue

            // !!
            server.Channel.Agent.QueryModule(string server.Switch.Id, SWArg server.Switch) |> HandleResponse
            server.Switch.NextModules 
            |> Seq.iteri 
                (fun gate next -> 
                    server.Channel.Agent.ConnectModule(string server.Switch.Id, gate, string next.Id) |> HandleResponse)

            this.ToR.L2.Remove(dmac, server) |> EnsureTrue

        let CrossDisconnect(redundant : KeyValuePair<IPlanVertex, Server>) = 
            let vertex = redundant.Key
            let server = redundant.Value
            let dmac = PhysicalAddress.Parse("06" + vertex.Id.ToString("X10"))

            // Remove other LB entries
            let lastHopEdges = vertex |> this.Plan.InEdges
            let lastHopVertices = lastHopEdges |> Seq.map (fun e -> e.Source)
            if not (Seq.isEmpty lastHopVertices) then 
                for pred in lastHopVertices do
                    SetLoadBalancer false pred vertex
            else
                let RemoveFirstHopLBEntry (s : Server) =
                    let lb = s.FirstHopLB
                    lb.ReplicaDMAC.Remove(dmac) |> EnsureTrue
                    s.Channel.Agent.QueryModule(string lb.Id, LBArg server.Id lb) |> HandleResponse
                this.Servers |> Seq.iter RemoveFirstHopLBEntry
        
        let Dump() = 
            let time = System.DateTime.Now.ToString()

            let portStats = this.Servers |> Seq.map (fun s -> s.Channel.Agent.QueryPPortStats())
                                         |> Seq.map (fun stats -> assert (stats.code = 0); stats.result)
                                         |> Seq.map (fun stats -> stats |> Seq.exactlyOne)
            let aggregateIn = portStats |> Seq.sumBy (fun stat -> stat.inc_mbps)
            let aggregateOut = portStats |> Seq.sumBy (fun stat -> stat.out_mbps)

            let usedCores = this.Servers |> Seq.sumBy (fun s -> s.NF.Count) |> float
            let optimalCores =
                let PolicyVertexLoads pv = 
                    plan.FindPlanVertices pv
                    |> Seq.map (fun v -> v.AggregatePacketsPerSeconds.FindMax())
                    |> Seq.sum
                let replicaNumIdeal (pv : IPolicyVertex) = 
                    PolicyVertexLoads pv * pv.CyclesPerPacket / (2.6e+9 * 0.9) 
                this.Policy.Vertices |> Seq.map replicaNumIdeal |> Seq.sum

            let optimalPlacement =     
                let Servers = 
                    let s = [
                        Server(15, "c34.millennium.berkeley.edu"); 
                        Server(15, "c35.millennium.berkeley.edu");
                        Server(31, "c38.millennium.berkeley.edu");
                        Server(19, "c41.millennium.berkeley.edu")
                    ] 
                    new List<Server> (s)
                Placement.Place this.Plan Servers true
            
            let optimalOut =
                let OutTraffic v =
                    v |> this.Plan.OutEdges 
                      |> Seq.filter (fun e -> optimalPlacement.[e.Target] <> optimalPlacement.[e.Source])
                      |> Seq.sumBy (fun e -> e.Tag.PacketsPerSecond * 1000.0 * 8.0 / 1e6)
                let normal = this.Plan.Vertices |> Seq.map OutTraffic |> Seq.sum
                let last = this.Plan.Vertices |> Seq.filter (fun v -> this.Plan.OutEdges v |> Seq.isEmpty) // last hop vertices
                                              |> Seq.sumBy (fun v -> v.AggregatePacketsPerSeconds.FindMax() * 1000.0 * 8.0 / 1e6)
                normal + last

            let optimalIn =
                let InTraffic v =
                    v |> this.Plan.InEdges 
                      |> Seq.filter (fun e -> optimalPlacement.[e.Target] <> optimalPlacement.[e.Source])
                      |> Seq.sumBy (fun e -> e.Tag.PacketsPerSecond * 1000.0 * 8.0 / 1e6)
                let normal = this.Plan.Vertices |> Seq.map InTraffic |> Seq.sum
                let first = this.Plan.Vertices |> Seq.filter (fun v -> this.Plan.InEdges v |> Seq.isEmpty) // first hop vertices
                                               |> Seq.sumBy (fun v -> v.AggregatePacketsPerSeconds.FindMax() * 1000.0 * 8.0 / 1e6)
                normal + first

            log.WarnFormat("{0} Core: {1}/{2}, In: {3}/{4}, Out: {5}/{6}",
                time, usedCores, optimalCores, aggregateIn, optimalIn, aggregateOut, optimalOut)
            ()

        let rec Loop() = 
            log.Debug "Sanity check for placement."
            assert (this.CurrentPlacement |> Seq.map (fun kv -> kv.Key.State = Placed) |> Seq.reduce (fun a b -> a && b))
            
            log.Debug "Update edges in the graph."
            UpdateEdges this.Plan
            
            log.Debug "Scale the graph."
            Planner.Scale this.Policy this.Plan
            
            // Place replicas
            this.CurrentPlacement <- Placement.Incremental this.Plan this.Servers this.CurrentPlacement

            // Process assigned but not placed replicas
            let replicas = this.CurrentPlacement |> Seq.filter (fun kv -> kv.Key.State = Assigned) |> Seq.toList
            let obsolete = this.CurrentPlacement |> Seq.filter (fun kv -> kv.Key.State = Obsolete) |> Seq.toList

            let scaleup = not (List.isEmpty replicas)
            let scalein = not (List.isEmpty obsolete)

            if scaleup || scalein then 
                this.Servers |> Seq.iter (fun s -> s.Channel.Agent.PauseSoftNIC() |> ignore)
            
            replicas |> List.iter ExecuteScaleUp
            replicas |> List.iter CrossConnect

            obsolete |> List.iter CrossDisconnect
            obsolete |> List.iter ExecuteScaleDown

            if scaleup || scalein then 
                log.Debug "Change detected. Update ToR switch ACL tables."
                this.ApplySwitch()
                this.Servers |> Seq.iter (fun s -> s.Channel.Agent.ResumeSoftNIC() |> ignore)
            else
                log.Debug "No change detected."
            
            log.Debug "Dumping experimental data."
            Dump()

            log.Debug "Wait 1 second for next iteration."
            
            Thread.Sleep(1000)

            Loop()

        log.Info "Enter the loop for dynamic scaling."
        Loop()
