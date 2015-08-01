namespace E2

open System
open System.Collections.Generic

type IEdge<'V, 'Tag> = 
    abstract Source: 'V
    abstract Target: 'V
    abstract Tag: 'Tag

type IGraph<'V, 'Tag> = 
    inherit ICloneable
    abstract Vertices: IEnumerable<'V>
    abstract Edges: IEnumerable<IEdge<'V, 'Tag>>
    
    abstract AddVertex: 'V -> bool
    abstract AddEdge: IEdge<'V, 'Tag> -> bool
    
    abstract GetEdges: 'V -> 'V -> IEnumerable<IEdge<'V, 'Tag>>

    abstract InEdges: 'V -> IEnumerable<IEdge<'V, 'Tag>>
    abstract OutEdges: 'V -> IEnumerable<IEdge<'V, 'Tag>>

type IPolicyVertex = 
    abstract Name: string
    abstract Type: string
    abstract UnitCore: float

type IPolicyEdgeTag = 
    abstract Filter: string
    abstract Attribute: string
    abstract PipeletId: int

type IPlanVertex = 
    abstract Id: Guid
    abstract Parent: IPolicyVertex

type IPlanEdgeTag = 
    abstract Id: Guid
    abstract Parent: IPolicyEdgeTag
    abstract Load: float

type IPolicy = 
    inherit IGraph<IPolicyVertex, IPolicyEdgeTag>

type IPlan = 
    inherit IGraph<IPlanVertex, IPlanEdgeTag>
    abstract FindInstanceFromPolicy: IPolicyVertex -> IList<IPlanVertex>

type IVisualize = 
    abstract Visualize: unit -> string