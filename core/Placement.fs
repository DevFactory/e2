namespace E2

open System.Collections.Generic
open QuickGraph

type Placement() = 
    member private this.PlaceRandom (plan: IPlan) (servers: IList<IServer>) = 
        let rand = new System.Random()
        let n = servers.Count
        let dict = new Dictionary<IPlanVertex, IServer>()
        plan.Vertices |> Seq.iter (fun v -> let k = rand.Next(n) in dict.Add(v, servers.[k]))
        dict :> IDictionary<IPlanVertex, IServer>

    member private this.PlaceBreadthFirst (plan: IPlan) (servers: IList<IServer>) = 
        let fg = new FlatGraph(plan)
        let g = fg :> E2.IUndirectedGraph<IPlanVertex, IPlanEdgeTag>
                
        // I like immutable too, but...
        let mutable serverIndex = 0
        let mutable usedCores = 0.0
        let dict = Dictionary<IPlanVertex, IServer>()
        
        let colors = new Dictionary<IPlanVertex, GraphColor>()
        let pending = new Queue<IPlanVertex>()
        
        // Initialize
        g.Vertices |> Seq.iter (fun v -> colors.Add(v, GraphColor.White))

        // Enqueue heads
        let components = new Dictionary<IPlanVertex, int>()
        let component_count = fg.ConnectedComponents(components)
        for i = 0 to component_count-1 do
            let start = components |> Seq.filter (fun kv -> kv.Value = i)
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
                let v = if e.Source = current then e.Target else e.Source
                if colors.[v] = GraphColor.White then
                    colors.[v] <- GraphColor.Gray
                    pending.Enqueue(v)

        dict :> IDictionary<IPlanVertex, IServer>

    member private this.PlaceHeuristic (plan: IPlan) (servers: IList<IServer>) = 
        let dict = this.PlaceBreadthFirst plan servers
        let fg = new FlatGraph(plan)
        let g = fg :> E2.IUndirectedGraph<IPlanVertex, IPlanEdgeTag>

        let rec Iteration n = 
            let pairs = seq {for v1 in g.Vertices do for v2 in g.Vertices do yield v1, v2}
            
            let IsInSamePartitions (v1, v2) = (dict.[v1] = dict.[v2])
            
            // Assumption: v1 and v2 are not in the same partition
            let SwapGain (v1, v2) =
                let IsInternalEdge (e: IEdge<IPlanVertex, IPlanEdgeTag>) =
                    IsInSamePartitions(e.Source, e.Target)

                let IsExternalEdge (v1: IPlanVertex) (v2: IPlanVertex) (e: IEdge<IPlanVertex, IPlanEdgeTag>) =
                    let v' = if e.Source = v1 then e.Target else e.Source
                    not (IsInSamePartitions(v1, v')) && IsInSamePartitions(v2, v')

                let ExternalCost v1 v2 = 
                    v1 |> g.AdjacentEdges
                       |> Seq.filter (IsExternalEdge v1 v2)
                       |> Seq.map (fun e -> e.Tag.Load)
                       |> Seq.sum
                
                let InternalCost v = 
                    v |> g.AdjacentEdges
                      |> Seq.filter IsInternalEdge
                      |> Seq.map (fun e -> e.Tag.Load)
                      |> Seq.sum

                let reduction_v1 = ExternalCost v1 v2 - InternalCost v1
                let reduction_v2 = ExternalCost v2 v1 - InternalCost v2
                let cost = g.GetEdges v1 v2 |> Seq.map (fun e -> e.Tag.Load)
                                            |> Seq.sum

                reduction_v1 + reduction_v2 - 2.0 * cost

            let MaxGain (v1, v2) (v1', v2') = 
                let gain1 = SwapGain (v1, v2)
                let gain2 = SwapGain (v1', v2')
                if gain1 <= gain2 then (v1, v2) else (v1', v2')

            let SwapBestPair ps =
                if Seq.isEmpty ps then ()
                else 
                    let (v1, v2) = ps |> Seq.reduce MaxGain
                    let temp = dict.[v1]
                    dict.[v1] <- dict.[v2]
                    dict.[v2] <- temp

            match n with
            | 0 -> ()
            | _ -> pairs |> Seq.filter (IsInSamePartitions >> not)
                         |> Seq.filter (fun p -> (SwapGain p) > 0.0)
                         |> SwapBestPair; Iteration (n-1)
                
        Iteration 10
        dict 
        
    interface IPlacement with
        member this.Place (plan: IPlan) (servers: IList<IServer>) = 
            this.PlaceHeuristic plan servers