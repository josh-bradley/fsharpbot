module MessageBuilder

open AdditionalMessages
open Apple
open Spotify
open Microsoft.Bot.Builder
open Microsoft.Bot.Schema

let getAdditionalMessage() =
    let ran = new System.Random()
    let num = ran.Next(0, additionalMessages.Length - 1)
    additionalMessages.[num]

let buildMessageText (message: string) =
    let failedMessage = ""

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

let buildImageMessage imageString =
    let messageText = sprintf "![Image](%s)" imageString
    let message = MessageFactory.Text(messageText)
    message.TextFormat <- TextFormatTypes.Markdown
    message
