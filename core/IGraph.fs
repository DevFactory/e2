namespace E2

open System
open System.Collections.Generic

type IEdge<'V, 'Tag> = 
    abstract Source : 'V
    abstract Target : 'V
    abstract Tag : 'Tag

type IUndirectedGraph<'V, 'Tag> = 
    abstract Vertices : IEnumerable<'V>
    abstract Edges : IEnumerable<IEdge<'V, 'Tag>>
    abstract AddVertex : 'V -> bool
    abstract AddEdge : IEdge<'V, 'Tag> -> bool
    abstract GetEdges : 'V -> 'V -> IEnumerable<IEdge<'V, 'Tag>>
    abstract AdjacentEdges : 'V -> IEnumerable<IEdge<'V, 'Tag>>

type IVisualizable = 
    abstract Visualize : unit -> string

type IGraph<'V, 'Tag> = 
    inherit IVisualizable
    abstract Vertices : IEnumerable<'V>
    abstract Edges : IEnumerable<IEdge<'V, 'Tag>>
    abstract AddVertex : 'V -> bool
    abstract AddEdge : IEdge<'V, 'Tag> -> bool
    abstract RemoveVertex : 'V -> bool
    abstract GetEdges : 'V -> 'V -> IEnumerable<IEdge<'V, 'Tag>>
    abstract InEdges : 'V -> IEnumerable<IEdge<'V, 'Tag>>
    abstract OutEdges : 'V -> IEnumerable<IEdge<'V, 'Tag>>

type IPolicyVertex = 
    abstract Name : string
    abstract Type : string
    abstract UnitCore : float // cores per load

type IPolicyEdgeTag = 
    abstract Filter : string
    abstract Attribute : string
    abstract PipeletId : int

type IPlanVertex = 
    abstract Id : Guid
    abstract Parent : IPolicyVertex
    abstract IsPlaced : bool with get, set

type IPlanEdgeTag = 
    abstract Id : Guid
    abstract Parent : IPolicyEdgeTag
    abstract Load : float with get, set

type IPolicy = 
    inherit IGraph<IPolicyVertex, IPolicyEdgeTag>

type IPlan = 
    inherit IGraph<IPlanVertex, IPlanEdgeTag>
    abstract FindPlanVertices : IPolicyVertex -> IList<IPlanVertex>
    abstract FindPlanEdgeTags : IPolicyEdgeTag -> IList<IPlanEdgeTag>

type IPlanUpdate = 
    inherit IPlan
    abstract NewVertices : IEnumerable<IPlanVertex>
    abstract NewEdges : IEnumerable<IEdge<IPlanVertex, IPlanEdgeTag>>
    abstract RemovedVertices : IEnumerable<IPlanVertex>
    abstract GetPlan : IPlan
