﻿namespace E2

type Planner = 
    
    static member InitialPlan (policy : IPolicy) = 
        let p = new Plan()
        p.FromPolicyGraph(policy)
        p :> IPlan

    static member ScaleUpPlanVertex (v : IPlanVertex) (policy : IPolicy) (plan: IPlan) =
        let pv = v.Parent
        let replica = PlanVertex(pv)
        for e in plan.InEdges v do
            let source = e.Source
            let tag' = new PlanEdgeTag(e.Tag.Parent)
            let e' = new Edge<IPlanVertex, IPlanEdgeTag>(source, replica, tag')
            plan.AddEdge(e') |> ignore
        for e in plan.OutEdges v do
            let target = e.Target
            let tag' = new PlanEdgeTag(e.Tag.Parent)
            let e' = new Edge<IPlanVertex, IPlanEdgeTag>(replica, target, tag')
            plan.AddEdge(e') |> ignore
        replica

    static member ScaleDownPlanVertex (v : IPlanVertex) (policy : IPolicy) (plan: IPlan) =
        plan.RemoveVertex(v)

    static member Scale (policy : IPolicy) (plan : IPlan) = 
        let PlanVertexLoad v = 
            plan.InEdges v
            |> Seq.map (fun e -> e.Tag.PacketsPerSecond)
                            |> Seq.sum

        let PolicyVertexLoads pv = 
            plan.FindPlanVertices pv
            |> Seq.map (fun v -> PlanVertexLoad v)
                                            |> Seq.sum

        let Scale pv = 
            let totalLoad = PolicyVertexLoads pv
                
            let replicaNumIdeal = 
                totalLoad * pv.CyclesPerPacket / (2.6e+9)
                |> ceil
                |> int
                
            let replicaNum = (plan.FindPlanVertices pv).Count
                
            let prevPlanVerticesWithPolicyTag = 
                policy.Edges
                |> Seq.filter (fun e -> e.Target = pv)
                |> Seq.map (fun e -> (e.Source, e.Tag))
                |> Seq.map (fun (pv, ptag) -> plan.FindPlanVertices pv |> Seq.map (fun v -> (v, ptag)))
                |> Seq.concat

            let nextPlanVerticesWithPolicyTag = 
                policy.Edges
                |> Seq.filter (fun e -> e.Source = pv)
                |> Seq.map (fun e -> (e.Target, e.Tag))
                |> Seq.map (fun (pv, ptag) -> plan.FindPlanVertices pv |> Seq.map (fun v -> (v, ptag)))
                |> Seq.concat

            let AddEdge v1 v2 ptag =
                let tag = new PlanEdgeTag(ptag)
                let e = new Edge<IPlanVertex, IPlanEdgeTag>(v1, v2, tag)
                plan.AddEdge(e)
                
            // Scale up or down
            if replicaNumIdeal > replicaNum then 
                for i = 0 to replicaNumIdeal - replicaNum - 1 do
                    let replica = PlanVertex(pv)
                    plan.AddVertex replica |> ignore
                    prevPlanVerticesWithPolicyTag |> Seq.iter (fun (v, ptag) -> AddEdge v replica ptag |> ignore)
                    nextPlanVerticesWithPolicyTag |> Seq.iter (fun (v, ptag) -> AddEdge replica v ptag |> ignore)
            else if replicaNumIdeal < replicaNum / 2 then
                let vs = plan.FindPlanVertices pv
                let redundant = vs |> Seq.take (replicaNum - replicaNumIdeal)
                redundant |> Seq.iter (fun v -> v.State <- Deleted)

            // Manually rebalance load value to avoid repeated scaling.
            // Ideally we should not do this, because it will be updated via notification.
//            let BalancePolicyEdge(pe : IEdge<IPolicyVertex, IPolicyEdgeTag>) = 
//                let totalLoad = 
//                    plan.FindPlanEdgeTags(pe.Tag)
//                    |> Seq.map (fun tag -> tag.PacketsPerSecond)
//                    |> Seq.sum
//                    
//                let n = (plan.FindPlanVertices pe.Source).Count * (plan.FindPlanVertices pe.Target).Count
//                let load = totalLoad / (float n)
//                plan.FindPlanEdgeTags(pe.Tag) |> Seq.iter (fun tag -> tag.PacketsPerSecond <- load)
//                
//            // First rebalance loads coming from each of previous edges
//            let prevPolicyEdges = policy.Edges |> Seq.filter (fun e -> e.Target = pv)
//            prevPolicyEdges |> Seq.iter BalancePolicyEdge
//            // Then rebalance loads to each of next edges
//            let nextPolicyEdges = policy.Edges |> Seq.filter (fun e -> e.Source = pv)
//            nextPolicyEdges |> Seq.iter BalancePolicyEdge
            
        policy.Vertices |> Seq.iter Scale