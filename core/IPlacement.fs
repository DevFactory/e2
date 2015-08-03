namespace E2

open System.Collections.Generic

type IPlacement = 
    abstract Place: IPlan -> IList<IServer> -> IDictionary<IPlanVertex, IServer>

