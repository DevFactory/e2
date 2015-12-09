namespace Module

open System.Collections.Generic

type Classifier() = 
    inherit Module()
    member val Filters = List<string>()
