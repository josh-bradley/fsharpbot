namespace SpotifyBot

open Microsoft.Bot.Builder.Integration.AspNet.Core;
open Microsoft.Bot.Builder.TraceExtensions;
open Microsoft.Extensions.Configuration;
open Microsoft.Extensions.Logging;

type public AdapterWithErrorHandler =
  inherit BotFrameworkHttpAdapter
  public new (configuration: IConfiguration, logger: ILogger<BotFrameworkHttpAdapter>) = {
    inherit BotFrameworkHttpAdapter(configuration, logger);
  }
