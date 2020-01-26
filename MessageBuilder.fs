module MessageBuilder

open Apple
open Spotify


let buildMessageText (message: string) =
    let failedMessage = ""
    let r = message.Split ' '

    message.Split ' '
    |> function
        | [|command; appleUrl|] -> 
            let additional = if(command.Trim() = "Surprise") then "![Surprise](https://i.imgur.com/gDQk9GQ.gif)" else "FTFY"
            appleUrl.Trim().StartsWith("https://music.apple.com")
            |> function
                | false -> failedMessage
                | true -> 
                    getAppleIdentifiers (appleUrl.Trim())
                    |> function
                        | None -> failedMessage
                        | Some (title, subTitle) ->
                            let url = getSpotifyUrl title subTitle
                            sprintf "[%s](%s) \n\n %s" url url additional
        | _ -> failedMessage