module KickVote

open FSharp.Data
open Microsoft.Bot.Schema
open System.Collections.Generic
open Microsoft.Bot.Builder
open System
open System.Threading.Tasks
open Microsoft.Bot.Connector

type KickData = JsonProvider<""" { "yes": ["John", "Mary"], "no": ["John", "Mary"], "targetId": "asoentuhoaesnutoehsntoaheu", "targetTag": "aotenuhaoentu",  "reason": "Because"  } """>
let yesVoteText = "Yes"
let noVoteText = "No"
let votesToKick = 5

let buildVotersString (voters: string) =
    if voters.Length = 0 then "" else sprintf "(%s)" voters

let buildKickVoteCard yesCount noCount value reason (mentionText: string) (yesVotes: string) (noVotes: string) =
    let actions = new List<CardAction>()
    actions.Add(new CardAction(Type = ActionTypes.MessageBack, Title = "Yes", Text = "VoteYes", Value = value))
    actions.Add(new CardAction(Type = ActionTypes.MessageBack, Title = "No", Text = "VoteNo", Value = value))
    let yesVoters = buildVotersString yesVotes
    let noVoters = buildVotersString noVotes
    let text = sprintf "Vote to kick %s. For: %s <br/>%d Votes required<br/><br/> %s Yes: %d %s<br/> No: %d %s<br/>" mentionText reason votesToKick System.Environment.NewLine yesCount yesVoters noCount noVoters
    new HeroCard(Title = "Vote to kick", Text = text, Buttons = actions)


let buildValueString yesVotes noVotes targetId targetTag reason =
    sprintf """ { "yes": [%s], "no": [%s], "targetId": "%s", "targetTag": "%s", "reason": "%s" } """ yesVotes noVotes targetId targetTag reason

let buildInitialCard targetId targetTag reason =
    buildKickVoteCard 0 0 (buildValueString "" "" targetId targetTag reason) reason targetTag "" ""

let appendVote (vote:string) voterName votes voteType =
        let hasVoted = Array.exists (fun x -> x = voterName) votes
        match vote = voteType with
        | true -> if(hasVoted) then votes else Array.append [|voterName|] votes 
        | _ -> Array.filter (fun x -> x <> voterName) votes
        |> Array.map (sprintf "\"%s\"")

let updateVotes (kickVoteData: KickData.Root) voterName vote = 
    let voteAppender = appendVote vote voterName
    let newYesVotes = voteAppender kickVoteData.Yes "Yes"
    let newNoVotes = voteAppender kickVoteData.No "No"
    (newYesVotes, newNoVotes)

let buildKickVoteActivity reason (target: Mention) = 
        let userTag = sprintf "<at>%s</at>" target.Mentioned.Name
        let mention = new Mention(Mentioned = target.Mentioned, Text = userTag)
        let card = buildInitialCard target.Mentioned.Id userTag reason
        let activity = MessageFactory.Attachment(card.ToAttachment())
        let entities = new List<Entity>()
        entities.Add(mention)
        activity.Entities <- entities
        activity

let buildUpdateCard (yesVotes: string[]) (noVotes: string[]) (kickVoteData: KickData.Root) =
    let yesVotesString = yesVotes
                        |> String.concat ", "
    let noVotesString = noVotes
                        |> String.concat ", "
    let newValue = buildValueString yesVotesString noVotesString kickVoteData.TargetId kickVoteData.TargetTag kickVoteData.Reason
    buildKickVoteCard yesVotes.Length noVotes.Length newValue kickVoteData.Reason  kickVoteData.TargetTag yesVotesString noVotesString

let kickMember (turnContext: ITurnContext<IMessageActivity>) conversationId (conversations: Conversations) (kickVoteData: KickData.Root) cancellationToken =
    async { 
    turnContext.SendActivityAsync(MessageFactory.Text(sprintf "Goodbye %s" kickVoteData.TargetTag), cancellationToken) |> ignore 
    conversations.DeleteConversationMemberAsync(conversationId, kickVoteData.TargetId) |> ignore 
    turnContext.DeleteActivityAsync(turnContext.Activity.ReplyToId) |> ignore
    } |> Async.StartAsTask :> Task

let sendUpdatedCard (turnContext: ITurnContext<IMessageActivity>) (card: HeroCard) cancellationToken =
    let activity = MessageFactory.Attachment(card.ToAttachment()) 
    activity.Id <- turnContext.Activity.ReplyToId
    async { return turnContext.UpdateActivityAsync(activity, cancellationToken) } |> Async.StartAsTask :> Task

let handleVote (turnContext: ITurnContext<IMessageActivity>) vote conversationId (conversations: Conversations) cancellationToken =
    let valueRaw = string turnContext.Activity.Value
    let kickVoteData = KickData.Parse(valueRaw)
    updateVotes kickVoteData (turnContext.Activity.From.Name) vote
    |> function
        | (yesVotes, _) when yesVotes.Length >= votesToKick -> 
            kickMember turnContext conversationId conversations kickVoteData cancellationToken
        | (yesVotes, noVotes) when noVotes.Length >= votesToKick ->
            async { 
                turnContext.DeleteActivityAsync(turnContext.Activity.ReplyToId) |> ignore
                turnContext.SendActivityAsync(MessageFactory.Text(sprintf "Vote failed %d to %d" noVotes.Length yesVotes.Length), cancellationToken) |> ignore
            } |> Async.StartAsTask :> Task
        | (yesVotes, noVotes) -> 
            let card = buildUpdateCard yesVotes noVotes kickVoteData
            sendUpdatedCard turnContext card cancellationToken