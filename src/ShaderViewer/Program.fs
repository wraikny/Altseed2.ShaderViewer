module ShaderViewer.Program

open System
open System.Collections.Generic
open Altseed2

let makeDispatcher initModel update =
  let mutable isUpdating = false
  let mutable modelMemo = initModel
  let queue = Queue()

  fun msg ->
    if isUpdating then
      queue.Enqueue msg
    else
      isUpdating <- true

      let rec applyQueueMsgs model =
        queue.TryDequeue() |> function
        | true, msg ->
          model
          |> update msg
          |> applyQueueMsgs
        | _ -> model

      modelMemo <- modelMemo |> update msg |> applyQueueMsgs

      isUpdating <- false

module Viewer =
  let white = Color(255, 255, 255, 255)

  let inline window name flag f =
    if Engine.Tool.Begin (name, flag) then
      f ()
      Engine.Tool.End ()

  let makeViewer (texture) =

    let t = Engine.Tool
    fun () ->
      let flags = ToolWindowFlags.None
      window "Shader" flags (fun () ->
        t.Image(texture, Vector2F(600.0f, 450.0f), Vector2F(0.0f, 0.0f), Vector2F(1.0f, 1.0f), white, white)
      )
      ()

[<EntryPoint>]
let main _ =
  let config =
    Configuration
      ( IsResizable = true
      , EnabledCoreModules = (CoreModules.Default ||| CoreModules.Tool)
      , ConsoleLoggingEnabled = true
      , FileLoggingEnabled = true
      )

  if not <| Engine.Initialize("ShaderViewer", 800, 600, config) then
    failwith "Failed to initialize the Engine"

  let viewer = makeViewer ()

  let rec loop () =
    if Engine.DoEvents() then
      viewer ()
      Engine.Update() |> ignore
      loop ()

  loop ()
  Engine.Terminate()
  0
