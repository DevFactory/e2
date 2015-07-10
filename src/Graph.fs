module E2.Graph

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization

type VF = {
    name: string;
    t: string;
    weight: double;
}

type Attribute = {
    filter: string;
    attr: string;
    pipelet_id: int;
    weight: double;
}

type FileDotEngine() = 
    interface IDotEngine with
        member this.Run(imageType: Dot.GraphvizImageType, dot: string, outputFileName: string) = 
            dot

type Graph(state: Parser.ParseState) = 
    // Internal graph
    let graph = new BidirectionalGraph<VF, TaggedEdge<VF, Attribute>>()
    
    // Add vertices
    let _ = 
        let vfs = state.V |> Map.toList |> List.map (fun (key, value) -> 
            {name = key; t = value; weight = 1.0})
        (vfs |> List.fold (fun result vf -> result && graph.AddVertex(vf)) true)
    
    // Add edges
    let _ = (state.E |> List.mapi (fun i lst ->
        let add_edge result (v1, v2, e1, e2) =
            let tag = {filter = e1; attr = e2; pipelet_id = i; weight = 0.0}
            let vertex1 = {name = v1; t = state.V.[v1]; weight = 1.0}
            let vertex2 = {name = v2; t = state.V.[v2]; weight = 1.0}
            let edge = new TaggedEdge<VF, Attribute>(vertex1, vertex2, tag)
            result && graph.AddEdge(edge)
        List.fold add_edge true lst) |> List.fold (fun result r -> result && r) true)

    member this.ToGraphViz() = 
        let graphviz = new GraphvizAlgorithm<VF, TaggedEdge<VF, Attribute>>(graph)
        let OnFormatVertex(e: FormatVertexEventArgs<VF>) = 
            e.VertexFormatter.Label <- e.Vertex.name
        let OnFormatEdge(e: FormatEdgeEventArgs<VF, TaggedEdge<VF, Attribute>>) = 
            let attr = e.Edge.Tag
            e.EdgeFormatter.Label.Value <- string attr.pipelet_id + ": " + attr.filter
        graphviz.FormatVertex.Add(OnFormatVertex)
        graphviz.FormatEdge.Add(OnFormatEdge)
        graphviz.Generate(new FileDotEngine(), "")

    member this.ToGraphML() = 
        use stream = new System.IO.MemoryStream()
        graph.ToDirectedGraphML((fun v -> v.name), (fun e -> e.Tag.filter)).WriteXml(stream)
        System.Text.Encoding.ASCII.GetString(stream.GetBuffer(), 0, int(stream.Length))