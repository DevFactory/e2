namespace E2

open CookComputing.XmlRpc
open System.Net

type ISwitchNorthbound = 
    [<XmlRpcMethod("nb.Table.getTable")>]
    abstract GetTable : unit -> XmlRpcStruct

type SwitchChannel (endpoint : IPEndPoint) = 
    let proxy = XmlRpcProxyGen.Create<ISwitchNorthbound>()
    do (proxy :?> IXmlRpcProxy).Url <- "http://" + endpoint.ToString()

    member this.Agent = proxy