namespace SpotifyBot

open MessageBuilder
open KickVote
open Microsoft.Bot.Builder.Teams
open Microsoft.Bot.Builder
open Microsoft.Bot.Connector
open Microsoft.Bot.Schema
open Microsoft.Bot.Schema.Teams
open Microsoft.Extensions.Configuration
open System
open System.Threading
open System.Threading.Tasks

type TeamsSpotifyBot() =
    inherit TeamsActivityHandler()
    new (configuration: IConfiguration) as this =
        TeamsSpotifyBot() then
            this.Configuration <- configuration

    override this.OnTeamsChannelRenamedAsync(channelInfo: ChannelInfo, _: TeamInfo, turnContext: ITurnContext<IConversationUpdateActivity>, cancellationToken: CancellationToken) =
        let msg = sprintf "%s changed the group name to %s" turnContext.Activity.From.Name channelInfo.Name
        async { return turnContext.SendActivityAsync(MessageFactory.Text(msg), cancellationToken) } |> Async.StartAsTask :> Task

    override this.OnMessageActivityAsync(turnContext: ITurnContext<IMessageActivity>, cancellationToken: CancellationToken) =
        let mentions = turnContext.Activity.GetMentions()
        turnContext.Activity.RemoveRecipientMention() |> ignore

        let baseUri = new Uri(turnContext.Activity.ServiceUrl);
        let connector = new Microsoft.Bot.Connector.ConnectorClient(baseUri, this.Configuration.["MicrosoftAppId"], this.Configuration.["MicrosoftAppPassword"])
        let conversations = new Conversations(connector)

        let rawText = turnContext.Activity.Text

        let (|Prefix|_|) (p:string) (s:string) =
            if s.StartsWith(p) then
                Some(s.Substring(p.Length))
            else
                None
        
        let destinationConversationId = this.Configuration.["TeamsDestinationConversationId"]
        match rawText with
        | Prefix "kickvote" reason ->
            let targets = mentions
                            |> Array.filter (fun x -> x.Mentioned.Id <> turnContext.Activity.Recipient.Id)
            let target = targets.[0]

            match target.Mentioned.Name.Contains("aoehuaoeuoeu") with
            | true -> 
                let message = buildImageMessage "https://media.giphy.com/media/uIGfoVAK9iU1y/giphy.gif"
                async { return turnContext.SendActivityAsync(message, cancellationToken) } |> Async.StartAsTask :> Task
            | false ->
                let activity = buildKickVoteActivity reason target
                async { return turnContext.SendActivityAsync(activity) } |> Async.StartAsTask :> Task
        | Prefix "Vote" vote ->
            handleVote turnContext vote turnContext.Activity.Conversation.Id conversations cancellationToken
        | _ ->
            let messageText = rawText.Trim()
                                |> buildMessageText

            match messageText with
            | "" -> async { return turnContext.SendActivityAsync(MessageFactory.Text("Unable to find for " + rawText), cancellationToken) } |> Async.StartAsTask :> Task
            | _ ->
                let message = MessageFactory.Text(messageText)
                message.TextFormat <- TextFormatTypes.Markdown
                
                async { return conversations.SendToConversationWithHttpMessagesAsync(destinationConversationId, message) } |> Async.StartAsTask :> Task

    member val Configuration : IConfiguration = null with get, set