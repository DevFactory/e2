module E2.Scale

open Graph

let UpscaleThreshold = 1
let DownscaleThreshold = -5

/// Scale the graph according to the rate numbers of instance edges
/// Compute the optimal number of NF instances. 
/// If current number exceeds the optimal, mark excess instances as "Garbage".
/// They will be recycled later.
/// Otherwise, add more instances.
let Scale (g: Graph) = 
    let scaleNode (node : Node) = 
        let in_edges = g.InEdge node
        let edges = if Seq.isEmpty in_edges then g.OutEdge node else in_edges
        let accum_rate = 
            edges 
            |> Seq.map (fun e -> e.Instances) 
            |> Seq.concat 
            |> Seq.sumBy (fun e -> e.Rate)

        let ideal_count = int (ceil (accum_rate / node.MaxRatePerCore))
        let current_count = Seq.length node.Instances
        let gap = ideal_count - current_count

        if gap >= UpscaleThreshold then
            for i in 1..gap do 
                node.Instances.Add(Instance(Unassigned))
        else if gap <= DownscaleThreshold then
            for i in 1..(-gap) do 
                node.Instances.[current_count - i].Status <- Garbage
        g.UpdateInstanceEdges()

    g.Nodes |> Seq.iter scaleNode
