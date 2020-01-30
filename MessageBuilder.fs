module MessageBuilder

open AdditionalMessages
open Apple
open Spotify

let getAdditionalMessage() =
    let ran = new System.Random()
    let num = ran.Next(0, additionalMessages.Length - 1)
    additionalMessages.[num]

let buildMessageText (message: string) =
    let failedMessage = ""
    let r = message.Split ' '

    message.Split ' '
    |> function
        | [|command; appleUrl|] -> 
            let additional = if(command.Trim() = "Surprise") then "Kind Regards" else getAdditionalMessage()
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
