namespace E2

open System.Collections.Generic

type IPlacement = 
    abstract Initial: IPlan -> IList<IServer> -> IDictionary<IPlanVertex, IServer>
    abstract Incremental: IPlan -> IList<IServer> -> IDictionary<IPlanVertex, IServer> -> IDictionary<IPlanVertex, IServer>

