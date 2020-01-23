module Apple

open FSharp.Data

let isElement (elementName: string) (x: HtmlNode) = x.Name() = elementName

let getAppleIdentifiers (appleUrl: string) : Option<string * string> =
    let results = HtmlDocument.Load(appleUrl)

    let isSpan = isElement "span"
    let isAnchor = isElement "a"

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