namespace E2

type Planner = 
    
    static member InitialPlan(policy : IPolicy) = 
        let p = new Plan()
        p.FromPolicyGraph(policy)
        p :> IPlan
    
    static member Scale (policy : IPolicy) (plan : IPlan) = 
        let PlanVertexLoad v = 
            plan.InEdges v
            |> Seq.map (fun e -> e.Tag.Load)
            |> Seq.sum
        
        let PolicyVertexLoads pv = 
            plan.FindInstanceFromPolicy pv
            |> Seq.map (fun v -> PlanVertexLoad v)
            |> Seq.sum
        
        let NumReplicas pv = (plan.FindInstanceFromPolicy pv).Count
        
        let Scale pv = 
            let loads = PolicyVertexLoads pv
            let n = NumReplicas pv
            
            let prevPlanVertices = 
                policy.Edges
                |> Seq.filter (fun e -> e.Target = pv)
                |> Seq.map (fun e -> (e.Source, e.Tag))
                |> Seq.map (fun (pv, ptag) -> plan.FindInstanceFromPolicy pv |> Seq.map (fun v -> (v, ptag)))
                |> Seq.concat
            
            let nextPlanVertices = 
                policy.Edges
                |> Seq.filter (fun e -> e.Source = pv)
                |> Seq.map (fun e -> (e.Target, e.Tag))
                |> Seq.map (fun (pv, ptag) -> plan.FindInstanceFromPolicy pv |> Seq.map (fun v -> (v, ptag)))
                |> Seq.concat
            
            let AddEdge v1 v2 ptag = 
                let tag = new PlanEdgeTag(ptag)
                let e = new Edge<IPlanVertex, IPlanEdgeTag>(v1, v2, tag)
                plan.AddEdge(e)
            
            // TODO: Reset loads after scaling.
            let desired = int (ceil loads)
            if desired - n > 0 then 
                for i = 0 to desired - n - 1 do
                    let replica = PlanVertex(pv)
                    plan.AddVertex replica |> ignore
                    prevPlanVertices |> Seq.iter (fun (v, ptag) -> AddEdge v replica ptag |> ignore)
                    nextPlanVertices |> Seq.iter (fun (v, ptag) -> AddEdge replica v ptag |> ignore)
        policy.Vertices |> Seq.iter Scale
