#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.FscHelper

let buildDir = "./build/"

Target "clean" (fun _ ->
    CleanDirs [ buildDir ]
)

Target "all" (fun _ ->
    ["src/parser.fs"]
    |> Fsc (fun p -> 
        { p with Output = buildDir + "Main.exe"
                 References = 
                    [ "packages/FParsec/lib/net40-client/FParsec.dll"
                      "packages/FParsec/lib/net40-client/FParsecCS.dll" ] 
                 OtherParams = 
                    [ "--standalone"] })
)

"clean"
    ==> "all"

RunTargetOrDefault "all"