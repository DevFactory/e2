module E2.Graph

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization


type VF(name: string, t: string) =
    member val name = name with get, set
    member val t = t with get, set
    member val cycles_per_pkt = None with get, set // TODO

    override this.Equals(obj) =
        match obj with
        | :? VF as o -> (this.name = o.name)
        | _ -> false

    override this.GetHashCode() = hash this.name


[<NoEquality; NoComparison>]
type EdgeTag(filter: string, attr: string, pipelet: int) = 
    member val filter = filter with get, set
    member val attribute = attr with get, set
    member val pipelet = pipelet with get, set
    member val pkts_per_sec = None with get, set // TODO


type FileDotEngine() = 
    interface IDotEngine with
        member this.Run(imageType: Dot.GraphvizImageType, dot: string, outputFileName: string) = 
            dot


type Graph(state: Parser.ParseState) = 
    // Internal graph
    let graph = new BidirectionalGraph<VF, TaggedEdge<VF, EdgeTag>>()
    
    // Add vertices
    let _ = 
        let vfs = state.V |> Map.toList |> List.map (fun (key, value) -> 
            new VF(key, value))
        (vfs |> List.fold (fun result vf -> result && graph.AddVertex(vf)) true)
    
    // Add edges
    let _ = (state.E |> List.mapi (fun i lst ->
        let add_edge result (v1, v2, e1, e2) =
            let tag = new EdgeTag(e1, e2, i)
            let vertex1 = new VF(v1, state.V.[v1])
            let vertex2 = new VF(v2, state.V.[v2])
            let edge = new TaggedEdge<VF, EdgeTag>(vertex1, vertex2, tag)
            result && graph.AddEdge(edge)
        List.fold add_edge true lst) |> List.fold (fun result r -> result && r) true)

    member this.ToGraphViz() = 
        let graphviz = new GraphvizAlgorithm<VF, TaggedEdge<VF, EdgeTag>>(graph)
        let OnFormatVertex(e: FormatVertexEventArgs<VF>) = 
            e.VertexFormatter.Label <- e.Vertex.name
        let OnFormatEdge(e: FormatEdgeEventArgs<VF, TaggedEdge<VF, EdgeTag>>) = 
            let attr = e.Edge.Tag
            e.EdgeFormatter.Label.Value <- string attr.pipelet + ": " + attr.filter
        graphviz.FormatVertex.Add(OnFormatVertex)
        graphviz.FormatEdge.Add(OnFormatEdge)
        graphviz.Generate(new FileDotEngine(), "")

    member this.ToGraphML() = 
        use stream = new System.IO.MemoryStream()
        graph.ToDirectedGraphML((fun v -> v.name), (fun e -> e.Tag.filter)).WriteXml(stream)
        System.Text.Encoding.ASCII.GetString(stream.GetBuffer(), 0, int(stream.Length))