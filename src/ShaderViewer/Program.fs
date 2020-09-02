module ShaderViewer.Program

open System
open Altseed2
open EffFs

type Handler = {
  material: Material
} with
  static member Handle(x) = x
  static member Handle(Model.OpenShaderEffect path, k) =
    Eff.capture (fun h ->
    let mutable shader = null
    let res = Shader.TryCreateFromFile ("pixelshader", path, ShaderStage.Pixel, &shader)
    if isNull shader then
      Error res |> k
    else
      h.material.SetShader(shader)
      Ok res |> k
    )

type MyPostEffect(texture) =
  inherit PostEffectNode ()
  let mutable time = 0.0f
  let material = Material.Create()
  let param = RenderPassParameter(Color(50, 50, 50, 255), true, true)

  member __.Material with get() = material

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
    material = posteffect.Material
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
