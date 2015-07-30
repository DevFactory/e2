module E2.Graph

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms

open System
open System.Collections.Generic

open IGraph

type FileDotEngine() = 
    interface IDotEngine with
        member this.Run(imageType: Dot.GraphvizImageType, dot: string, outputFileName: string) = 
            dot

type Edge<'V, 'Tag>(source: 'V, target: 'V, tag: 'Tag) = 
    interface IEdge<'V, 'Tag> with
        member this.tag = tag
        member this.source = source
        member this.target = target

type PolicyVertex(name: string, t: string) = 
    interface IPolicyVertex with
        member val name = name
        member val t = t
        member val unit_core = 
            match t with
            | _ -> 1.0

type PlanVertex(parent: IPolicyVertex) = 
    interface IPlanVertex with
        member val id = Guid.NewGuid()
        member val parent = parent

type PolicyEdgeTag(filter: string, attr: string, pipelet: int) = 
    interface IPolicyEdgeTag with
        member val filter = filter 
        member val attribute = attr 
        member val pipelet_id = pipelet

type PlanEdgeTag(parent: IPolicyEdgeTag) =
    interface IPlanEdgeTag with
        member val id = Guid.NewGuid()
        member val parent = parent
        member val load = 1.0 

type Policy(state: Parser.ParseState) =
    let g = 
        let g = new BidirectionalGraph<IPolicyVertex, TaggedEdge<IPolicyVertex, IPolicyEdgeTag>>()
        let nfs = state.V |> Map.toList |> List.map (fun (key, value) -> 
            new PolicyVertex(key, value))
        nfs |> List.fold (fun result nf -> result && g.AddVertex(nf)) true
            |> ignore
        state.E |> List.mapi (fun i lst ->
            let add_edge result (v1, v2, e1, e2) =
                let tag = new PolicyEdgeTag(e1, e2, i)
                let vertex1 = g.Vertices |> Seq.filter (fun v -> v.name = v1) |> Seq.head
                let vertex2 = g.Vertices |> Seq.filter (fun v -> v.name = v2) |> Seq.head
                let edge = new TaggedEdge<IPolicyVertex, IPolicyEdgeTag>(vertex1, vertex2, tag)
                result && g.AddEdge(edge)
            List.fold add_edge true lst) 
                |> List.fold (fun result r -> result && r) true
                |> ignore
        g
    
    let TransformEdge (e: TaggedEdge<IPolicyVertex, IPolicyEdgeTag>) =
        new Edge<IPolicyVertex, IPolicyEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPolicyVertex, IPolicyEdgeTag>

    interface IPolicy with
        member this.Vertices = g.Vertices
        member this.Edges = g.Edges |> Seq.map TransformEdge
        member this.AddVertex(v) = g.AddVertex(v)
        member this.AddEdge(e) = g.AddEdge(new TaggedEdge<IPolicyVertex, IPolicyEdgeTag>(e.source, e.target, e.tag))
        member this.InEdges(v) = g.InEdges(v) |> Seq.map TransformEdge
        member this.OutEdges(v) = g.OutEdges(v) |> Seq.map TransformEdge
        
type Plan(policy: IPolicy) =
    let instances = new Dictionary<IPolicyVertex, IList<IPlanVertex>>()
    let g = 
        let g = new BidirectionalGraph<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>()
        
        policy.Vertices |> Seq.fold (fun r v ->
            let v' = new PlanVertex(v)
            let lst = new List<IPlanVertex>([v' :> IPlanVertex])
            instances.Add(v, lst)
            r && g.AddVertex(v')) true
                        |> ignore
        
        policy.Edges |> Seq.fold (fun r e ->
            let v1 = instances.[e.source].[0]
            let v2 = instances.[e.target].[0]
            let tag = new PlanEdgeTag(e.tag)
            let e' = new TaggedEdge<IPlanVertex, IPlanEdgeTag>(v1, v2, tag)
            r && g.AddEdge(e')) true
                    |> ignore

        g

    let TransformEdge (e: TaggedEdge<IPlanVertex, IPlanEdgeTag>) =
        new Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag>

    interface IPlan with
        member this.Vertices = g.Vertices
        member this.Edges = g.Edges |> Seq.map TransformEdge
        member this.AddVertex(v) = g.AddVertex(v)
        member this.AddEdge(e) = g.AddEdge(new TaggedEdge<IPlanVertex, IPlanEdgeTag>(e.source, e.target, e.tag))
        member this.InEdges(v) = g.InEdges(v) |> Seq.map TransformEdge
        member this.OutEdges(v) = g.OutEdges(v) |> Seq.map TransformEdge
        member this.instance_table = instances :> IDictionary<IPolicyVertex, IList<IPlanVertex>>

    interface IVisualize with
        member this.Visualize() = 
            let graphviz = new GraphvizAlgorithm<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>(g)
            let OnFormatVertex(e: FormatVertexEventArgs<IPlanVertex>) = 
                e.VertexFormatter.Label <- e.Vertex.parent.name + " " + e.Vertex.id.ToString()
            let OnFormatEdge(e: FormatEdgeEventArgs<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>) = 
                let tag = e.Edge.Tag.parent
                e.EdgeFormatter.Label.Value <- string tag.pipelet_id + ": " + tag.filter
            graphviz.FormatVertex.Add(OnFormatVertex)
            graphviz.FormatEdge.Add(OnFormatEdge)
            graphviz.Generate(new FileDotEngine(), "")

//    member private this.FlatGraph() = 
//        let g = new UndirectedGraph<InstanceNF, TaggedEdge<InstanceNF, InstanceEdgeTag>>(false)
//        let _ = igraph.Vertices |> Seq.fold (fun r v -> r && g.AddVertex(v)) true 
//        for v1 in igraph.Vertices do
//            for v2 in igraph.Vertices do
//                let edges = igraph.Edges |> Seq.filter (fun e -> (e.Source.Equals(v1) && e.Target.Equals(v2)) ||
//                                                                 (e.Source.Equals(v2) && e.Target.Equals(v1)))
//                if not (Seq.isEmpty edges) then
//                    let max_weight_edge = edges |> Seq.reduce (fun a b -> if a.Tag.load > b.Tag.load then a else b)
//                    g.AddEdge(max_weight_edge) |> ignore
//        g
//
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

