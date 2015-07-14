module E2.ResourceManager

type Host(id: int, cores: double) = 
    member val id = id
    member val cores = cores with get, set
    
    override this.Equals(obj) = 
        match obj with
        | :? Host as o -> this.id.Equals(o.id)
        | _ -> false

    override this.GetHashCode() = hash this.id

type Manager() = 
    member val hosts = [] with get, set
    member this.AddHost(host: Host) = 
        this.hosts <- host :: this.hosts