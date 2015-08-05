namespace E2

open System
open System.Linq
open System.Collections.Generic

open QuickGraph
open QuickGraph.Graphviz
open QuickGraph.Serialization
open QuickGraph.Algorithms

type PlanVertex(parent: IPolicyVertex) = 
    interface IPlanVertex with
        member val Id = Guid.NewGuid()
        member val Parent = parent
        member val IsPlaced = true with get, set

type PlanEdgeTag(parent: IPolicyEdgeTag) =
    interface IPlanEdgeTag with
        member val Id = Guid.NewGuid()
        member val Parent = parent
        member val Load = 0.0 with get, set
        
type Plan() =
    let mutable i = new Dictionary<IPolicyVertex, IList<IPlanVertex>>()
    let mutable g = new BidirectionalGraph<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>()
    let TransformEdge (e: TaggedEdge<IPlanVertex, IPlanEdgeTag>) =
        new E2.Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag> 

    member internal this.instances
        with get () = i
        and set (value) = i <- value

    member internal this.graph 
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

        member this.AddVertex v = 
            this.instances.[v.Parent].Add(v)
            this.graph.AddVertex(v)

        member this.AddEdge e = 
            this.graph.AddEdge(new TaggedEdge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag))

        member this.RemoveVertex v = 
            this.instances.[v.Parent].Remove(v) |> ignore
            this.graph.RemoveVertex(v)
        
        member this.InEdges v = this.graph.InEdges(v) |> Seq.map TransformEdge
        member this.OutEdges v = this.graph.OutEdges(v) |> Seq.map TransformEdge
        
        member this.GetEdges v1 v2 = 
            this.graph.OutEdges(v1) |> Seq.filter (fun v -> v.Target = v2) 
                                    |> Seq.map TransformEdge
        
        member this.FindInstanceFromPolicy pv = this.instances.[pv]

        member this.Visualize() = 
            let graphviz = new GraphvizAlgorithm<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>(g)
            let OnFormatVertex(e: FormatVertexEventArgs<IPlanVertex>) = 
                e.VertexFormatter.Label <- e.Vertex.Parent.Name + " " + e.Vertex.Id.ToString()
            let OnFormatEdge(e: FormatEdgeEventArgs<IPlanVertex, TaggedEdge<IPlanVertex, IPlanEdgeTag>>) = 
                let tag = e.Edge.Tag
                e.EdgeFormatter.Label.Value <- tag.Parent.Filter + " - " + string tag.Load
            graphviz.FormatVertex.Add(OnFormatVertex)
            graphviz.FormatEdge.Add(OnFormatEdge)
            graphviz.Generate(new FileDotEngine(), "")

//type PlanUpdate(plan: Plan) = 
//    let g = plan.graph.Clone()
//
//    let new_v = new HashSet<IPlanVertex>()
//    let new_e = new HashSet<TaggedEdge<IPlanVertex, IPlanEdgeTag>>()
//    
//    let del_v = new HashSet<IPlanVertex>()
//
//    let TransformEdge (e: TaggedEdge<IPlanVertex, IPlanEdgeTag>) =
//        new E2.Edge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag) :> IEdge<IPlanVertex, IPlanEdgeTag>
//
//    let TransformIEdge (e: IEdge<IPlanVertex, IPlanEdgeTag>) =
//        new TaggedEdge<IPlanVertex, IPlanEdgeTag>(e.Source, e.Target, e.Tag)
//
//    interface IPlanUpdate with
//        member this.Vertices = g.Vertices.Union(new_v).Except(del_v)
//        
//        member this.Edges = g.Edges.Union(new_e) |> Seq.map TransformEdge
//
//        member this.AddVertex v = 
//            new_v.Add(v)
//
//        member this.RemoveVertex v = 
//            del_v.Add(v)
//
//        member this.AddEdge e = 
//            new_e.Add(TransformIEdge e)
//        
//        member this.InEdges v = 
//            g.Edges.Union(new_e) |> Seq.filter (fun e -> e.Target = v)
//                                 |> Seq.map TransformEdge
//        member this.OutEdges v = 
//            g.Edges.Union(new_e) |> Seq.filter (fun e -> e.Source = v)
//                                 |> Seq.map TransformEdge
//
//        member this.GetEdges v1 v2 = 
//            (this :> IPlanUpdate).OutEdges v1 |> Seq.filter (fun v -> v.Target = v2) 
//
//        member this.GetPlan = 
//            let p = new Plan()
//            (this :> IPlanUpdate).Vertices |> Seq.iter (fun v -> (p :> IPlan).AddVertex v |> ignore)
//            (this :> IPlanUpdate).Edges |> Seq.iter (fun e -> (p :> IPlan).AddEdge e |> ignore)
//            p :> IPlan
//
//        member this.FindInstanceFromPolicy pv = 
//            (this :> IPlanUpdate).GetPlan.FindInstanceFromPolicy pv
//
//        member this.NewVertices = new_v :> IEnumerable<IPlanVertex>
//        member this.NewEdges = new_e |> Seq.map TransformEdge
//        member this.RemovedVertices = del_v :> IEnumerable<IPlanVertex>
     