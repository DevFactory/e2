namespace E2

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms

open System
open System.Collections.Generic

type Edge<'V, 'Tag>(source: 'V, target: 'V, tag: 'Tag) = 
    interface IEdge<'V, 'Tag> with
        member this.Tag = tag
        member this.Source = source
        member this.Target = target

type FlatGraph(origin: E2.IGraph<IPlanVertex, IPlanEdgeTag>) = 
    let mutable g = new UndirectedGraph<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>(false)

    let TransformEdge (e: TaggedEdge<IPlanVertex, IPlanEdgeTag>) =
        new Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag> 
    
    let TransformIEdge (e: IEdge<IPlanVertex, IPlanEdgeTag>) =
        new TaggedEdge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag)

    do 
        origin.Vertices |> Seq.fold (fun r v -> r && g.AddVertex(v)) true |> ignore
        for v1 in origin.Vertices do
            for v2 in origin.Vertices do
                let edges = origin.Edges |> Seq.filter (fun e -> (e.Source = v1 && e.Target = v2) ||
                                                                 (e.Target = v1 && e.Source = v1))
                if not (Seq.isEmpty edges) then 
                    let maxWeightEdge = edges |> Seq.reduce (fun a b -> if a.Tag.Load > b.Tag.Load then a else b)
                                              |> TransformIEdge
                    g.AddEdge(maxWeightEdge) |> ignore
    
    interface E2.IGraph<IPlanVertex, IPlanEdgeTag> with
        member this.Vertices = g.Vertices
        member this.Edges = g.Edges |> Seq.map TransformEdge
        member this.AddVertex v = g.AddVertex(v)
        member this.AddEdge e = g.AddEdge(TransformIEdge e)
        member this.InEdges v = g.AdjacentEdges(v) |> Seq.map TransformEdge
        member this.OutEdges v = g.AdjacentEdges(v) |> Seq.map TransformEdge
        member this.GetEdges v1 v2 = 
            g.AdjacentEdges(v1) |> Seq.filter (fun v -> v.Target = v2 || v.Source = v2) 
                                |> Seq.map TransformEdge
            
//    member this.PlaceRandom(k: int) = 
//        let rand = new System.Random()
//        igraph.Vertices |> Seq.map (fun v -> (v.id, rand.Next(k))) |> Seq.toList
//       
//    member this.PlaceBreadthFirstSearch(k: int, host_cores: float []) = 
//        let g = this.FlatGraph()
//                
//        let mutable host = 0
//        let mutable cores = host_cores.[host]
//        let mutable result = []
//
//        let place_nf (nf: InstanceNF) (load: float) =
//            let core = nf.parent.core // * load
//            if core < cores then
//                cores <- cores - core
//                result <- (nf.id, host) :: result
//            else
//                host <- host + 1
//                cores <- host_cores.[host] - core
//                result <- (nf.id, host) :: result
//        
//        let colors = new Collections.Generic.Dictionary<InstanceNF, GraphColor>()
//        let pending = new Collections.Generic.Queue<InstanceNF>()
//        
//        // initialize
//        g.Vertices |> Seq.iter (fun v -> colors.Add(v, GraphColor.White))
//
//        // enqueue heads
//        let components = new Collections.Generic.Dictionary<InstanceNF, int>()
//        let component_count = g.ConnectedComponents(components)
//        for i = 0 to component_count-1 do
//            let start = components |> Seq.filter (fun kv -> kv.Value = i)
//                                   |> Seq.map (fun kv -> kv.Key)
//                                   |> Seq.head
//            colors.[start] <- GraphColor.Gray
//            pending.Enqueue(start)
//            place_nf start 1.0
//
//        // main bfs loop
//        while pending.Count <> 0 do
//            let current = pending.Dequeue()
//            colors.[current] <- GraphColor.Black
//
//            let edges = g.AdjacentEdges(current)
//            for e in edges do
//                let v = if e.Source = current then e.Target else e.Source
//                if colors.[v] = GraphColor.White then
//                    colors.[v] <- GraphColor.Gray
//                    pending.Enqueue(v)
//                    place_nf v e.Tag.load
//
//        result
//
//    member this.GetInstance(id: Guid) = 
//        igraph.Vertices |> Seq.filter (fun v -> v.id = id) |> Seq.head

