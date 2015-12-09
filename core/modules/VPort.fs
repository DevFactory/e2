namespace Module

open Graph

type VPort(nf : Instance) = 
    inherit Module()
    member val NF = nf
