namespace fetcherski.controllers.fs

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open fetcherski.tools

/// <summary>
/// ASP.Net API controller written in F# only to demonstrate how to write asynchronous action methods.
/// 'KerbungleRequirement' policy applies the <see cref="KerbungleRequirement"/> requirement that has one
/// parameter, <see cref="KerbungleRequirement.KerbungleTokenRequired"/>, that tells the authorization handler
/// to check the authenticated user for the authenticated identity of the type 'Kerbungle', added by
/// <see cref="KerbungleAuthentication"/> if the Authorization header hes a Kerbungle token.
/// </summary>
/// <seealso cref="KerbungleAuthentication"/>
/// <seealso cref="KerbungleAuthorization"/>
[<Route("api/mafia")>]
[<Authorize(nameof KerbungleRequirement)>]
type SupplementalController(logger: ILogger<SupplementalController>) =
    inherit Controller()

    // Parentheses are needed because all C# functions in F# take arguments as tuples
    static let MakeDonEventId = EventId(1, nameof SupplementalController)

    let rec makeDonAsync (donName: string) (delay: int) ct =
        logger.LogTrace(MakeDonEventId, "{member} | donName={donName}, delay={delay}", nameof makeDonAsync, donName, delay)

        task {
            do! Task.Delay(delay, ct)
            // Return is ceremoniously needed in asynchronous computation expressions.
            // The returned value is the task result, and it is cast to the interface IActionResult implemented
            // by all ASP.Net action result classes. The casting is not necessary unless several different expressions
            // may produce results for a single action method.
            return JsonResult({| Don = donName |}) :> IActionResult
        }

    [<Route("don")>]
    [<HttpGet>]
    member this.GetDonAsync ct =
        match Random.Shared.Next 2 with
        | 1 -> makeDonAsync "Pedro" 100 ct
        | _ -> makeDonAsync "Julio" 50 ct
