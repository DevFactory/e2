module Parser

open FParsec
open System
open System.Collections.Generic

let ptype = 
    let opts = IdentifierOptions()
    identifier opts .>> spaces

let pvar = 
    let opts = IdentifierOptions()
    identifier opts .>> spaces

let pnode = ptype .>>. pvar .>> pchar ';' .>> spaces
let pnodedecl = pnode |>> Ast.NodeDecl

let pexpr = 
    let str s = pstring s
    let normalCharSnippet = manySatisfy (fun c -> c <> '\\' && c <> '"')
    
    let escapedChar = 
        str "\\" >>. (anyOf "\\\"nrt" |>> function 
                      | 'n' -> "\n"
                      | 'r' -> "\r"
                      | 't' -> "\t"
                      | c -> string c)
    between (str "\"") (str "\"") (stringsSepBy normalCharSnippet escapedChar)

let pedge = 
    let attr = (between (pchar '[') (pchar ']') pexpr .>> spaces) <|>% "true"
    let arrow = pstring "->" .>> spaces
    pipe5 pvar attr arrow attr pvar (fun a b c d e -> (a, e, b, d)) .>> pchar ';' .>> spaces

let psubgraph = between (pstring "TC {" .>> spaces) (pstring "}" .>> spaces) (many pedge) .>> spaces
let ptrafficclass = psubgraph |>> Ast.TraffcClass
let ptoplevel = spaces >>. many (ptrafficclass <|> pnodedecl)

let test p str fn = 
    match run p str with
    | Success(result, _, _) -> fn result
    | Failure(errorMsg, _, _) -> failwith (sprintf "Failure: %s" errorMsg)

type ParseState = 
    { V : Map<string, string>
      E : (string * string * string * string) list list }

let ParseTopLevels(lst : Ast.TopLevel list) = 
    let state = 
        { V = Map.empty
          E = [] }
    
    let handleTopLevel s toplevel = 
        match toplevel with
        | Ast.NodeDecl(t, v) -> 
            match Map.tryFind v s.V with
            | Some _ -> failwith ("Redefined NF: " + v + ", Type: " + t)
            | None -> 
                { V = Map.add v t s.V
                  E = s.E }
        | Ast.TraffcClass g -> 
            { V = s.V
              E = g :: s.E }
    
    List.fold handleTopLevel state lst

let Parse str = test ptoplevel str ParseTopLevels
