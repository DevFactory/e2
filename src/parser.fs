module Main
open FParsec

type E2 = 
    | E2Type of string
    | E2Name of string
    | E2Attr of string

let test p str = 
    match run p str with 
    | Success(result, _, _) -> printfn "Success: %A" result
    | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg

let ptype = 
    let opts = IdentifierOptions(isAsciiIdStart = isAsciiUpper)
    identifier opts .>> spaces

let pname = 
    let opts = IdentifierOptions()
    identifier opts .>> spaces


[<EntryPoint>]
let main args = 
    test ptype "Firewall"
    test ptype "firewall"
    test pname "firewall"
    0
