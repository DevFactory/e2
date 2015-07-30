module E2.ResourceManager

open System
open System.Collections.Generic
open Graph

//type Host(id: int, cores: float) = 
//    member val id = id
//    member val total_cores = cores with get, set
//    member val used_cores = 0.0 with get, set
//
//type Manager(policy: string) = 
//    member val hosts = List<Host>() with get, set
//    member val graph = policy |> Parser.Parse |> Graph.Graph
//
//    member val placement = Dictionary<InstanceNF, Host>() with get, set
//    
//    member this.AddHomogeneousHosts(num_hosts: int, num_cores: float) = 
//        for i = 0 to num_hosts-1 do 
//            this.hosts.Add(new Host(i, num_cores))
//
//    member this.PlaceNF(nf_id: Guid, host_index: int) = 
//        let host = this.hosts.[host_index]
//        let instance = this.graph.GetInstance(nf_id)
//        
//        host.used_cores <- host.used_cores + instance.parent.core
//        this.placement.Add(instance, host)
//
//        // TODO: Actual Placement
//
//    member this.InitialPlacement() = 
//        let n = this.hosts.Count
//        let cores = this.hosts |> Seq.map (fun h -> h.total_cores - h.used_cores) |> Seq.toArray
//        let mapping = this.graph.PlaceBreadthFirstSearch(n, cores)
//        mapping |> Seq.iter (fun (nf, host) -> this.PlaceNF(nf, host))