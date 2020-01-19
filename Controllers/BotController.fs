namespace SpotifyBot.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Bot.Builder.Integration.AspNet.Core
open Microsoft.Bot.Builder
open System.Threading.Tasks

[<ApiController>]
[<Route("api/messages")>]
type BotController (adapter: IBotFrameworkHttpAdapter, bot: IBot) as this =
    inherit ControllerBase()

    [<HttpPost>]
    member __.Post() : Task =
        async {
            return adapter.ProcessAsync(this.Request, this.Response, bot);
        } |> Async.StartAsTask :> Task
        
