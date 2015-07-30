module E2.IGraph

open System
open System.Collections.Generic

type IEdge<'V, 'Tag> = 
    abstract source: 'V
    abstract target: 'V
    abstract tag: 'Tag

type IGraph<'V, 'Tag> = 
    abstract Vertices: IEnumerable<'V>
    abstract Edges: IEnumerable<IEdge<'V, 'Tag>>
    
    abstract AddVertex: 'V -> bool
    abstract AddEdge: IEdge<'V, 'Tag> -> bool
    
    abstract InEdges: 'V -> IEnumerable<IEdge<'V, 'Tag>>
    abstract OutEdges: 'V -> IEnumerable<IEdge<'V, 'Tag>>

type IPolicyVertex = 
    abstract name: string
    abstract t: string
    abstract unit_core: float

type IPolicyEdgeTag = 
    abstract filter: string
    abstract attribute: string
    abstract pipelet_id: int

type IPlanVertex = 
    abstract id: Guid
    abstract parent: IPolicyVertex

type IPlanEdgeTag = 
    abstract id: Guid
    abstract parent: IPolicyEdgeTag
    abstract load: float

type IPolicy = 
    inherit IGraph<IPolicyVertex, IPolicyEdgeTag>

type IPlan = 
    inherit IGraph<IPlanVertex, IPlanEdgeTag>
    abstract member instance_table: IDictionary<IPolicyVertex, IList<IPlanVertex>>

type IVisualize = 
    abstract Visualize: unit -> string