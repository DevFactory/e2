module Parser

open FParsec
open System
open System.Collections.Generic

let ptype = 
    let opts = IdentifierOptions(isAsciiIdStart = isAsciiUpper)
    identifier opts .>> spaces

let pvar = 
    let opts = IdentifierOptions()
    identifier opts .>> spaces

let pnode = 
    ptype .>>. pvar .>> pchar ';' .>> spaces 

let pnodedecl = 
    pnode |>> Ast.NodeDecl

let pexpr = 
    let str s = pstring s
    let normalCharSnippet = manySatisfy (fun c -> c <> '\\' && c <> '"')
    let escapedChar = str "\\" >>. (anyOf "\\\"nrt" |>> function
                                                        | 'n' -> "\n"
                                                        | 'r' -> "\r"
                                                        | 't' -> "\t"
                                                        | c   -> string c)
    between (str "\"") (str "\"")
            (stringsSepBy normalCharSnippet escapedChar)

let pedge =
    let attr = (between (pchar '[') (pchar ']') pexpr .>> spaces) <|>% "true"
    let arrow = pstring "->" .>> spaces
    pipe5 pvar attr arrow attr pvar (fun a b c d e -> (a, e, b, d)) .>> pchar ';' .>> spaces

let psubgraph = 
    between (pstring "TC {" .>> spaces) (pstring "}" .>> spaces) 
            (many pedge) .>> spaces

let ptrafficclass = 
    psubgraph |>> Ast.TraffcClass

let ptoplevel = 
    spaces >>. many (ptrafficclass <|> pnodedecl)

let test p str = 
    match run p str with 
    | Success(result, _, _) -> printfn "Success: %A" result
    | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg

let example_code = """
Proxy proxy;
NAT nat;
Firewall fw;
Ports cp;
Ports ep;

TC {
    cp["dst port 80"] -> proxy;
    cp["!(dst port 80)"] -> nat;
    proxy -> nat;
    nat -> fw;
    fw["fw safe"] -> ep;
}


TC {
    ep -> nat;
    nat["src port 80"] -> proxy;
    nat["!(src port 80)"] -> fw;
    proxy -> fw;
    fw["fw safe"] -> cp;
}
"""

type ParseState = {
    V : Map<string, string>;
    E : Ast.Edge list list;
}

let ParseTopLevels (lst: Ast.TopLevel list) =
    let state = { V = Map.empty; E = [] }
    let handleTopLevel s toplevel =
        match toplevel with
        | Ast.NodeDecl (t, v) ->
            match Map.tryFind v s.V with
            | Some _ -> failwith ("Redefined NF: " + v + ", Type: " + t)
            | None -> { V = Map.add v t s.V; E = s.E }
        | Ast.TraffcClass g ->
            { V = s.V; E = g :: s.E}
    List.fold handleTopLevel state lst

[<EntryPoint>]
let main args = 
    match run ptoplevel example_code with
    | Success(result, _, _) -> printfn "%A" (ParseTopLevels result)
    | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg
    0