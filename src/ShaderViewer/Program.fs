module ShaderViewer.Program

open System
open Altseed2
open EffFs

type Handler = {
  setShader: Shader -> unit
} with
  static member Handle(x) = x
  static member Handle(e: Model.ShaderEffect, k) =
    Eff.capture (fun h ->
    let mutable shader = null
    let res = e |> function
      | Model.ShaderEffect.Open path ->
        
        Shader.TryCreateFromFile ("pixelshader", path, ShaderStage.Pixel, &shader)
      | Model.ShaderEffect.Update path ->
        StaticFile.Create(path).Reload() |> ignore
        Shader.TryCreateFromFile ("pixelshader", path, ShaderStage.Pixel, &shader)

    if isNull shader then
      Error res |> k
    else
      h.setShader(shader)
      Ok res |> k
    )

type MyPostEffect(texture: RenderTexture) =
  inherit PostEffectNode ()
  let mutable time = 0.0f
  let material = Material.Create()
  let param = RenderPassParameter(Color(50, 50, 50, 255), true, true)

  do
    
    material.SetVector4F("windowSize", Vector4F(float32 texture.Size.X, float32 texture.Size.Y, 0.0f, 0.0f))

  member __.SetShader(shader) =
    time <- 0.0f
    material.SetShader(shader)

  override this.OnUpdate() =
    time <- time + Engine.DeltaSecond
    material.SetVector4F("time", Vector4F(time, sin time, cos time, 0.0f))

  override this.Draw(_src, _clearColor) =
    if material.GetShader(ShaderStage.Pixel) <> null then
      Engine.Graphics.CommandList.RenderToRenderTexture (material, texture, param)



[<EntryPoint; STAThread>]
let main _ =
  let config =
    Configuration
      ( EnabledCoreModules = (CoreModules.Graphics ||| CoreModules.Tool)
      , ConsoleLoggingEnabled = true
      , FileLoggingEnabled = true
      )

  if not <| Engine.Initialize("ShaderViewer", 800, 600, config) then
    failwith "Failed to initialize the Engine"

  let texture = RenderTexture.Create(Vector2I(800, 600), TextureFormat.R8G8B8A8_UNORM)
  
  let posteffect = MyPostEffect(texture)
  Engine.AddNode(posteffect)

  let handler = {
    setShader = posteffect.SetShader
  }

  let dispatcher =
    let initState = Model.State.Init ()
    let update msg state = Model.update msg state |> Eff.handle handler
    Dispatcher(initState, update)

  let viewer = Imgui.makeViewer {
    texture = texture
    dispatch = dispatcher.Dispatch
  }

  let rec loop () =
    if Engine.DoEvents() then
      viewer dispatcher.State
      Engine.Update() |> ignore
      loop ()

  loop ()
  Engine.Terminate()
  0
