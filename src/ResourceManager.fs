module E2.ResourceManager

open System.Collections.Generic
open Graph

type Host(id: int, cores: float) = 
    member val id = id
    member val total_cores = cores with get, set
    member val used_cores = 0.0 with get, set

type Manager(policy: string) = 
    member val hosts = List<Host>() with get, set
    member val placement = Dictionary<InstanceNF, Host>() with get, set
    member val graph = policy |> Parser.Parse |> Graph.Graph

    member this.HomogeneousHosts(num_hosts: int, num_cores: float) = 
        for i = 0 to num_hosts-1 do 
            this.hosts.Add(new Host(i, num_cores))

    member this.InitiateNF(nf: InstanceNF , host: Host) = 
        this.placement.Add(nf, host)
        // step 1: spawn the nf using the controller channel
        
        // step 2: set up the chain


    member this.InitialPlacement() = 
        let n = this.hosts.Count
        let cores = this.hosts |> Seq.map (fun h -> h.total_cores - h.used_cores) |> Seq.toArray
        this.graph.PlaceBreadthFirstSearch(n, cores)