namespace SpotifyBot

open Microsoft.Bot.Builder.Teams
open Microsoft.Bot.Builder
open Microsoft.Bot.Schema
open System.Threading
open System.Threading.Tasks
open Apple
open Spotify
open Microsoft.Bot.Connector
open System
open Microsoft.Extensions.Configuration

type TeamsSpotifyBot() =
    inherit TeamsActivityHandler()
    new (configuration: IConfiguration) as this =
        TeamsSpotifyBot() then
            this.Configuration <- configuration

    override this.OnMessageActivityAsync(turnContext: ITurnContext<IMessageActivity> , cancellationToken: CancellationToken) =
        turnContext.Activity.RemoveRecipientMention() |> ignore

        let baseUri = new Uri(turnContext.Activity.ServiceUrl);
        let connector = new Microsoft.Bot.Connector.ConnectorClient(baseUri, this.Configuration.["MicrosoftAppId"], this.Configuration.["MicrosoftAppPassword"])
        let conversations = new Conversations(connector)

        let messageText = turnContext.Activity.Text.Trim()
                            |> this.BuildMessageText

        let message = MessageFactory.Text(messageText)
        message.TextFormat <- TextFormatTypes.Markdown
        let destinationConversationId = this.Configuration.["TeamsDestinationConversationId"]
        async { return conversations.SendToConversationWithHttpMessagesAsync(destinationConversationId, message) } |> Async.StartAsTask :> Task

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

    member val Configuration : IConfiguration = null with get, set