namespace E2

open System.Collections.Generic

type Placement() = 
    member private this.PlaceRandom (plan: IPlan) (servers: IList<IServer>) = 
        let rand = new System.Random()
        let n = servers.Count
        let dict = new Dictionary<IPlanVertex, IServer>()
        plan.Vertices |> Seq.iter (fun v -> let k = rand.Next(n) in dict.Add(v, servers.[k]))
        dict :> IDictionary<IPlanVertex, IServer>
        
    interface IPlacement with
        member this.Place (plan: IPlan) (servers: IList<IServer>) = 
            this.PlaceRandom plan servers