namespace E2

open System
open System.Collections.Generic
open System.Net
open System.Net.NetworkInformation

type Orchestrator(conf : string) = 
    let state = Parser.Parse conf
    let blueprint = Policy()
    do blueprint.LoadPolicyState state
    let plan = Planner.InitialPlan(blueprint)

    member val Servers = List<Server>()
    member val ToR = ToRSwitch([17;18;19;20], IPAddress.Parse("127.0.0.1"))
    member this.Policy = blueprint :> IPolicy
    member this.Plan = plan
    
    member this.InitServer() = 
        printfn "Init servers..."
        let servers = [("c34.millennium.berkeley.edu", 16, 46);
                       ("c35.millennium.berkeley.edu", 16, 48);
                       ("c38.millennium.berkeley.edu", 16, 42);
                       ("c41.millennium.berkeley.edu", 16, 44)]
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
        printfn "Init internal states..."
        //Planner.Scale this.Policy this.Plan

        printfn "Set up LB for first-hop vNFs..."
        // FIXME: Assume only one first-hop NF
        
        let firstHopVNF = 
            this.Plan.Vertices |> Seq.filter (fun vnf -> 
                                      vnf
                                      |> this.Plan.InEdges
                                      |> Seq.isEmpty)
        printfn "First-hop vNF: %A" firstHopVNF

        for server in this.Servers do
            let dmacs = firstHopVNF |> Seq.map (fun vnf -> PhysicalAddress.Parse("06" + vnf.Id.ToString("X10")))
            server.FirstHopLB.ReplicaDMAC.AddRange(dmacs)

        let scheme = Placement.Place this.Plan this.Servers
        for entry in scheme do
            let server = entry.Value
            let vnf = entry.Key
            let dmac = PhysicalAddress.Parse("06" + vnf.Id.ToString("X10"))
            printfn "Allocate %s on Server %d." vnf.Parent.Name server.Id

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
            let nextHopEdges = vnf.Parent |> this.Policy.OutEdges
            printfn "%d nexthops." (nextHopEdges |> Seq.length)
            if Seq.isEmpty nextHopEdges then
                let lb = LoadBalancer(true)
                lb.NextModules.Add(server.Switch)
                cl.Filters.Add("true")
                cl.NextModules.Add(lb)
            else 
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
    
    // TODO: Will refactor using BFS. The code should be much more succinct.
    member this.ApplyServer(server : Server) = 
        let HandleResponse (resp : Response) =
            match resp.code with
            | 0 -> ()
            //| -1 -> () // Not Implemented
            | _ -> failwith (sprintf "Error code: %d, Message: %s" resp.code resp.msg)

        let GetDMACString (dmac : PhysicalAddress) =
            BitConverter.ToString(dmac.GetAddressBytes()).Replace('-', ':')

        let LBArg (id : int) (lb : LoadBalancer) =
            let arg = new CookComputing.XmlRpc.XmlRpcStruct()
            let dmacs = lb.ReplicaDMAC |> Seq.map (fun dmac -> GetDMACString dmac) |> Seq.toArray
            arg.Add("is_end", lb.IsLastHop)
            arg.Add("server_id", id)
            arg.Add("dmacs", dmacs)
            arg
        
        let CLArg (cl : Classifier) = 
            cl.Filters |> Seq.map (fun c -> 1) |> Seq.toArray

        let SWArg (sw : Switch) = 
            let arg = new CookComputing.XmlRpc.XmlRpcStruct()
            let table = new CookComputing.XmlRpc.XmlRpcStruct()
            sw.DMAC |> Seq.mapi (fun i dmac -> (i + 2, GetDMACString dmac))
                    |> Seq.iter (fun (port, dmac) -> table.Add(dmac, port))
            arg.Add("insert", false)
            arg.Add("table", table)
            arg

        // 1. flush SoftNIC modules
        printfn "On Server %A:" server.Address
        printfn "Reset SoftNIC."
        server.Channel.Agent.ResetSoftNIC() |> HandleResponse

        // 1.5 Launch SN
        printfn "Launch SoftNIC."
        server.Channel.Agent.LaunchSoftNIC([| 0 |]) |> HandleResponse
        
        // 2. Setup PPort
        printfn "Create PPort."
        server.Channel.Agent.CreatePPort(string server.PPort.Id) |> HandleResponse

        printfn "Create PPort's PortInc and PortOut modules."
        server.Channel.Agent.CreateModuleEx("PortInc", string server.PPortIn.Id, string server.PPort.Id, true) |> HandleResponse
        server.Channel.Agent.CreateModuleEx("PortOut", string server.PPortOut.Id, string server.PPort.Id, true) |> HandleResponse
        
        // 3. Setup E2Switch
        printfn "Create E2Switch module."
        server.Channel.Agent.CreateModuleEx("E2Switch", string server.Switch.Id, SWArg server.Switch, true) |> HandleResponse

        printfn "Connect PPortPortInc[0] -> E2Switch."
        server.Channel.Agent.ConnectModule(string server.PPortIn.Id, 0, string server.Switch.Id) |> HandleResponse

        printfn "Connect E2Switch[0] -> PPortPortOut."
        server.Channel.Agent.ConnectModule(string server.Switch.Id, 0, string server.PPortOut.Id) |> HandleResponse

        // Setup first-hop LB
        printfn "Create the first-hop LB module."
        server.Channel.Agent.CreateModuleEx("E2LoadBalancer", string server.FirstHopLB.Id, LBArg server.Id server.FirstHopLB, true) |> HandleResponse

        printfn "Connect E2Switch[1] -> FirstHopLB."
        server.Channel.Agent.ConnectModule(string server.Switch.Id, 1, string server.FirstHopLB.Id) |> HandleResponse

        printfn "Connect FirstHopLB[0] -> E2Switch."
        server.Channel.Agent.ConnectModule(string server.FirstHopLB.Id, 0, string server.Switch.Id) |> HandleResponse

        // 4. Setup VPorts
        for vout in server.VPortOut do
            let vp = vout.NextModules.[0]
            let vin = vp.NextModules.[0]
            printfn "Create VPort %d." vp.Id
            server.Channel.Agent.CreateVPort(string vp.Id) |> HandleResponse

            printfn "Create VPort %d's PortInc %d and PortOut %d modules." vp.Id vin.Id vout.Id
            server.Channel.Agent.CreateModuleEx("PortInc", string vin.Id, string vp.Id, true) |> HandleResponse
            server.Channel.Agent.CreateModuleEx("PortOut", string vout.Id, string vp.Id, true) |> HandleResponse
            
            let gate = server.Switch.NextModules.IndexOf(vout) + 2
            printfn "Connect E2Switch[%d] -> PortOut %d." gate vout.Id
            server.Channel.Agent.ConnectModule(string server.Switch.Id, gate, string vout.Id) |> HandleResponse

            //printfn "Connect PortInc[0] -> E2Switch." 
            //server.Channel.Agent.ConnectModule(string vin.Id, 0, string server.Switch.Id) |> HandleResponse
            
            // 5. Setup CL
            for m in vin.NextModules do
                let cl = m :?> Classifier
                let gate = vin.NextModules.IndexOf(m)
                printfn "Create E2Classifier %d module." cl.Id
                server.Channel.Agent.CreateModuleEx("E2Classifier", string cl.Id, CLArg cl, true) |> HandleResponse

                printfn "Connect PortInc[%d] -> E2Classifier %d." gate cl.Id
                server.Channel.Agent.ConnectModule(string vin.Id, gate, string cl.Id) |> HandleResponse

                // 6. Setup LB
                for m in cl.NextModules do
                    let lb = m :?> LoadBalancer
                    let gate = cl.NextModules.IndexOf(m)
                    printfn "Create E2LB %d module." lb.Id
                    server.Channel.Agent.CreateModuleEx("E2LoadBalancer", string lb.Id, LBArg server.Id lb, true) |> HandleResponse
                    printfn "Connect E2Classifier[%d] -> E2LB %d." gate lb.Id
                    server.Channel.Agent.ConnectModule(string cl.Id, gate, string lb.Id) |> HandleResponse
                    printfn "Connect E2LB[0] -> E2Switch." 
                    server.Channel.Agent.ConnectModule(string lb.Id, 0, string server.Switch.Id) |> HandleResponse
        
        // Run vNF 
        let idleNF = server.NF |> Seq.filter (fun v -> v.State = New)
        // Run idleNF on server
        for vnf in idleNF do
            let core = [| server.NF.IndexOf(vnf) + 1 |]
            let vp = 
                server.VPort
                |> Seq.filter (fun vp -> vp.NF = vnf)
                |> Seq.exactlyOne

            printfn "Launch NF %d of %s on vport %d and core %d." vnf.Id vnf.Parent.Type vp.Id core.[0]
            server.Channel.Agent.LaunchNF(core, vnf.Parent.Type, string vp.Id, string vnf.Id) |> HandleResponse
            vnf.State <- Placed
    
    member this.ApplySwitch() = 
        let HandleResponse code =
            match code with
            | 0 -> ()
            | x -> failwith (sprintf "Error code: %d" x)

        printfn "Clean up previous ACL tables..."
        this.ToR.Channel.CleanTables ()

        // Setup First-hop ACL
        printfn "Set up ACL tables for first-hop vNF..."
        let firstHopVNF = 
            this.Plan.Vertices |> Seq.filter (fun vnf -> 
                                      vnf
                                      |> this.Plan.InEdges
                                      |> Seq.isEmpty)

        let findServer vnf = 
            this.Servers |> Seq.filter (fun s -> s.NF.Contains(vnf)) |> Seq.exactlyOne

        let firstHopServers = firstHopVNF |> Seq.map findServer
        let firstHopPorts = firstHopServers |> Seq.map (fun s -> this.ToR.Port.[s])
        let numPorts = firstHopPorts |> Seq.length
        let factor = 100 / numPorts
        let ingressPorts = this.ToR.IngressPort |> Seq.map string |> String.concat ","

        let RedirectIngress index l4port port =
            this.ToR.Channel.Agent.ACLExpressionsAddRow(index, "InPorts", "", ingressPorts) |> HandleResponse
            this.ToR.Channel.Agent.ACLExpressionsAddRow(index, "L4SrcPort", "65535", string l4port) |> HandleResponse
            this.ToR.Channel.Agent.ACLActionsAddRow(index, "Redirect", string port) |> HandleResponse
            this.ToR.Channel.Agent.ACLRulesAddRow(index, index, index, "Ingress", "Enabled", 2) |> HandleResponse

        // FIXME: this code look ugly
        firstHopPorts |> Seq.iteri (fun i port ->
            let low = i * factor + 1
            let high = (i + 1) * factor

            printfn "L4 Src Port %d-%d -> Switch Port %d..." low high port
            for j = low to high do
                let index = j + 1000
                RedirectIngress index j port

            if i = numPorts - 1 then
                if high + 1 <= 100 then printfn "L4 Src Port %d-%d -> Switch Port %d..." (high + 1) 100 port
                for j = high + 1 to 100 do
                    let index = j + 1000
                    RedirectIngress index j port
        )

        // Setup L2 ACL
        printfn "Set up ACL tables for other vNF..."
        this.ToR.L2 |> Seq.iteri (fun i entry ->
            let dmac = entry.Key
            let dmacFormat = BitConverter.ToString(dmac.GetAddressBytes()).Replace('-', ':')
            let server = entry.Value
            let port = this.ToR.Port.[server]
            let index = i + 1
            printfn "Destination MAC %s -> Switch Port %d..." dmacFormat port
            this.ToR.Channel.Agent.ACLExpressionsAddRow(index, "DstMac", "ff:ff:ff:ff:ff:ff", dmacFormat) |> HandleResponse
            this.ToR.Channel.Agent.ACLActionsAddRow(index, "Redirect", string port) |> HandleResponse
            this.ToR.Channel.Agent.ACLRulesAddRow(index, index, index, "Ingress", "Enabled", 1) |> HandleResponse
        )
    
    member this.Apply() = 
        this.Servers |> Seq.iter this.ApplyServer
        this.ApplySwitch()

    member this.DetectLoop() = 
        let overloadedInstances () =
            let findInstance (portname) (s : Server) =
                let vport = s.VPort |> Seq.filter (fun vport -> string vport.Id = portname) |> Seq.exactlyOne
                vport.NF

            let aux (s : Server) = 
                let response = s.Channel.Agent.QueryVPortStats()
                assert (response.code = 0)
                let stats = response.result
                stats |> Seq.filter (fun stat -> stat.out_qlen > 512) 
                      |> Seq.map (fun stat -> findInstance (stat.port) s)

            this.Servers |> Seq.map aux |> Seq.concat
        ()
        //let ScaleUp (vnf : IPlanVertex) = 
        //    let replica = Planner.ScaleUpPlanVertex vnf this.Policy this.Plan


