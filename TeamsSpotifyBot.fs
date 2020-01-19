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

        let url = this.GetUrl (turnContext.Activity.Text.Trim())
        let message = MessageFactory.Text(sprintf "FTFY\n[%s](%s)" url url)
        message.TextFormat <- TextFormatTypes.Markdown
        async { return turnContext.SendActivityAsync(message, cancellationToken) } |> Async.StartAsTask :> Task

   member this.GetUrl (appleUrl: string) = 
    let results = HtmlDocument.Load(appleUrl)

    let isSpan (x: HtmlNode) = x.Name() = "span"

    let headerSpan = results.Descendants["h1"]
                      |> Seq.head
                      |> HtmlNode.descendants false isSpan
                      |> Seq.head

    let name = headerSpan.InnerText()

    let googleSearch = sprintf "http://www.google.com/search?q=site:spotify.com+%s" name
    let results = HtmlDocument.Load(googleSearch)

    results.Descendants["a"]
        |> Seq.choose (fun x -> x.TryGetAttribute("href")
                                |> Option.map (fun a -> x.InnerText(), a.Value()))
        |> Seq.filter (fun (name, url) -> 
            name <> "Cached" && name <> "Similar" && url.StartsWith("/url?"))
        |> Seq.map (fun (name, url) -> url.Substring(0, url.IndexOf("&sa=")).Replace("/url?q=", ""))
        |> Seq.filter (fun url -> url.StartsWith("https://open.spotify.com"))
        |> Seq.head