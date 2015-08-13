namespace E2

open CookComputing.XmlRpc
open System.Net

type ExpressionId = int
type RuleId = int
type ActionId = int
type Priority = int

type ISwitchNorthbound = 
    [<XmlRpcMethod("nb.ACLExpressions.addRow")>]
    abstract ACLExpressionsAddRow : ExpressionId * string * string * string -> int

    [<XmlRpcMethod("nb.ACLExpressions.find")>]
    abstract ACLExpressionsFind : ExpressionId -> int

    [<XmlRpcMethod("nb.ACLExpressions.delRow")>]
    abstract ACLExpressionsDelRow : int -> int

    [<XmlRpcMethod("nb.ACLActions.addRow")>]
    abstract ACLActionsAddRow : ActionId * string * string -> int

    [<XmlRpcMethod("nb.ACLActions.find")>]
    abstract ACLActionsFind : ActionId -> int

    [<XmlRpcMethod("nb.ACLActions.delRow")>]
    abstract ACLActionsDelRow : int -> int

    [<XmlRpcMethod("nb.ACLRules.addRow")>]
    abstract ACLRulesAddRow : RuleId * ExpressionId * ActionId * string * string * Priority -> int

    [<XmlRpcMethod("nb.ACLRules.find")>]
    abstract ACLRulesFind : RuleId -> int

    [<XmlRpcMethod("nb.ACLRules.delRow")>]
    abstract ACLRulesDelRow : int -> int

type SwitchChannel (endpoint : IPEndPoint) = 
    let proxy = XmlRpcProxyGen.Create<ISwitchNorthbound>()
    do (proxy :?> IXmlRpcProxy).Url <- "http://" + endpoint.ToString()

    member this.Agent = proxy