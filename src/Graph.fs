namespace E2

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms

open System
open System.Collections.Generic

type FileDotEngine() = 
    interface IDotEngine with
        member this.Run(imageType: Dot.GraphvizImageType, dot: string, outputFileName: string) = 
            dot

type Edge<'V, 'Tag>(source: 'V, target: 'V, tag: 'Tag) = 
    interface IEdge<'V, 'Tag> with
        member this.Tag = tag
        member this.Source = source
        member this.Target = target

type PolicyVertex(name: string, t: string) = 
    interface IPolicyVertex with
        member val Name = name
        member val Type = t
        member val UnitCore = 
            match t with
            | _ -> 1.0

type PlanVertex(parent: IPolicyVertex) = 
    interface IPlanVertex with
        member val Id = Guid.NewGuid()
        member val Parent = parent

type PolicyEdgeTag(filter: string, attr: string, pipelet: int) = 
    interface IPolicyEdgeTag with
        member val Filter = filter 
        member val Attribute = attr 
        member val PipeletId = pipelet

type PlanEdgeTag(parent: IPolicyEdgeTag) =
    interface IPlanEdgeTag with
        member val Id = Guid.NewGuid()
        member val Parent = parent
        member val Load = 1.0 

type Policy() =
    let mutable g = new BidirectionalGraph<IPolicyVertex, TaggedEdge<IPolicyVertex, IPolicyEdgeTag>>()
    let TransformEdge (e: TaggedEdge<IPolicyVertex, IPolicyEdgeTag>) =
        new Edge<IPolicyVertex, IPolicyEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPolicyVertex, IPolicyEdgeTag>

    member private this.graph 
        with get () = g
        and set (value) = g <- value

    member this.LoadPolicyState (state: Parser.ParseState) = 
        let nfs = state.V |> Map.toList |> List.map (fun (key, value) -> 
            new PolicyVertex(key, value))
        nfs |> List.fold (fun result nf -> result && this.graph.AddVertex(nf)) true
            |> ignore
        state.E |> List.mapi (fun i lst ->
            let AddEdge result (v1, v2, e1, e2) =
                let tag = new PolicyEdgeTag(e1, e2, i)
                let vertex1 = this.graph.Vertices |> Seq.filter (fun v -> v.Name = v1) |> Seq.head
                let vertex2 = this.graph.Vertices |> Seq.filter (fun v -> v.Name = v2) |> Seq.head
                let edge = new TaggedEdge<IPolicyVertex, IPolicyEdgeTag>(vertex1, vertex2, tag)
                result && this.graph.AddEdge(edge)
            List.fold AddEdge true lst) 
                |> List.fold (fun result r -> result && r) true
                |> ignore

    interface IPolicy with
        member this.Vertices = this.graph.Vertices
        member this.Edges = this.graph.Edges |> Seq.map TransformEdge
        member this.AddVertex v = this.graph.AddVertex(v)
        member this.AddEdge e = this.graph.AddEdge(new TaggedEdge<IPolicyVertex, IPolicyEdgeTag>(e.Source, e.Target, e.Tag))
        member this.InEdges v = this.graph.InEdges(v) |> Seq.map TransformEdge
        member this.OutEdges v = this.graph.OutEdges(v) |> Seq.map TransformEdge
        member this.GetEdges v1 v2 = 
            this.graph.OutEdges(v1) |> Seq.filter (fun v -> v.Target = v2)
                                    |> Seq.map TransformEdge
        member this.Clone() = 
            let p' = new Policy()
            p'.graph <- this.graph.Clone()
            p' :> obj

    interface IVisualizable with
        member this.Visualize() = 
            let graphviz = new GraphvizAlgorithm<IPolicyVertex, TaggedEdge<IPolicyVertex, IPolicyEdgeTag>>(this.graph)
            let OnFormatVertex(e: FormatVertexEventArgs<IPolicyVertex>) = 
                e.VertexFormatter.Label <- e.Vertex.Name
            let OnFormatEdge(e: FormatEdgeEventArgs<IPolicyVertex, TaggedEdge<IPolicyVertex, IPolicyEdgeTag>>) = 
                let tag = e.Edge.Tag
                e.EdgeFormatter.Label.Value <- string tag.Filter
            graphviz.FormatVertex.Add(OnFormatVertex)
            graphviz.FormatEdge.Add(OnFormatEdge)
            graphviz.Generate(new FileDotEngine(), "")
        
type Plan() =
    let mutable i = new Dictionary<IPolicyVertex, IList<IPlanVertex>>()
    let mutable g = new BidirectionalGraph<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>()
    let TransformEdge (e: TaggedEdge<IPlanVertex, IPlanEdgeTag>) =
        new Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag> 

    member private this.instances
        with get () = i
        and set (value) = i <- value

    member private this.graph 
        with get () = g
        and set (value) = g <- value

    member this.FromPolicyGraph (policy: IPolicy) = 
        policy.Vertices |> Seq.fold (fun r v ->
            let v' = new PlanVertex(v)
            let lst = new List<IPlanVertex>([v' :> IPlanVertex])
            this.instances.Add(v, lst)
            r && this.graph.AddVertex(v')) true
                        |> ignore
        policy.Edges |> Seq.fold (fun r e ->
            let v1 = this.instances.[e.Source].[0]
            let v2 = this.instances.[e.Target].[0]
            let tag = new PlanEdgeTag(e.Tag)
            let e' = new TaggedEdge<IPlanVertex, IPlanEdgeTag>(v1, v2, tag)
            r && this.graph.AddEdge(e')) true
                     |> ignore

    interface IPlan with
        member this.Vertices = this.graph.Vertices
        member this.Edges = this.graph.Edges |> Seq.map TransformEdge
        member this.AddVertex v = this.graph.AddVertex(v)
        member this.AddEdge e = this.graph.AddEdge(new TaggedEdge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag))
        member this.InEdges v = this.graph.InEdges(v) |> Seq.map TransformEdge
        member this.OutEdges v = this.graph.OutEdges(v) |> Seq.map TransformEdge
        member this.GetEdges v1 v2 = 
            this.graph.OutEdges(v1) |> Seq.filter (fun v -> v.Target = v2) 
                                    |> Seq.map TransformEdge
        member this.FindInstanceFromPolicy(pv) = this.instances.[pv]
        member this.Clone() = 
            let p' = new Plan()
            p'.graph <- this.graph.Clone()
            p'.instances <- new Dictionary<IPolicyVertex, IList<IPlanVertex>>(this.instances)
            p' :> obj

    interface IVisualizable with
        member this.Visualize() = 
            let graphviz = new GraphvizAlgorithm<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>(g)
            let OnFormatVertex(e: FormatVertexEventArgs<IPlanVertex>) = 
                e.VertexFormatter.Label <- e.Vertex.Parent.Name + " " + e.Vertex.Id.ToString()
            let OnFormatEdge(e: FormatEdgeEventArgs<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>) = 
                let tag = e.Edge.Tag.Parent
                e.EdgeFormatter.Label.Value <- string tag.PipeletId + ": " + tag.Filter
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

