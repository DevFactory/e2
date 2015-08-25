namespace E2

open CookComputing.XmlRpc
open System.Net
open System

type Cores = int array

type NFType = string

type NFName = string

type ModuleType = string

type ModuleName = string

type ModuleConfig = string

type GateIndex = int

[<Struct>]
type PortStat = 
    val mutable port : string
    val mutable out_pps : int
    val mutable inc_pps : int
    val mutable out_qlen : int
    val mutable out_bps : int
    val mutable inc_bps : int

[<Struct>]
type Response =
    val mutable code : int
    val mutable msg : string
    [<XmlRpcMissingMapping(MappingAction.Ignore)>]
    val mutable result : PortStat array

type IServerAgent = 
    
    [<XmlRpcMethod("reset")>]
    abstract ResetSoftNIC : unit -> Response

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

    [<XmlRpcMethod("create_module_with_arg")>]
    abstract CreateModuleEx : ModuleType * ModuleName * Object * bool -> Response
    
    [<XmlRpcMethod("remove_module")>]
    abstract RemoveModule : ModuleName -> Response
    
    [<XmlRpcMethod("connect_module")>]
    abstract ConnectModule : ModuleName * GateIndex * ModuleName -> Response
    
    [<XmlRpcMethod("disconnect_module")>]
    abstract DisconnectModule : ModuleName * GateIndex -> Response
    
    [<XmlRpcMethod("configure_module")>]
    abstract ConfigureModule : ModuleName * ModuleConfig -> Response
    
    [<XmlRpcMethod("query_vport_stats")>]
    abstract QueryVPortStats : unit -> Response

    [<XmlRpcMethod("query_pport_stats")>]
    abstract QueryPPortStats : ModuleName -> Response

    [<XmlRpcMethod("query_module")>]
    abstract QueryModule : ModuleName * Object -> Response

type ServerChannel (addr : string, port : int) = 
    let proxy = XmlRpcProxyGen.Create<IServerAgent>()
    do printfn "Connect to server: %A" addr
    do (proxy :?> IXmlRpcProxy).Url <- "http://" + addr + ":" + port.ToString()

    member this.Agent = proxy
