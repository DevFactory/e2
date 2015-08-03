namespace E2

open System
open System.Collections.Generic

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms

type PlanVertex(parent: IPolicyVertex) = 
    interface IPlanVertex with
        member val Id = Guid.NewGuid()
        member val Parent = parent

type PlanEdgeTag(parent: IPolicyEdgeTag) =
    interface IPlanEdgeTag with
        member val Id = Guid.NewGuid()
        member val Parent = parent
        member val Load = 1.0 
        
type Plan() =
    let mutable i = new Dictionary<IPolicyVertex, IList<IPlanVertex>>()
    let mutable g = new BidirectionalGraph<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>()
    let TransformEdge (e: TaggedEdge<IPlanVertex, IPlanEdgeTag>) =
        new E2.Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag> 

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