using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MopsBot.Data.Tracker;
using static MopsBot.Data.Tracker.BaseTracker;

namespace MopsBot.Module.TypeReader
{
    public class TrackerTypeReader : Discord.Commands.TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var command = context.Message.Content;
            var prefix = await StaticBase.GetGuildPrefixAsync(context.Guild.Id);
            if(!command.StartsWith(prefix))
                prefix = Program.Client.CurrentUser.Mention;
            var module = command.Remove(0, prefix.Length).Split(" ").First(x => x.Length > 0);
            var worked = Enum.TryParse<TrackerType>(module, true, out TrackerType type);
            
            if(!CapSensitive.Any(x => x == type))
                input = input.ToLower();

            var result = StaticBase.Trackers[type].GetTracker(context.Channel.Id, input);

            if (result != null){
                result.LastCalledChannelPerGuild[context.Guild.Id] = context.Channel.Id;
                return TypeReaderResult.FromSuccess(result);
            }
            
            result = StaticBase.Trackers[type].GetGuildTrackers(context.Guild.Id).FirstOrDefault(x => x.Name.Equals(input));

            if(result == null){
                await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(context.Channel.Id, StaticBase.Trackers[type].GetTrackersEmbed(context.Channel.Id, true));
                return TypeReaderResult.FromError(CommandError.ParseFailed, $"Could not find a {module}-tracker for {input}.");
            }

            var guildChannels = (await context.Guild.GetTextChannelsAsync()).Select(x => x.Id);
            var channelMatches = result.ChannelConfig.Keys.Where(x => guildChannels.Any(y => y.Equals(x)));
            if(channelMatches.Count() == 1){
                result.LastCalledChannelPerGuild[context.Guild.Id] = channelMatches.First();
                return TypeReaderResult.FromSuccess(result);
            }
            
            await MopsBot.Data.Interactive.MopsPaginator.CreatePagedMessage(context.Channel.Id, StaticBase.Trackers[type].GetTrackersEmbed(context.Channel.Id, true, input));
            return TypeReaderResult.FromError(CommandError.ParseFailed, $"Multiple trackers for {input} across multiple channels.\nPlease repeat the command in the channel containing the tracker you meant.");    
        }
    }
}