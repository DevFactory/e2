module E2.Graph

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms
open System
open System.Collections.Generic

type PolicyNF(name: string, t: string) = 
    member val name = name with get, set
    member val t = t with get, set
    member val core = 
        match t with
        | _ -> 1.0
    member val instances = List<InstanceNF>() with get, set
and InstanceNF(parent: PolicyNF) = 
    member val id = Guid.NewGuid()
    member val parent = parent


[<NoEquality; NoComparison>]
type PolicyEdgeTag(filter: string, attr: string, pipelet: int) = 
    member val filter = filter with get, set
    member val attribute = attr with get, set
    member val pipelet = pipelet with get, set
    member val instances = List<InstanceEdgeTag>() with get, set
and InstanceEdgeTag(parent: PolicyEdgeTag) =
    member val id = Guid.NewGuid()
    member val parent = parent
    member val load = 1.0 with get, set


type FileDotEngine() = 
    interface IDotEngine with
        member this.Run(imageType: Dot.GraphvizImageType, dot: string, outputFileName: string) = 
            dot


type Graph(state: Parser.ParseState) = 
    let CreatePolicyGraph (state: Parser.ParseState) = 
        let graph = new BidirectionalGraph<PolicyNF, TaggedEdge<PolicyNF, PolicyEdgeTag>>()
        
        let nfs = state.V |> Map.toList |> List.map (fun (key, value) -> 
            new PolicyNF(key, value))

        nfs |> List.fold (fun result nf -> result && graph.AddVertex(nf)) true
            |> ignore

        state.E |> List.mapi (fun i lst ->
            let add_edge result (v1, v2, e1, e2) =
                let tag = new PolicyEdgeTag(e1, e2, i)
                let vertex1 = graph.Vertices |> Seq.filter (fun v -> v.name = v1) |> Seq.head
                let vertex2 = graph.Vertices |> Seq.filter (fun v -> v.name = v2) |> Seq.head
                let edge = new TaggedEdge<PolicyNF, PolicyEdgeTag>(vertex1, vertex2, tag)
                result && graph.AddEdge(edge)
            List.fold add_edge true lst) 
                |> List.fold (fun result r -> result && r) true
                |> ignore
        
        graph

    let CreateInstanceGraph (instance_graph: BidirectionalGraph<PolicyNF, TaggedEdge<PolicyNF, PolicyEdgeTag>>) =
        let graph = new BidirectionalGraph<InstanceNF, TaggedEdge<InstanceNF, InstanceEdgeTag>>()

        instance_graph.Vertices |> Seq.fold (fun r v -> 
            let v' = new InstanceNF(v)
            v.instances.Add(v')
            r && graph.AddVertex(v')) true
                                |> ignore

        instance_graph.Edges |> Seq.fold (fun r e -> 
            let v1 = e.Source.instances.[0]
            let v2 = e.Target.instances.[0]
            let tag = new InstanceEdgeTag(e.Tag)
            let e' = new TaggedEdge<InstanceNF, InstanceEdgeTag>(v1, v2, tag)
            r && graph.AddEdge(e')) true
                             |> ignore
        
        graph

    let pgraph = CreatePolicyGraph(state)
    let igraph = CreateInstanceGraph(pgraph)

    member this.Visualize() = 
        let graphviz = new GraphvizAlgorithm<InstanceNF, TaggedEdge<InstanceNF, InstanceEdgeTag>>(igraph)
        let OnFormatVertex(e: FormatVertexEventArgs<InstanceNF>) = 
            e.VertexFormatter.Label <- e.Vertex.parent.name + " " + e.Vertex.id.ToString()
        let OnFormatEdge(e: FormatEdgeEventArgs<InstanceNF, TaggedEdge<InstanceNF, InstanceEdgeTag>>) = 
            let tag = e.Edge.Tag.parent
            e.EdgeFormatter.Label.Value <- string tag.pipelet + ": " + tag.filter
        graphviz.FormatVertex.Add(OnFormatVertex)
        graphviz.FormatEdge.Add(OnFormatEdge)
        graphviz.Generate(new FileDotEngine(), "")

    member this.VisualizeFlatGraph() = 
        let g = this.FlatGraph()
        let graphviz = new GraphvizAlgorithm<InstanceNF, TaggedEdge<InstanceNF, InstanceEdgeTag>>(g)
        let OnFormatVertex (e: FormatVertexEventArgs<InstanceNF>) = 
            e.VertexFormatter.Label <- e.Vertex.parent.name + " " + e.Vertex.id.ToString()
        let OnFormatEdge (e: FormatEdgeEventArgs<InstanceNF, TaggedEdge<InstanceNF, InstanceEdgeTag>>) = 
            e.EdgeFormatter.Label.Value <- string e.Edge.Tag.load
        graphviz.FormatVertex.Add(OnFormatVertex)
        graphviz.FormatEdge.Add(OnFormatEdge)
        let str = graphviz.Generate(new FileDotEngine(), "")
        str.Replace("->", "--")

    member private this.FlatGraph() = 
        let g = new UndirectedGraph<InstanceNF, TaggedEdge<InstanceNF, InstanceEdgeTag>>(false)
        let _ = igraph.Vertices |> Seq.fold (fun r v -> r && g.AddVertex(v)) true 
        for v1 in igraph.Vertices do
            for v2 in igraph.Vertices do
                let edges = igraph.Edges |> Seq.filter (fun e -> (e.Source.Equals(v1) && e.Target.Equals(v2)) ||
                                                                 (e.Source.Equals(v2) && e.Target.Equals(v1)))
                if not (Seq.isEmpty edges) then
                    let max_weight_edge = edges |> Seq.reduce (fun a b -> if a.Tag.load > b.Tag.load then a else b)
                    g.AddEdge(max_weight_edge) |> ignore
        g

    member this.PlaceRandom(k: int) = 
        let rand = new System.Random()
        igraph.Vertices |> Seq.map (fun v -> (v.id, rand.Next(k))) |> Seq.toList
       
    member this.PlaceBreadthFirstSearch(k: int, host_cores: float []) = 
        let g = this.FlatGraph()
                
        let mutable host = 0
        let mutable cores = host_cores.[host]
        let mutable result = []

        let place_nf (nf: InstanceNF) (load: float) =
            let core = nf.parent.core * load
            if core < cores then
                cores <- cores - core
                result <- (nf.id, host) :: result
            else
                host <- host + 1
                cores <- host_cores.[host] - core
                result <- (nf.id, host) :: result
        
        let colors = new Collections.Generic.Dictionary<InstanceNF, GraphColor>()
        let pending = new Collections.Generic.Queue<InstanceNF>()
        
        // initialize
        g.Vertices |> Seq.iter (fun v -> colors.Add(v, GraphColor.White))

        // enqueue heads
        let components = new Collections.Generic.Dictionary<InstanceNF, int>()
        let component_count = g.ConnectedComponents(components)
        for i = 0 to component_count-1 do
            let start = components |> Seq.filter (fun kv -> kv.Value = i)
                                   |> Seq.map (fun kv -> kv.Key)
                                   |> Seq.head
            colors.[start] <- GraphColor.Gray
            pending.Enqueue(start)
            place_nf start 1.0

        // main bfs loop
        while pending.Count <> 0 do
            let current = pending.Dequeue()
            colors.[current] <- GraphColor.Black

            let edges = g.AdjacentEdges(current)
            for e in edges do
                let v = if e.Source = current then e.Target else e.Source
                if colors.[v] = GraphColor.White then
                    colors.[v] <- GraphColor.Gray
                    pending.Enqueue(v)
                    place_nf v e.Tag.load

        result

