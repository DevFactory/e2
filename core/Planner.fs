namespace E2

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

    static member ScaleDownPlanVertex (v : IPlanVertex) (policy : IPolicy) (plan: IPlan) =
        plan.RemoveVertex(v)

    static member Scale (policy : IPolicy) (plan : IPlan) = 
        let log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)
        let PolicyVertexLoads pv = 
            plan.FindPlanVertices pv
            |> Seq.map (fun v -> v.AggregatePacketsPerSeconds.FindMax())
            |> Seq.sum

        let Scale (pv : IPolicyVertex) = 
            let totalLoad = PolicyVertexLoads pv
                
            let replicaNumIdeal = 
                let ideal = totalLoad * pv.CyclesPerPacket / (2.6e+9 * 0.9) |> ceil
                if ideal < 1.0 then 1 else int ideal
            let replicaNum = (plan.FindPlanVertices pv).Count

            log.DebugFormat("Scaling policy vertex {0}. PPS: {1}. Ideal #: {2}. Current #: {3}", 
                pv.Name, totalLoad, replicaNumIdeal, replicaNum)
                
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
            if replicaNumIdeal - replicaNum > 0 then 
                for i = 0 to replicaNumIdeal - replicaNum (* intentionally +1 *) do
                    let replica = PlanVertex(pv)
                    plan.AddVertex replica |> ignore
                    prevPlanVerticesWithPolicyTag |> Seq.iter (fun (v, ptag) -> AddEdge v replica ptag |> ignore)
                    nextPlanVerticesWithPolicyTag |> Seq.iter (fun (v, ptag) -> AddEdge replica v ptag |> ignore)
            else if replicaNumIdeal - replicaNum < -2 then
                let vs = plan.FindPlanVertices pv
                let redundant = vs |> Seq.sortByDescending(fun v -> v.Id) |> Seq.take (replicaNum - replicaNumIdeal)
                redundant |> Seq.iter (fun v -> v.State <- Obsolete)

        policy.Vertices |> Seq.iter Scale
