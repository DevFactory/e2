namespace E2

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms
open System
open System.Collections.Generic

type Edge<'V, 'Tag>(source : 'V, target : 'V, tag : 'Tag) = 
    interface IEdge<'V, 'Tag> with
        member this.Tag = tag
        member this.Source = source
        member this.Target = target

type FlatGraph(origin : E2.IGraph<IPlanVertex, IPlanEdgeTag>) = 
    let mutable g = new UndirectedGraph<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>(false)
    let TransformEdge(e : TaggedEdge<IPlanVertex, IPlanEdgeTag>) = 
        new Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag>
    let TransformIEdge(e : IEdge<IPlanVertex, IPlanEdgeTag>) = 
        new TaggedEdge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag)
    
    do 
        origin.Vertices
        |> Seq.fold (fun r v -> r && g.AddVertex(v)) true
        |> ignore
        for v1 in origin.Vertices do
            for v2 in origin.Vertices do
                let edges = 
                    origin.Edges 
                    |> Seq.filter (fun e -> (e.Source = v1 && e.Target = v2) || (e.Target = v1 && e.Source = v1))
                if not (Seq.isEmpty edges) then 
                    let maxWeightEdge = 
                        edges
                        |> Seq.reduce (fun a b -> 
                               if a.Tag.Load >= b.Tag.Load then a
                               else b)
                        |> TransformIEdge
                    g.AddEdge(maxWeightEdge) |> ignore
    
    member this.ConnectedComponents(components : IDictionary<IPlanVertex, int>) = g.ConnectedComponents components
    interface E2.IUndirectedGraph<IPlanVertex, IPlanEdgeTag> with
        member this.Vertices = g.Vertices
        member this.Edges = g.Edges |> Seq.map TransformEdge
        member this.AddVertex v = g.AddVertex(v)
        member this.AddEdge e = g.AddEdge(TransformIEdge e)
        member this.AdjacentEdges v = g.AdjacentEdges(v) |> Seq.map TransformEdge
        member this.GetEdges v1 v2 = 
            g.AdjacentEdges(v1)
            |> Seq.filter (fun v -> v.Target = v2 || v.Source = v2)
            |> Seq.map TransformEdge
