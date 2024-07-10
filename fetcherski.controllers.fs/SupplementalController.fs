namespace fetcherski.controllers.fs

open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open fetcherski.tools

[<Route("api")>]
[<Authorize(nameof GrpcKerbungleRequirement)>]
type SupplementalController() =
    inherit Controller()

    [<Route("plop")>]
    [<HttpGet>]
    member this.GetPlopAsync(ct: CancellationToken) =
        task {
            do! Task.Delay(100, ct)
            return JsonResult({| Don = "Pedro" |}) :> IActionResult
        }
