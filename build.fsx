#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target
nuget FAKE.IO.Zip
nuget FAKE.Net.Http
nuget FSharp.Json //"
#load ".fake/build.fsx/intellisense.fsx"
open System

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Net

open FSharp.Json

Target.initEnvironment ()

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "publish"
    |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)

let runtimes = [ "linux-x64"; "win-x64"; "osx-x64" ]
let publishOutput = sprintf "publish/Altseed2.ShaderViewer.%s"

Target.create "Publish" (fun _ ->
  runtimes
  |> Seq.iter (fun target ->
    let outputPath = publishOutput target

    "src/ShaderViewer/ShaderViewer.fsproj"
    |> DotNet.publish (fun p ->
      { p with
          Runtime = Some target
          Configuration = DotNet.BuildConfiguration.Release
          SelfContained = Some true
          MSBuildParams = {
            p.MSBuildParams with
              Properties =
                ("PublishSingleFile", "true")
                :: ("PublishTrimmed", "true")
                :: p.MSBuildParams.Properties
          }
          OutputPath = outputPath |> Some
      }
    )

    Shell.copyDir (sprintf "%s/Samples" outputPath) @"src/Shaders" (String.endsWith ".hlsl")

    !! (sprintf "%s/**" outputPath)
    |> Zip.zip "publish" (sprintf "%s.zip" outputPath)
  )
)

let [<Literal>] altseed2Dir = @"lib/Altseed2"

Target.create "Download" (fun _ ->
  let commitId = "14171e9f9a520b66422a6d62b5462d78512f6e5c"

  let token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
  let url = @"https://api.github.com/repos/altseed/altseed2-csharp/actions/artifacts"

  use client = new Net.Http.HttpClient()
  client.DefaultRequestHeaders.UserAgent.ParseAdd("wraikny.Altseed2.ShaderViewer")
  client.DefaultRequestHeaders.Authorization <- Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)

  let downloadName = sprintf "Altseed2-%s" commitId

  let rec getArchiveUrl page = async {
    Trace.tracefn "page %d" page
    let! data = client.GetStringAsync(sprintf "%s?page=%d" url page) |> Async.AwaitTask

    let artifacts =
      data
      |> Json.deserialize<{| artifacts: {| name: string; archive_download_url: string; expired: bool |} [] |}>

    if artifacts.artifacts |> Array.isEmpty then
      failwithf "'%s' is not found" downloadName

    match
      artifacts.artifacts
      |> Seq.tryFind(fun x -> x.name = downloadName) with
    | Some x when x.expired -> return failwithf "'%s' is expired" downloadName
    | Some x -> return x.archive_download_url
    | None -> return! getArchiveUrl (page + 1)
  }

  let outputFilePath = sprintf "%s.zip" altseed2Dir

  async {
    let! archiveUrl = getArchiveUrl 1

    let! res =
      client.GetAsync(archiveUrl, Net.Http.HttpCompletionOption.ResponseHeadersRead)
      |> Async.AwaitTask

    use fileStream = IO.File.Create(outputFilePath)
    use! httpStream = res.Content.ReadAsStreamAsync() |> Async.AwaitTask
    do! httpStream.CopyToAsync(fileStream) |> Async.AwaitTask
    do! fileStream.FlushAsync() |> Async.AwaitTask
  } |> Async.RunSynchronously

  Zip.unzip altseed2Dir outputFilePath
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "All"

Target.runOrDefault "All"
