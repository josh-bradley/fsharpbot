namespace SpotifyBot

open Microsoft.Bot.Builder.Teams
open Microsoft.Bot.Builder
open Microsoft.Bot.Schema
open System.Threading
open System.Threading.Tasks
open FSharp.Data

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

    member this.GetAppleIdentifiers (appleUrl: string) : Option<string * string> =
        let results = HtmlDocument.Load(appleUrl)

        let isSpan (x: HtmlNode) = x.Name() = "span"
        let isAnchor (x: HtmlNode) = x.Name() = "a"

        results.Descendants["h1"]
            |> Seq.toList
            |> function
                | [] -> None
                | head::_ ->
                    head
                    |> HtmlNode.descendants false isSpan
                    |> Seq.toList
                    |> function
                        | [mainTitleSpan; subTitleSpan;] ->
                            let mainTitle = mainTitleSpan |> HtmlNode.innerText
                            let subTitle = HtmlNode.descendants false isAnchor subTitleSpan
                                            |> Seq.toList
                                            |> function
                                                | [] -> ""
                                                | head::_ -> head |> HtmlNode.innerText
                            Some (mainTitle, subTitle)
                        | _ -> None

    member this.GetSpotifyUrl (title: string) (subTitle: string) =
        let googleSearch = sprintf "http://www.google.com/search?q=site:spotify.com+%s+%s" title subTitle
        let results = HtmlDocument.Load(googleSearch)

        let getAnchorNameAndUrl (x: HtmlNode) = x.TryGetAttribute("href")
                                                |> Option.map (fun a -> x.InnerText(), a.Value())
        let isSearchResultLink (name: string, url: string) =
                name <> "Cached" && name <> "Similar" && url.StartsWith("/url?")
        let extractResultUrl (url: string) = url.Substring(0, url.IndexOf("&sa=")).Replace("/url?q=", "")
        let isValidSpotifyUrl (url: string) = url.StartsWith("https://open.spotify.com")


        results.Descendants["a"]
            |> Seq.choose getAnchorNameAndUrl
            |> Seq.filter isSearchResultLink
            |> Seq.map snd
            |> Seq.map extractResultUrl
            |> Seq.filter isValidSpotifyUrl
            |> Seq.head

    member this.BuildMessageText (appleUrl: string) =
            let failedMessage = "Could not find it"
            appleUrl.StartsWith("https://music.apple.com")
            |> function
                | false -> failedMessage
                | true -> this.GetAppleIdentifiers (appleUrl)
                            |> function
                                | None -> failedMessage
                                | Some (title, subTitle) ->
                                    let url = this.GetSpotifyUrl title subTitle
                                    sprintf "FTFY\n[%s](%s)" url url