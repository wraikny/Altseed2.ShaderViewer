namespace ShaderViewer

open System.Collections.Generic

type Dispatcher<'state, 'msg> (initState: 'state, update: 'msg -> 'state -> 'state) =
  let mutable isUpdating = false
  let mutable stateMemo = initState
  let queue = Queue()

  member __.Dispatch msg =
    if isUpdating then
      queue.Enqueue msg
    else
      isUpdating <- true

      let rec applyQueueMsgs state =
        queue.TryDequeue() |> function
        | true, msg ->
          state
          |> update msg
          |> applyQueueMsgs
        | _ -> state

      stateMemo <- stateMemo |> update msg |> applyQueueMsgs

      isUpdating <- false

  member __.State with get() = stateMemo