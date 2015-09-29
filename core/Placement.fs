namespace E2

open System.Net
open System.Linq
open System.Collections.Generic
open System.Net.NetworkInformation
open QuickGraph

type Placement() =     
    static member private PlaceBreadthFirst (plan : IPlan) (servers : IList<Server>) (fake : bool) = 
        let placement = new Dictionary<IPlanVertex, Server>()
        
        let fg = new FlatGraph(plan)
        let g = fg :> E2.IUndirectedGraph<IPlanVertex, IPlanEdgeTag>
        // I like immutable too, but...
        let mutable serverIndex = 0
        let mutable usedCores = 0

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
            if 1 <= servers.[serverIndex].Cores.Count - usedCores then 
                usedCores <- usedCores + 1
            else 
                usedCores <- 0
                serverIndex <- serverIndex + 1
                if serverIndex >= servers.Count then failwith "Not enough servers for placement."

            placement.Add(current, servers.[serverIndex])
            if not fake then current.State <- Assigned

            let edges = g.AdjacentEdges(current)
            for e in edges do
                let v = 
                    if e.Source = current then e.Target
                    else e.Source
                if colors.[v] = GraphColor.White then 
                    colors.[v] <- GraphColor.Gray
                    pending.Enqueue(v)
        placement
    
    static member private PlaceHeuristic (plan : IPlan) (servers : IList<Server>) (fake : bool) = 
        let placement = Placement.PlaceBreadthFirst plan servers fake
        let fg = new FlatGraph(plan)
        let g = fg :> E2.IUndirectedGraph<IPlanVertex, IPlanEdgeTag>
        
        let rec Iteration n = 
            let pairs = 
                seq { 
                    for v1 in g.Vertices do
                        for v2 in g.Vertices do
                            yield v1, v2
                }
            
            let IsInSamePartitions(v1, v2) = (placement.[v1] = placement.[v2])
            
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
                    |> Seq.map (fun e -> e.Tag.PacketsPerSecond)
                    |> Seq.sum
                
                let InternalCost v = 
                    v
                    |> g.AdjacentEdges
                    |> Seq.filter IsInternalEdge
                    |> Seq.map (fun e -> e.Tag.PacketsPerSecond)
                    |> Seq.sum
                
                let reduction_v1 = ExternalCost v1 v2 - InternalCost v1
                let reduction_v2 = ExternalCost v2 v1 - InternalCost v2
                
                let cost = 
                    g.GetEdges v1 v2
                    |> Seq.map (fun e -> e.Tag.PacketsPerSecond)
                    |> Seq.sum
                reduction_v1 + reduction_v2 - 2.0 * cost
            
            let MaxGain (v1, v2) (v1', v2') = 
                let gain1 = SwapGain(v1, v2)
                let gain2 = SwapGain(v1', v2')
                if gain1 >= gain2 then (v1, v2)
                else (v1', v2')
            
            let SwapBestPair ps = 
                if Seq.isEmpty ps then ()
                else 
                    let (v1, v2) = ps |> Seq.reduce MaxGain
                    let temp = placement.[v1]
                    placement.[v1] <- placement.[v2]
                    placement.[v2] <- temp
            
            match n with
            | 0 -> ()
            | _ -> 
                pairs
                |> Seq.filter (IsInSamePartitions >> not)
                |> Seq.filter (fun p -> (SwapGain p) > 0.0)
                |> SwapBestPair
                Iteration(n - 1)
        Iteration 20
        placement
    
    static member Incremental (plan : IPlan) (servers : IList<Server>) (placement : Dictionary<IPlanVertex, Server>) = 
        let UsedCores = 
            let d = Dictionary<Server, int>()
            servers |> Seq.iter (fun s -> d.Add(s, 0))
            d
        let PlaceVertex(v : IPlanVertex) = 
            assert (v.State = Unassigned)
            let Cost s = 
                let e1 = plan.InEdges v |> Seq.filter (fun e -> placement.ContainsKey(e.Source) && placement.[e.Source] <> s)
                let e2 = plan.OutEdges v |> Seq.filter (fun e -> placement.ContainsKey(e.Target) && placement.[e.Target] <> s)
                let edges = e1.Union(e2)
                edges
                |> Seq.map (fun e -> e.Tag.PacketsPerSecond)
                |> Seq.sum
            
            let candidates = servers |> Seq.filter (fun s -> s.Cores.Count - UsedCores.[s] > 0)
            if Seq.isEmpty candidates then 
                failwith "Not enough servers for placement."
            else 
                let choice = 
                    candidates |> Seq.reduce (fun s1 s2 -> if Cost s1 <= Cost s2 then s1 else s2)
                //printfn "Placed on server %d." choice.Id
                v.State <- Assigned
                placement.Add(v, choice)
                UsedCores.[choice] <- UsedCores.[choice] + 1

        plan.Vertices
        |> Seq.filter (fun v -> (v.State = Unassigned))
        |> Seq.iter PlaceVertex
        placement
    
    static member Place (plan : IPlan) (servers : IList<Server>) (fake : bool) = 
        Placement.PlaceHeuristic plan servers fake
