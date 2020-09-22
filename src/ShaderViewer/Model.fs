module ShaderViewer.Model

type State = {
  path: string voption
  compileResult: string
  logs: string list
} with
  static member Init () = {
    path = ValueNone
    compileResult = ""
    logs = List.empty
  }

type Msg =
  | Open of string
  | Update

open EffFs

[<Struct>]
type ShaderEffect =
  | Open of op:string
  | Update of update:string
with
  static member Effect(_) = Eff.marker<Result<string, string>>


let logf state fmt = Printf.kprintf (fun s -> { state with logs = s::state.logs }) fmt


let inline openShader res path state = eff {
  let state = { state with path = ValueSome path }

  match res with
  | Ok msg ->
    let state = logf state "Success to open '%s'" path
    return { state with compileResult = sprintf "Success\n%s" msg }

  | Error msg ->
    let state = logf state "Failed to open '%s'" path
    return { state with compileResult = sprintf "Error\n%s" msg  }
}



let inline update msg state = eff {
  match msg with
  | Msg.Open (path) ->
    if state.path = ValueSome path then
      let state = logf state "'%s' is already open" path
      return state
    else
      let! res = ShaderEffect.Open path
      return! openShader res path state

  | Msg.Update ->
    match state.path with
    | ValueNone -> return state
    | ValueSome path ->
      let! res = ShaderEffect.Update path
      return! openShader res path state
}
