module E2.Graph

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms
open System

type NF(name: string, t: string) =
    member val id = Guid.NewGuid()
    member val name = name with get, set
    member val t = t with get, set
    member val core = 
        match t with
        | _ -> 1.0

    override this.Equals(obj) =
        match obj with
        | :? NF as o -> this.name.Equals(o.name)
        | _ -> false

    override this.GetHashCode() = this.name.GetHashCode()


[<NoEquality; NoComparison>]
type EdgeTag(filter: string, attr: string, pipelet: int) = 
    member val id = Guid.NewGuid()
    member val filter = filter with get, set
    member val attribute = attr with get, set
    member val pipelet = pipelet with get, set
    member val load = 1.0 with get, set // TODO

    member this.SetLoad(load: float) = this.load <- load


type FileDotEngine() = 
    interface IDotEngine with
        member this.Run(imageType: Dot.GraphvizImageType, dot: string, outputFileName: string) = 
            dot


type Graph(state: Parser.ParseState) = 
    // Internal graph
    let graph = new BidirectionalGraph<NF, TaggedEdge<NF, EdgeTag>>()
    
    // Add vertices
    let _ = 
        let nfs = state.V |> Map.toList |> List.map (fun (key, value) -> 
            new NF(key, value))
        (nfs |> List.fold (fun result nf -> result && graph.AddVertex(nf)) true)
    
    // Add edges
    let _ = (state.E |> List.mapi (fun i lst ->
        let add_edge result (v1, v2, e1, e2) =
            let tag = new EdgeTag(e1, e2, i)
            let vertex1 = graph.Vertices |> Seq.filter (fun v -> v.name = v1) |> Seq.head
            let vertex2 = graph.Vertices |> Seq.filter (fun v -> v.name = v2) |> Seq.head
            let edge = new TaggedEdge<NF, EdgeTag>(vertex1, vertex2, tag)
            result && graph.AddEdge(edge)
        List.fold add_edge true lst) |> List.fold (fun result r -> result && r) true)

    member this.Visualize() = 
        let graphviz = new GraphvizAlgorithm<NF, TaggedEdge<NF, EdgeTag>>(graph)
        let OnFormatVertex(e: FormatVertexEventArgs<NF>) = 
            e.VertexFormatter.Label <- e.Vertex.name + " " + e.Vertex.id.ToString()
        let OnFormatEdge(e: FormatEdgeEventArgs<NF, TaggedEdge<NF, EdgeTag>>) = 
            let attr = e.Edge.Tag
            e.EdgeFormatter.Label.Value <- string attr.pipelet + ": " + attr.filter
        graphviz.FormatVertex.Add(OnFormatVertex)
        graphviz.FormatEdge.Add(OnFormatEdge)
        graphviz.Generate(new FileDotEngine(), "")

    member this.VisualizeFlatGraph() = 
        let g = this.FlatGraph()
        let graphviz = new GraphvizAlgorithm<NF, TaggedEdge<NF, EdgeTag>>(g)
        let OnFormatVertex (e: FormatVertexEventArgs<NF>) = 
            e.VertexFormatter.Label <- e.Vertex.name + " " + e.Vertex.id.ToString()
        let OnFormatEdge (e: FormatEdgeEventArgs<NF, TaggedEdge<NF, EdgeTag>>) = 
            e.EdgeFormatter.Label.Value <- string e.Edge.Tag.load
        graphviz.FormatVertex.Add(OnFormatVertex)
        graphviz.FormatEdge.Add(OnFormatEdge)
        let str = graphviz.Generate(new FileDotEngine(), "")
        str.Replace("->", "--")

    member private this.FlatGraph() = 
        let g = new UndirectedGraph<NF, TaggedEdge<NF, EdgeTag>>(false)
        let _ = graph.Vertices |> Seq.fold (fun r v -> r && g.AddVertex(v)) true 
        for v1 in graph.Vertices do
            for v2 in graph.Vertices do
                if graph.ContainsEdge(v1, v2) || graph.ContainsEdge(v2, v1) then
                    let edges = 
                        graph.Edges |> Seq.filter (fun e -> (e.Source.Equals(v1) && e.Target.Equals(v2)) ||
                                                            (e.Source.Equals(v2) && e.Target.Equals(v1)))
                    let max_weight_edge = 
                        edges |> Seq.reduce 
                            (fun (a: TaggedEdge<NF, EdgeTag>) (b: TaggedEdge<NF, EdgeTag>) -> 
                                if a.Tag.load > b.Tag.load then a else b) 

                    g.AddEdge(max_weight_edge) |> ignore
        g

    member this.PlaceRandom(k: int) = 
        let rand = new System.Random()
        graph.Vertices |> Seq.map (fun v -> (v.id, rand.Next(k))) |> Seq.toList
       
    member this.PlaceBreadthFirstSearch(k: int, cores_per_host: float) = 
        let g = this.FlatGraph()
        
        let mutable cores = cores_per_host
        let mutable host = 0
        let mutable result = []

        let place_nf (nf: NF) (load: float) =
            let core = nf.core * load
            if core < cores then
                cores <- cores - core
                result <- (nf.id, host) :: result
            else
                cores <- cores_per_host - core
                host <- host + 1
                result <- (nf.id, host) :: result
        
        let colors = new Collections.Generic.Dictionary<NF, GraphColor>()
        let pending = new Collections.Generic.Queue<NF>()
        
        // initialize
        g.Vertices |> Seq.iter (fun v -> colors.Add(v, GraphColor.White))

        // enqueue heads
        let components = new Collections.Generic.Dictionary<NF, int>()
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

