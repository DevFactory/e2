module TestGraph

open NUnit.Framework
open Graph
open Scale

[<Test>]
let Empty () =
    let g = Graph()
    Assert.IsTrue(g.Nodes |> Seq.isEmpty)
    Assert.IsTrue(g.Edges |> Seq.isEmpty)
    Assert.IsTrue(g.NodeInstances |> Seq.isEmpty)
    Assert.IsTrue(g.NodeInstances |> Seq.isEmpty)

[<Test>]
let ScaleSingleNode () =
    let g = Graph()
    g.Nodes.Add(Node("invalid", "invalid"))
    Assert.IsTrue(g.Nodes |> Seq.length = 1)
    Assert.IsTrue(g.NodeInstances |> Seq.isEmpty)
    Scale(g)
    Assert.IsTrue(g.Nodes |> Seq.length = 1)
    Assert.IsTrue(g.NodeInstances |> Seq.length = 1)
