namespace SpotifyBot

open Microsoft.Bot.Builder.Teams
open Microsoft.Bot.Builder
open Microsoft.Bot.Schema
open System.Threading
open System.Threading.Tasks
open MessageBuilder
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

        let rawText = turnContext.Activity.Text
        let messageText = rawText.Trim()
                            |> buildMessageText

        match messageText with
        | "" -> async { return turnContext.SendActivityAsync(MessageFactory.Text("Unable to find for " + rawText), cancellationToken) } |> Async.StartAsTask :> Task
        | _ ->
            let message = MessageFactory.Text(messageText)
            message.TextFormat <- TextFormatTypes.Markdown
            let destinationConversationId = this.Configuration.["TeamsDestinationConversationId"]
            async { return conversations.SendToConversationWithHttpMessagesAsync(destinationConversationId, message) } |> Async.StartAsTask :> Task

    member val Configuration : IConfiguration = null with get, set