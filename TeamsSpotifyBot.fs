namespace SpotifyBot

open Microsoft.Bot.Builder.Teams
open Microsoft.Bot.Builder
open Microsoft.Bot.Schema
open System.Threading
open System.Threading.Tasks
open Apple
open Spotify

type TeamsSpotifyBot =
   inherit TeamsActivityHandler
   new () = {}

   override this.OnMessageActivityAsync(turnContext: ITurnContext<IMessageActivity> , cancellationToken: CancellationToken) =
        turnContext.Activity.RemoveRecipientMention() |> ignore

        let messageText = turnContext.Activity.Text.Trim()
                            |> this.BuildMessageText

        let message = MessageFactory.Text(messageText)
        message.TextFormat <- TextFormatTypes.Markdown
        async { return turnContext.SendActivityAsync(message, cancellationToken) } |> Async.StartAsTask :> Task

   member this.BuildMessageText (appleUrl: string) =
        let failedMessage = "Could not find what you are looking for"
        appleUrl.StartsWith("https://music.apple.com")
        |> function
            | false -> failedMessage
            | true -> getAppleIdentifiers (appleUrl)
                        |> function
                            | None -> failedMessage
                            | Some (title, subTitle) ->
                                let url = getSpotifyUrl title subTitle
                                sprintf "FTFY: [%s](%s)" url url