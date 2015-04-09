﻿// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"bin/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open XUnit2Helper
open System
open System.IO

let solutionFile  = "HdrHistogram.sln"
let assemblyInfoFile = ".\SharedAssemblyInfo.cs"

let sources = "./Src/HdrHistogram/"

let nuspec = sources + "HdrHistogram.nuspec"
let sourcesNuspec = sources + "HdrHistogram.Source.nuspec"
let nugetOutput = "./bin/NuGet"

let testAssemblies = "bin/Release/*Tests*.dll"

let release = LoadReleaseNotes "CHANGELOG.md"

Target "Clean" (fun _ ->
    CleanDirs ["bin\Debug"; "bin\Release"; "packages"]
)

Target "RestoreNuget" <| fun _ -> RestorePackages()

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" <| fun _ ->
    CreateCSharpAssemblyInfo assemblyInfoFile
            [Attribute.Company "Iulian Margarintescu"
             Attribute.Copyright ("Copyright Iulian Margarintescu © " + DateTime.Now.Year.ToString())
             Attribute.Product "HdrHistogram .NET"
             Attribute.Description "A High Dynamic Range (HDR) Histogram"
             Attribute.Culture ""
             Attribute.ComVisible false
             Attribute.Version release.AssemblyVersion
             Attribute.FileVersion release.AssemblyVersion]


let buildWithParams properties =
    solutionFile
    |> build (fun p ->
    { p with
        Verbosity = Some(MSBuildVerbosity.Minimal)
        Targets = ["Rebuild"]
        Properties = properties
    })

Target "BuildDebug" <| fun _ -> buildWithParams ["Configuration", "Debug"]
Target "Build" <| fun _ -> buildWithParams ["Configuration", "Release"]

let findXunit =
    try
        !!(@".\packages\xunit.runner.console.2.0.0*\tools\xunit.console.exe") |> Seq.exactlyOne
    with
    | _ ->
        CleanDir "packages"
        Run "RestoreNuget"
        !!(@".\packages\xunit.runner.console.2.0.0*\tools\xunit.console.exe") |> Seq.exactlyOne

Target "RunTests" (fun _ ->
    !! testAssemblies
    |> xUnit2 (fun p ->
        { p with
            ToolPath = findXunit
            MaxThreads = 4
            TimeOut = TimeSpan.FromMinutes 20.
            Parallel = ParallelOption.Collections })
)

Target "SourceNuGet" <| fun _ ->
    let workDir = "./bin/Release/NuGet.Sources/"
    ensureDirectory workDir
    CleanDir workDir
    ensureDirectory nugetOutput
    CleanDir nugetOutput
    

    for file in !! (sources + "*.cs") do
        let name = Path.GetFileNameWithoutExtension file
        let content = File.ReadAllText file
        let processed = replace "namespace HdrHistogram" "namespace $rootnamespace$" content 
        let output = Path.Combine(workDir, (name + ".cs.pp"))
        File.WriteAllText(output, processed)

    let files = 
        !!(workDir + "*.pp")
        |> Seq.map (fun f ->  f,Some (@"content\App_Packages\HdrHistogram."+ release.NugetVersion + @"\" + (Path.GetFileName f)), None )
        |> Seq.toList

    NuGet (fun p ->
        {p with
            OutputPath = nugetOutput
            WorkingDir = workDir
            Version = release.NugetVersion
            Dependencies = []
            Files = files
            Publish = false })
            sourcesNuspec
        

Target "NuGet" <| fun _ ->
    let workDir = "./bin/Release/NuGet/"
    ensureDirectory workDir
    CleanDir workDir
    ensureDirectory nugetOutput
    CleanDir nugetOutput
    
    NuGet (fun p ->
        {p with
            OutputPath = nugetOutput
            WorkingDir = workDir
            Version = release.NugetVersion
            Dependencies = []
            Publish = false })
            nuspec

Target "All" DoNothing

"Clean"
  ==> "RestoreNuget"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "NuGet"
  ==> "SourceNuGet"
  ==> "All"

RunTargetOrDefault "All"