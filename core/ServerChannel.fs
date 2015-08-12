namespace E2

open CookComputing.XmlRpc
open System.Net

type Cores = int array

type NFType = string

type NFName = string

type ModuleType = string

type ModuleName = string

type ModuleConfig = string

type GateIndex = int

[<Struct>]
type Response =
    val mutable code : int
    val mutable msg : string
    [<XmlRpcMissingMapping(MappingAction.Ignore)>]
    val mutable result : XmlRpcStruct

type IServerAgent = 
    
    [<XmlRpcMethod("reset")>]
    abstract ResetSoftNIC : unit -> unit

    [<XmlRpcMethod("launch_sn")>]
    abstract LaunchSoftNIC : Cores -> Response
    
    [<XmlRpcMethod("stop_sn")>]
    abstract StopSoftNIC : unit -> Response
    
    [<XmlRpcMethod("launch_nf")>]
    abstract LaunchNF : Cores * NFType * ModuleName * NFName -> Response
    
    [<XmlRpcMethod("stop_nf")>]
    abstract StopNF : NFName -> Response
    
    [<XmlRpcMethod("create_pport")>]
    abstract CreatePPort : ModuleName -> Response
    
    [<XmlRpcMethod("create_vport")>]
    abstract CreateVPort : ModuleName -> Response
    
    [<XmlRpcMethod("create_module")>]
    abstract CreateModule : ModuleType * ModuleName -> Response
    
    [<XmlRpcMethod("remove_module")>]
    abstract RemoveModule : ModuleName -> Response
    
    [<XmlRpcMethod("connect_module")>]
    abstract ConnectModule : ModuleName * GateIndex * ModuleName -> Response
    
    [<XmlRpcMethod("disconnect_module")>]
    abstract DisconnectModule : ModuleName * GateIndex -> Response
    
    [<XmlRpcMethod("configure_module")>]
    abstract ConfigureModule : ModuleName * ModuleConfig -> Response
    
    [<XmlRpcMethod("query_vport_stats")>]
    abstract QueryVPortStats : ModuleName -> Response

    [<XmlRpcMethod("query_pport_stats")>]
    abstract QueryPPortStats : ModuleName -> Response

type ServerChannel (endpoint : IPEndPoint) = 
    let proxy = XmlRpcProxyGen.Create<IServerAgent>()
    do printfn "Connecting to server: %A" endpoint
    do (proxy :?> IXmlRpcProxy).Url <- "http://" + endpoint.ToString()

    member this.Agent = proxy
