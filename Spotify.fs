module Spotify

open FSharp.Data

let loadHtml title subTitle =
    let googleSearch = sprintf "http://www.google.com/search?q=site:spotify.com+%s+%s" title subTitle
    HtmlDocument.Load(googleSearch)

let getSpotifyUrl (title: string) (subTitle: string) =
    let getAnchorNameAndUrl (x: HtmlNode) = x.TryGetAttribute("href")
                                            |> Option.map (fun a -> x.InnerText(), a.Value())
    let isSearchResultLink (name: string, url: string) =
            name <> "Cached" && name <> "Similar" && url.StartsWith("/url?")
    let extractResultUrl (url: string) = url.Substring(0, url.IndexOf("&sa=")).Replace("/url?q=", "")
    let isValidSpotifyUrl (url: string) = url.StartsWith("https://open.spotify.com")

    let results = loadHtml title subTitle
    results.Descendants["a"]
        |> Seq.choose getAnchorNameAndUrl
        |> Seq.filter isSearchResultLink
        |> Seq.map snd
        |> Seq.map extractResultUrl
        |> Seq.filter isValidSpotifyUrl
        |> Seq.head