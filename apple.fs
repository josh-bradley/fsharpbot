module Apple

open FSharp.Data

let isElement (elementName: string) (x: HtmlNode) = x.Name() = elementName
let isAnchor = isElement "a"

let getSubTitle subTitleSpan =
    HtmlNode.descendants false isAnchor subTitleSpan
    |> Seq.toList
    |> function
        | [] -> ""
        | head::_ -> head |> HtmlNode.innerText

let getAppleIdentifiers (appleUrl: string) : Option<string * string> =
    let results = HtmlDocument.Load(appleUrl)
    let isSpan = isElement "span"

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
                        let subTitle = getSubTitle subTitleSpan
                        Some (mainTitle, subTitle)
                    | _ -> None