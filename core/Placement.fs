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
                
        let mutable serverIndex = 0
        let mutable usedCores = 0.0
        let mutable dict = Dictionary<IPlanVertex, IServer>()
        
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

        dict
        
    interface IPlacement with
        member this.Place (plan: IPlan) (servers: IList<IServer>) = 
            this.PlaceRandom plan servers