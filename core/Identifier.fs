module Identifier

let internalId = ref 0

let GetId () = 
    System.Threading.Interlocked.Increment internalId


