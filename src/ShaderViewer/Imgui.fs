module ShaderViewer.Imgui

open Altseed2

let white = Color(255, 255, 255, 255)

let inline fixedWindow name (area: RectF) f =
  let flags = ToolWindowFlags.NoResize ||| ToolWindowFlags.NoMove ||| ToolWindowFlags.NoCollapse

  Engine.Tool.SetNextWindowPos (area.Position, ToolCond.Always)
  Engine.Tool.SetNextWindowSize (area.Size, ToolCond.Always)
  if Engine.Tool.Begin (name, flags) then
    f ()
    Engine.Tool.End ()

type Param = {
  texture: TextureBase
  dispatch: Model.Msg -> unit
}

let makeViewer param =
  fun (state: Model.State) ->
    let size = Vector2F(600.0f, 450.0f)
    fixedWindow "Shader" (RectF(Vector2F(), size)) (fun () ->
      Engine.Tool.Image(param.texture, size * Vector2F(0.9f, 0.9f), Vector2F(0.0f, 0.0f), Vector2F(1.0f, 1.0f), white, Color())
    )

    fixedWindow "Message" (RectF(0.0f, size.Y, 600.0f, 150.0f)) (fun () ->
      Engine.Tool.Text state.compileResult
    )

    fixedWindow "UI" (RectF(size.X, 0.0f, 200.0f, 300.0f)) (fun () ->
      if Engine.Tool.SmallButton("Open file") then
        let path = Engine.Tool.OpenDialog ("hlsl", System.Environment.CurrentDirectory)
        // let path = @"/Users/binar/Altseed2Projects/ShaderViewer/src/Shaders/sample.hlsl"
        if path <> null && path <> "" then
          param.dispatch (Model.Open path)
        ()
    )

    fixedWindow "Log" (RectF(size.X, 300.0f, 200.0f, 300.0f)) (fun () ->
      let n = 20

      ( if state.logs.Length > n then
          state.logs |> Seq.take n
        else state.logs |> List.toSeq
      )
      |> Seq.indexed
      |> Seq.iter (fun (index, msg) ->
        Engine.Tool.Text (sprintf "%d" <| state.logs.Length - index)
        Engine.Tool.TextWrapped msg
        Engine.Tool.Spacing ()
      )
    )
