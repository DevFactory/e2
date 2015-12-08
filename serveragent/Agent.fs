module ServerAgent
open Socket
open BessSerialization
open System.Net
open System.Net.Sockets
open System.Collections.Generic

let default_port = 10516
let default_address = IPAddress.Parse("127.0.0.1")

[<BessMessage>]
type private ServerAgentRequest(cmd: string, parameters: obj) =
  let _cmd = cmd
  let _parameters = parameters
  member this.Cmd with get() = _cmd
  member this.Parameters with get() = _parameters

[<BessMessage>]
type private LaunchNF(handle: string, kind: string, vport: string) =
  let _handle = handle
  let _kind = kind
  let _vport = vport
  member this.Handle with get() = _handle
  member this.Kind with get() = _kind
  member this.Vport with get() = _vport

[<BessMessage>]
type private StopNF(handle: string, delVport: int) =
  let _handle = handle
  let _delVport = delVport
  member this.Handle with get() = _handle
  member this.DelVPort with get() = _handle

// Add messages for listing resources etc.
type ServerAgent(address: IPAddress, port: int) =
  let address = address
  let port = port
  let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

  let receiveResponse() =
    let lenBa : (byte array) = Array.zeroCreate 4
    if (socket.ReceiveSynchronously lenBa) then () 
    else failwith "Could not receive"
    let len = ReadLength lenBa
    let ba : (byte array) = Array.zeroCreate len
    if (socket.ReceiveSynchronously ba) then () 
    else failwith "Could not receive"
    let result = Decode ba
    result

  let request (cmd: string) (args: obj) =
    let request = new ServerAgentRequest(cmd, args)
    let result = (Encode request) |> socket.SendSynchronously
    if result then () else failwith "Could not send request to BESS"
    receiveResponse()

  /// LaunchNF arguments
  /// handle: A string that will be used henceforth to refer to this
  /// kind: NF type to launch
  /// vport: VPort to connect NF
  member this.LaunchNF (handle: string, kind: string, vport: string) =
    request "launch" (new LaunchNF(handle, kind, vport))
  
  /// StopNF: This "stops" the NF (marks it for eventual garbage collection)
  /// handle: NF handle
  /// keepVport: Should the vport be kept (i.e., not deleted) after NF is
  /// shutdown. This is false by default
  member this.StopNF (handle: string, ?keepVport: bool) =
    let delVport = match keepVport with
                   | Some(true) -> 0
                   | Some(false) -> 1
                   | None -> 1
    request "stop" (new StopNF(handle, delVport))
  
  member this.ReportPerf (handle: string) =
    request "perf" (box handle)
  
  /// ListRunning: List all currently running NFs (this includes NFs marked for
  /// destruction which haven't yet been destroyed
  member this.ListRunning (?kind: string) =
    let arg = match kind with
              | Some(k) -> (box k)
              | None -> null
    request "list_running" arg
  
  /// ListMarked: List all currently running NFs that have been marked for
  /// destruction.
  member this.ListMarked (?kind: string) =
    let arg = match kind with
              | Some(k) -> (box k)
              | None -> null
    request "list_marked" arg

  /// StartBess: Start Bess.
  member this.StartBess() =
    request "start_bess" null
  
  /// StopBess: Stop Bess.
  member this.StopBess() =
    request "stop_bess" null
