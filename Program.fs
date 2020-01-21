namespace SpotifyBot

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting

module Program =
    let exitCode = 0

    let appConfiguration (_:HostBuilderContext) (config:IConfigurationBuilder) = config.AddEnvironmentVariables() |> ignore

    let config = new Action<HostBuilderContext, IConfigurationBuilder> (appConfiguration)

    let CreateHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder.UseStartup<Startup>() |> ignore
            )
            .ConfigureAppConfiguration(config)

    [<EntryPoint>]
    let main args =
        CreateHostBuilder(args).Build().Run()

        exitCode
