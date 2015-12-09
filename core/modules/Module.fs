namespace Module

open System.Collections.Generic

type Module() = 
    member val Id = Identifier.GetId()
    member val NextModules = List<Module>()
    member val IsCreated = false with get, set