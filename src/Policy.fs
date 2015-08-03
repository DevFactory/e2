namespace E2

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms

type PolicyVertex(name: string, t: string) = 
    interface IPolicyVertex with
        member val Name = name
        member val Type = t
        member val UnitCore = 
            match t with
            | _ -> 1.0

type PolicyEdgeTag(filter: string, attr: string, pipelet: int) = 
    interface IPolicyEdgeTag with
        member val Filter = filter 
        member val Attribute = attr 
        member val PipeletId = pipelet

type Policy() =
    let mutable g = new BidirectionalGraph<IPolicyVertex, TaggedEdge<IPolicyVertex, IPolicyEdgeTag>>()
    let TransformEdge (e: TaggedEdge<IPolicyVertex, IPolicyEdgeTag>) =
        new E2.Edge<IPolicyVertex, IPolicyEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPolicyVertex, IPolicyEdgeTag>

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