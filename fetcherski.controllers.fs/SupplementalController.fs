namespace fetcherski.controllers.fs

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open fetcherski.tools

/// ASP.Net API controller written in F# only to demonstrate how to write asynchronous action methods.
[<Route("api")>]
[<Authorize(nameof KerbungleRequirement)>]
type SupplementalController() =
    inherit Controller()

    let makeLongPlopAsync ct =
        task {
            do! Task.Delay(100, ct)
            // Return is ceremoniously needed in asynchronous computation expressions.
            // The returned value is the task result, and it is cast to the interface IActionResult implemented
            // by all ASP.Net action result classes. The casting is not necessary unless several different expressions
            // may produce results for a single action method.
            return JsonResult({| Don = "Pedro" |}) :> IActionResult
        }

    let makeShortPlopAsync ct =
        task {
            do! Task.Delay(10, ct)
            // Return is ceremoniously needed in asynchronous computation expressions.
            // The returned value is the task result, and it is cast to the interface IActionResult implemented
            // by all ASP.Net action result classes. The casting is not necessary unless several different expressions
            // may produce results for a single action method.
            return JsonResult({| Don = "Julio" |}) :> IActionResult
        }

    [<Route("plop")>]
    [<HttpGet>]
    member this.GetPlopAsync(ct: CancellationToken) =
        if Random.Shared.Next 2 = 1 then makeShortPlopAsync ct else makeLongPlopAsync ct
