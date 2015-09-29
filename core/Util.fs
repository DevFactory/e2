namespace E2

open System.Collections.Generic

type MovingMaximum<'T when 'T : comparison> (size : int) = 
    let q = Queue<'T>()

    member this.Enqueue (e : 'T) =
        if q.Count = size then
            q.Dequeue() |> ignore
        q.Enqueue(e)

    member this.FindMax () =
        if q.Count > 0 then Seq.max q else Unchecked.defaultof<'T>