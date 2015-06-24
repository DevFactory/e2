module Parser

open FParsec

let test p str = 
    match run p str with 
    | Success(result, _, _) -> printfn "Success: %A" result
    | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg

let ptype = 
    let opts = IdentifierOptions(isAsciiIdStart = isAsciiUpper)
    identifier opts .>> spaces

let pvar = 
    let opts = IdentifierOptions()
    identifier opts .>> spaces

let pnode = 
    ptype .>>. pvar .>> pchar ';' .>> spaces 

let pnode_decl = 
    pnode |>> (fun node -> Ast.NodeDecl(node))

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

[<EntryPoint>]
let main args = 
    test ptype "Firewall  "
    test ptype "firewall "
    test pvar "firewall "
    test pnode_decl "Firewall fw   ;  "
    test pedge "fw[\"safe\"] -> nat; "
    0