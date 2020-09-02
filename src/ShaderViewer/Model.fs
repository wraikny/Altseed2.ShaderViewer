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

open EffFs

[<Struct>]
type OpenShaderEffect = OpenShaderEffect of string with
  static member Effect(_) = Eff.marker<Result<string, string>>


let logf state fmt = Printf.kprintf (fun s -> { state with logs = s::state.logs }) fmt


let inline update msg state = eff {
  match msg with
  | Open (path) ->
    if state.path = ValueSome path then
      let state = logf state "'%s' is already open" path
      return state
    else
      let state = { state with path = ValueSome path }
      let! res = OpenShaderEffect path

      match res with
      | Ok msg ->
        let state = logf state "Success to open '%s'" path
        return { state with compileResult = sprintf "Success\n%s" msg }

      | Error msg ->
        let state = logf state "Failed to open '%s'" path
        return { state with compileResult = sprintf "Error\n%s" msg  }
}
