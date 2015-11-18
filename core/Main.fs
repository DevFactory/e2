module E2.Main

open System
open System.Collections.Generic
open CookComputing.XmlRpc
open log4net

let example = """
vpn vpn0;
nat nat0;
firewall fw0;
ids ids0;

TC {
    fw0 -> ids0;
    fw0 -> nat0;
    ids0 -> nat0;
    nat0 -> vpn0;
}

"""

let LogConfig () =
    let layout = Layout.PatternLayout(@"%date %-5level %logger: %message%newline")
    let console = Appender.ConsoleAppender()
    console.Layout <- layout
    console.Threshold <- Core.Level.Info
    
    let file = Appender.RollingFileAppender()
    file.Layout <- layout
    file.Threshold <- Core.Level.Debug
    file.File <- @"C:\Trace.log"
    file.RollingStyle <- Appender.RollingFileAppender.RollingMode.Composite
    file.MaxFileSize <- 100000000L
    file.StaticLogFileName <- true
    file.MaxSizeRollBackups <- 10
    file.AppendToFile <- true
    file.ActivateOptions()
    
    Config.BasicConfigurator.Configure(file, console) |> ignore

[<EntryPoint>]
let main args = 
    LogConfig()
    //let mgr = Orchestrator example
    //mgr.InitServer()
    //mgr.Init()
    //mgr.Apply()
    //mgr.Loop()
    0