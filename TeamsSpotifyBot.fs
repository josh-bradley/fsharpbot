namespace SpotifyBot

open Microsoft.Bot.Builder.Teams
open Microsoft.Bot.Builder
open Microsoft.Bot.Schema
open Microsoft.Extensions.Configuration
open System.Threading
open System.Threading.Tasks
open FSharp.Data

type TeamsSpotifyBot =
   inherit TeamsActivityHandler
   new(config: IConfiguration) = {}

   override this.OnMessageActivityAsync(turnContext: ITurnContext<IMessageActivity> , cancellationToken: CancellationToken) =
        turnContext.Activity.RemoveRecipientMention() |> ignore

        let failedMessage = "Could not find it"
        let appleUrl = turnContext.Activity.Text.Trim()
        let messageText = appleUrl.StartsWith("https://music.apple.com")
                            |> function
                                | false -> failedMessage
                                | true -> this.GetAppleIdentifiers (turnContext.Activity.Text.Trim())
                                            |> function
                                                | Some x ->
                                                    let url = this.GetSpotifyUrl x
                                                    sprintf "FTFY\n[%s](%s)" url url
                                                | None -> "Could not find it"

        let message = MessageFactory.Text(messageText)
        message.TextFormat <- TextFormatTypes.Markdown
        async { return turnContext.SendActivityAsync(message, cancellationToken) } |> Async.StartAsTask :> Task

    member this.GetAppleIdentifiers (appleUrl: string) : Option<string> =
        let results = HtmlDocument.Load(appleUrl)

        let isSpan (x: HtmlNode) = x.Name() = "span"

        results.Descendants["h1"]
            |> Seq.toList
            |> function
                | head::_ -> head |> HtmlNode.descendants false isSpan |> Seq.head |> HtmlNode.innerText |> Some
                | [] -> None

    member this.GetSpotifyUrl (name: string) =
        let googleSearch = sprintf "http://www.google.com/search?q=site:spotify.com+%s" name
        let results = HtmlDocument.Load(googleSearch)

        results.Descendants["a"]
            |> Seq.choose (fun x -> x.TryGetAttribute("href")
                                    |> Option.map (fun a -> x.InnerText(), a.Value()))
            |> Seq.filter (fun (name, url) ->
                name <> "Cached" && name <> "Similar" && url.StartsWith("/url?"))
            |> Seq.map (fun (_, url) -> url.Substring(0, url.IndexOf("&sa=")).Replace("/url?q=", ""))
            |> Seq.filter (fun url -> url.StartsWith("https://open.spotify.com"))
            |> Seq.head