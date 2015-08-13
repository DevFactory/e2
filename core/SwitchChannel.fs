namespace E2

open CookComputing.XmlRpc
open System.Net

type ExpressionId = int
type RuleId = int
type ActionId = int
type Priority = int

[<Struct>]
type Table =
    val mutable id : int

type ISwitchNorthbound = 
    [<XmlRpcMethod("nb.ACLExpressions.getFirst")>]
    abstract ACLExpressionsGetFirst : unit -> int64

    [<XmlRpcMethod("nb.ACLExpressions.addRow")>]
    abstract ACLExpressionsAddRow : ExpressionId * string * string * string -> int

    [<XmlRpcMethod("nb.ACLExpressions.find")>]
    abstract ACLExpressionsFind : ExpressionId -> int

    [<XmlRpcMethod("nb.ACLExpressions.delRow")>]
    abstract ACLExpressionsDelRow : int64 -> int

    [<XmlRpcMethod("nb.ACLActions.getFirst")>]
    abstract ACLActionsGetFirst : unit -> int64

    [<XmlRpcMethod("nb.ACLActions.addRow")>]
    abstract ACLActionsAddRow : ActionId * string * string -> int

    [<XmlRpcMethod("nb.ACLActions.find")>]
    abstract ACLActionsFind : ActionId -> int

    [<XmlRpcMethod("nb.ACLActions.delRow")>]
    abstract ACLActionsDelRow : int64 -> int

    [<XmlRpcMethod("nb.ACLRules.getFirst")>]
    abstract ACLRulesGetFirst : unit -> int64

    [<XmlRpcMethod("nb.ACLRules.addRow")>]
    abstract ACLRulesAddRow : RuleId * ExpressionId * ActionId * string * string * Priority -> int

    [<XmlRpcMethod("nb.ACLRules.find")>]
    abstract ACLRulesFind : RuleId -> int

    [<XmlRpcMethod("nb.ACLRules.delRow")>]
    abstract ACLRulesDelRow : int64 -> int

type SwitchChannel (endpoint : IPEndPoint) = 
    let proxy = XmlRpcProxyGen.Create<ISwitchNorthbound>()
    do (proxy :?> IXmlRpcProxy).Url <- "http://" + endpoint.ToString() + "/RPC2"

    member this.Agent = proxy
    member this.CleanTables () = 
        let HandleResponse code =
            match code with
            | 0 -> ()
            | x -> failwith (sprintf "Error code: %d" x)

        let agent = this.Agent

        let rec TryClear f1 f2 =
            match f1 () with
            | -1L -> ()
            | x -> f2 x |> HandleResponse; TryClear f1 f2

        TryClear agent.ACLRulesGetFirst agent.ACLRulesDelRow
        TryClear agent.ACLActionsGetFirst agent.ACLActionsDelRow
        TryClear agent.ACLExpressionsGetFirst agent.ACLExpressionsDelRow