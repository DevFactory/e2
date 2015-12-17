module Graph

open System
open System.Collections.Generic
open System.Net.NetworkInformation
open Parser

/// The possible status of an instance
type InstanceStatus =
    | Unassigned
    | Assigned
    | Running
    | Garbage

type Instance(status: InstanceStatus) =
    member val Id = Identifier.GetId()
    member val Status = status with get, set
    member this.GetAddress () =
        PhysicalAddress.Parse("06" + this.Id.ToString("X10"))

/// Edge that connects two instances
type InstanceEdge(source: Instance, target: Instance, rate: float) =
    member val Source = source
    member val Target = target
    member val Rate = rate with get, set

type Node(nodeType: string, name: string) =
    member val Type = nodeType
    member val Name = name
    member val Instances = List<Instance>()
    member this.MaxRatePerCore =
        match nodeType with
        | _ -> 10.0

type NodeEdge(source: Node, target: Node, pipelet: int) =
    let instances = List<InstanceEdge>()
    member val Source = source
    member val Target = target
    member val Pipelet = pipelet
    member this.Instances =
        instances
    member this.Rate () =
        this.Instances |> Seq.sumBy (fun e -> e.Rate)
    member this.UpdateInstances () =
        // Remove unused edges
        let obsoleteFilter =
            fun (ie: InstanceEdge) -> not (Seq.contains ie.Source source.Instances && Seq.contains ie.Target target.Instances)
        let obsoletePredicate = new Predicate<InstanceEdge>(obsoleteFilter)
        instances.RemoveAll obsoletePredicate |> ignore
        // Connect new node instances
        for s in source.Instances do
            for t in target.Instances do
                let exists = Seq.exists (fun (ie: InstanceEdge) -> ie.Source = s && ie.Target = t) instances
                if not exists then instances.Add(InstanceEdge(s, t, 0.0))

/// Graph is a collection of nodes and edges
type Graph() =
    let nodes = List<Node>()
    let edges = List<NodeEdge>()

    member this.Nodes = nodes
    member this.Edges = edges
    member this.NodeInstances = nodes |> Seq.map (fun n -> n.Instances) |> Seq.concat
    member this.EdgeInstances = edges |> Seq.map (fun n -> n.Instances) |> Seq.concat

    member this.UpdateInstanceEdges() =
        edges |> Seq.iter (fun e -> e.UpdateInstances())

    member this.LoadFromParseState (state: Parser.ParseState) =
        nodes.Clear()
        nodes.AddRange(state.V |> Map.toSeq |> Seq.map (fun (name, t) -> Node(t, name)))
        edges.Clear()
        let makeNodeEdgeList pipelet lst =
            lst |> Seq.map (fun (v1, v2, e1, e2) ->
                let v1' = nodes |> Seq.filter(fun n -> v1 = n.Name) |> Seq.exactlyOne
                let v2' = nodes |> Seq.filter(fun n -> v2 = n.Name) |> Seq.exactlyOne
                NodeEdge(v1', v2', pipelet))
        edges.AddRange(state.E |> Seq.mapi makeNodeEdgeList |> Seq.concat)
        // init instances
        nodes |> Seq.iter (fun n -> n.Instances.Add(Instance(Unassigned)))
        this.UpdateInstanceEdges()

    member this.InEdge (node: Node) =
        assert nodes.Contains(node)
        edges |> Seq.filter (fun e -> e.Target = node)

    member this.OutEdge (node: Node) =
        assert nodes.Contains(node)
        edges |> Seq.filter (fun e -> e.Source = node)

    member this.InInstanceEdge (instance: Instance) = 
        assert Seq.contains instance this.NodeInstances
        this.EdgeInstances |> Seq.filter (fun e -> e.Target = instance)

    member this.OutInstanceEdge (instance: Instance) = 
        assert Seq.contains instance this.NodeInstances
        this.EdgeInstances |> Seq.filter (fun e -> e.Source = instance)

