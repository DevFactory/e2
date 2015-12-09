module Placement

open System.Collections.Generic
open Graph
open Host

/// Places instances in the graph `g` onto a list of hosts.
let Place (g: Graph) (hlist: List<Host>) =
    let instances = g.NodeInstances |> Seq.filter (fun i -> i.Status = Unassigned)
    let instance_edges = g.EdgeInstances

    let discovered = HashSet<Instance>()
    let reordered_instances = List<Instance>()

    while Seq.exists (discovered.Contains >> not) instances do
        let head = instances |> Seq.filter (discovered.Contains >> not) |> Seq.head
        let q = Queue<Instance>()

        q.Enqueue(head)
        discovered.Add(head) |> ignore

        // Process the connected component starting at head
        while q.Count <> 0 do
            let current = q.Dequeue()
            reordered_instances.Add(current)

            let out_neighbors = instance_edges |> Seq.filter (fun e -> e.Source = current) |> Seq.map (fun e -> e.Target)
            let in_neighbors = instance_edges |> Seq.filter (fun e -> e.Target = current) |> Seq.map (fun e -> e.Source)

            let neighbors = (Seq.append out_neighbors in_neighbors) |> Seq.filter (discovered.Contains >> not) |> Seq.distinct

            neighbors |> Seq.iter (q.Enqueue)
            neighbors |> Seq.iter (discovered.Add >> ignore)

    let reordered_hosts = hlist |> Seq.map (fun h -> Seq.replicate h.FreeCores h) |> Seq.concat

    let total_cores = Seq.length reordered_hosts
    let total_instances = Seq.length reordered_instances
    if total_cores < total_instances then
        failwith "Not enough cores for allocation."

    Seq.iter2 (fun (i: Instance) (h: Host) -> h.VFI.Add(i)) reordered_instances (Seq.take total_instances reordered_hosts)
    instances |> Seq.iter (fun i -> i.Status <- Assigned)

let IncrementalPlace (g: Graph) (hlist: List<Host>) =
    let instances = g.NodeInstances |> Seq.filter (fun i -> i.Status = Unassigned)
    let instance_edges = g.EdgeInstances

    let rate (i: Instance) (h: Host) =
        let out_rate =
            instance_edges
            |> Seq.filter (fun e -> e.Source = i && e.Target.Status <> Unassigned && not (h.VFI.Contains(e.Target)))
            |> Seq.sumBy (fun e -> e.Rate)
        let in_rate =
            instance_edges
            |> Seq.filter (fun e -> e.Target = i && e.Source.Status <> Unassigned && not (h.VFI.Contains(e.Source)))
            |> Seq.sumBy (fun e -> e.Rate)
        out_rate + in_rate

    let place (i: Instance) =
        let h = hlist |> Seq.minBy (fun h -> rate i h)
        h.VFI.Add(i)
        i.Status <- Assigned

    instances |> Seq.iter place
