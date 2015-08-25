namespace E2

open System
open System.Linq
open System.Collections.Generic
open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms

type PlanVertex(parent : IPolicyVertex) = 
    interface IPlanVertex with
        member val Id = Identifier.GetId()
        member val Parent = parent
        member val State = New with get, set
    override this.ToString() = "PlanVertex: " + (this :> IPlanVertex).Id.ToString() + " Hash: " + this.GetHashCode().ToString()

type PlanEdgeTag(parent : IPolicyEdgeTag) = 
    interface IPlanEdgeTag with
        member val Id = Identifier.GetId()
        member val Parent = parent
        member val PacketsPerSecond = 0.0 with get, set

type Plan() = 
    let mutable i = new Dictionary<IPolicyVertex, IList<IPlanVertex>>()
    let mutable e = new Dictionary<IPolicyEdgeTag, IList<IPlanEdgeTag>>()
    let mutable g = new BidirectionalGraph<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>()
    let TransformEdge(e : TaggedEdge<IPlanVertex, IPlanEdgeTag>) = 
        new E2.Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag>
    
    member this.instances 
        with internal get () = i
        and internal set (value) = i <- value
    
    member this.edges 
        with internal get () = e
        and internal set (value) = e <- value
    
    member this.graph 
        with internal get () = g
        and internal set (value) = g <- value
    
    member this.FromPolicyGraph(policy : IPolicy) = 
        policy.Vertices
        |> Seq.fold (fun r v -> 
               let v' = new PlanVertex(v)
               let lst = new List<IPlanVertex>([ v' :> IPlanVertex ])
               this.instances.Add(v, lst)
               r && this.graph.AddVertex(v')) true
        |> ignore
        policy.Edges
        |> Seq.fold (fun r e -> 
               let v1 = this.instances.[e.Source].[0]
               let v2 = this.instances.[e.Target].[0]
               let tag = new PlanEdgeTag(e.Tag)
               let e' = new TaggedEdge<IPlanVertex, IPlanEdgeTag>(v1, v2, tag)
               let lst = new List<IPlanEdgeTag>([ tag :> IPlanEdgeTag ])
               this.edges.Add(e.Tag, lst)
               r && this.graph.AddEdge(e')) true
        |> ignore
    
    interface IPlan with
        member this.Vertices = this.graph.Vertices
        member this.Edges = this.graph.Edges |> Seq.map TransformEdge
        
        member this.AddVertex v = 
            this.instances.[v.Parent].Add(v)
            this.graph.AddVertex(v)
        
        member this.AddEdge e = 
            this.edges.[e.Tag.Parent].Add(e.Tag)
            this.graph.AddEdge(new TaggedEdge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag))
        
        member this.RemoveVertex v = 
            this.instances.[v.Parent].Remove(v) |> ignore
            this.graph.RemoveVertex(v)
        
        member this.InEdges v = (this :> IPlan).Edges |> Seq.filter (fun e -> e.Target = v)
        member this.OutEdges v = (this :> IPlan).Edges |> Seq.filter (fun e -> e.Source = v)
        member this.GetEdges v1 v2 = (this :> IPlan).OutEdges(v1) |> Seq.filter (fun v -> v.Target = v2)
        member this.FindPlanVertices pv = this.instances.[pv]
        member this.FindPlanEdgeTags pe = this.edges.[pe]
        member this.Visualize() = 
            let graphviz = new GraphvizAlgorithm<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>(g)
            let OnFormatVertex(e : FormatVertexEventArgs<IPlanVertex>) = 
                e.VertexFormatter.Label <- e.Vertex.Parent.Name + " " + e.Vertex.Id.ToString()
            
            let OnFormatEdge(e : FormatEdgeEventArgs<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>) = 
                let tag = e.Edge.Tag
                e.EdgeFormatter.Label.Value <- tag.Parent.Filter + " - " + string tag.PacketsPerSecond
            graphviz.FormatVertex.Add(OnFormatVertex)
            graphviz.FormatEdge.Add(OnFormatEdge)
            graphviz.Generate(new FileDotEngine(), "")