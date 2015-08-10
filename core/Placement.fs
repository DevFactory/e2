﻿namespace E2

open System.Net
open System.Collections.Generic
open System.Net.NetworkInformation
open QuickGraph

type Module() = 
    member val Id = Identifier.GetId()
    member val NextModules = List<Module>()

type Classifier() = 
    inherit Module()
    member val Filters = List<string>()

type LoadBalancer(isLastHop : bool) = 
    inherit Module()
    member val IsLastHop = isLastHop
    member val ReplicaDMAC = List<PhysicalAddress>()

type Switch() = 
    inherit Module()
    member val DMAC = List<PhysicalAddress>()

type VPortIn(intf : string) = 
    inherit Module()
    member val LocalInterface = intf

type VPortOut(intf : string) = 
    inherit Module()
    member val LocalInterface = intf

type PPortIn() = 
    inherit Module()

type PPortOut() = 
    inherit Module()

type Server(totalCores : int, ip : IPAddress) = 
    member val Id = Identifier.GetId()
    member val TotalCores = totalCores
    member val IPAddress = ip
    member val NF = List<IPlanVertex>()
    member val VPortIn = List<VPortIn>()
    member val VPortOut = List<VPortOut>()
    member val LB = List<LoadBalancer>()
    member val CL = List<Classifier>()
    member val PPortIn = PPortIn()
    member val PPortOut = PPortOut()
    member val Switch = Switch()
    member this.AvailableCores = float (this.TotalCores - this.NF.Count)

type Placement() = 
    
    static member private PlaceRandom (plan : IPlan) (servers : IList<Server>) = 
        let rand = new System.Random()
        let n = servers.Count
        let dict = new Dictionary<IPlanVertex, Server>()
        plan.Vertices |> Seq.iter (fun v -> 
                             let k = rand.Next(n)
                             dict.Add(v, servers.[k]))
        dict :> IDictionary<IPlanVertex, Server>
    
    static member private PlaceBreadthFirst (plan : IPlan) (servers : IList<Server>) = 
        let fg = new FlatGraph(plan)
        let g = fg :> E2.IUndirectedGraph<IPlanVertex, IPlanEdgeTag>
        // I like immutable too, but...
        let mutable serverIndex = 0
        let mutable usedCores = 0.0
        let dict = Dictionary<IPlanVertex, Server>()
        let colors = new Dictionary<IPlanVertex, GraphColor>()
        let pending = new Queue<IPlanVertex>()
        // Initialize
        g.Vertices |> Seq.iter (fun v -> colors.Add(v, GraphColor.White))
        // Enqueue heads
        let components = new Dictionary<IPlanVertex, int>()
        let component_count = fg.ConnectedComponents(components)
        for i = 0 to component_count - 1 do
            let start = 
                components
                |> Seq.filter (fun kv -> kv.Value = i)
                |> Seq.map (fun kv -> kv.Key)
                |> Seq.head
            colors.[start] <- GraphColor.Gray
            pending.Enqueue(start)
        // Main BFS loop
        while pending.Count <> 0 do
            let current = pending.Dequeue()
            colors.[current] <- GraphColor.Black
            // Place current NF
            if serverIndex >= servers.Count then failwith "Not enough servers for placement."
            if current.Parent.UnitCore <= servers.[serverIndex].AvailableCores - usedCores then 
                usedCores <- usedCores + current.Parent.UnitCore
                dict.Add(current, servers.[serverIndex])
            else 
                usedCores <- 0.0
                serverIndex <- serverIndex + 1
            let edges = g.AdjacentEdges(current)
            for e in edges do
                let v = 
                    if e.Source = current then e.Target
                    else e.Source
                if colors.[v] = GraphColor.White then 
                    colors.[v] <- GraphColor.Gray
                    pending.Enqueue(v)
        dict :> IDictionary<IPlanVertex, Server>
    
    static member private PlaceHeuristic (plan : IPlan) (servers : IList<Server>) = 
        let dict = Placement.PlaceBreadthFirst plan servers
        let fg = new FlatGraph(plan)
        let g = fg :> E2.IUndirectedGraph<IPlanVertex, IPlanEdgeTag>
        
        let rec Iteration n = 
            let pairs = 
                seq { 
                    for v1 in g.Vertices do
                        for v2 in g.Vertices do
                            yield v1, v2
                }
            
            let IsInSamePartitions(v1, v2) = (dict.[v1] = dict.[v2])
            
            // Assumption: v1 and v2 are not in the same partition
            let SwapGain(v1, v2) = 
                let IsInternalEdge(e : IEdge<IPlanVertex, IPlanEdgeTag>) = IsInSamePartitions(e.Source, e.Target)
                
                let IsExternalEdge (v1 : IPlanVertex) (v2 : IPlanVertex) (e : IEdge<IPlanVertex, IPlanEdgeTag>) = 
                    let v' = 
                        if e.Source = v1 then e.Target
                        else e.Source
                    not (IsInSamePartitions(v1, v')) && IsInSamePartitions(v2, v')
                
                let ExternalCost v1 v2 = 
                    v1
                    |> g.AdjacentEdges
                    |> Seq.filter (IsExternalEdge v1 v2)
                    |> Seq.map (fun e -> e.Tag.Load)
                    |> Seq.sum
                
                let InternalCost v = 
                    v
                    |> g.AdjacentEdges
                    |> Seq.filter IsInternalEdge
                    |> Seq.map (fun e -> e.Tag.Load)
                    |> Seq.sum
                
                let reduction_v1 = ExternalCost v1 v2 - InternalCost v1
                let reduction_v2 = ExternalCost v2 v1 - InternalCost v2
                
                let cost = 
                    g.GetEdges v1 v2
                    |> Seq.map (fun e -> e.Tag.Load)
                    |> Seq.sum
                reduction_v1 + reduction_v2 - 2.0 * cost
            
            let MaxGain (v1, v2) (v1', v2') = 
                let gain1 = SwapGain(v1, v2)
                let gain2 = SwapGain(v1', v2')
                if gain1 <= gain2 then (v1, v2)
                else (v1', v2')
            
            let SwapBestPair ps = 
                if Seq.isEmpty ps then ()
                else 
                    let (v1, v2) = ps |> Seq.reduce MaxGain
                    let temp = dict.[v1]
                    dict.[v1] <- dict.[v2]
                    dict.[v2] <- temp
            
            match n with
            | 0 -> ()
            | _ -> 
                pairs
                |> Seq.filter (IsInSamePartitions >> not)
                |> Seq.filter (fun p -> (SwapGain p) > 0.0)
                |> SwapBestPair
                Iteration(n - 1)
        Iteration 10
        dict
    
    static member Place (plan : IPlan) (servers : IList<Server>) = Placement.PlaceHeuristic plan servers
