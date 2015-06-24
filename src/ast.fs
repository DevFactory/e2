module Ast

type Type = string

and Variable = string

and Node = Type * Variable

and Expr = string (* TODO: Parse BPF later *)

and Edge = Variable * Variable * Expr * Expr

and SubGraph = Edge list

and Toplevel = 
    | NodeDecl of Node
    | TraffcClass of SubGraph
