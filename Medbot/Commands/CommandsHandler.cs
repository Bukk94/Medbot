using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medbot.Followers;
using Medbot.ExpSystem;
using Medbot.Exceptions;
using Medbot.Internal;
using Medbot.Points;
using Medbot.Users;
using Medbot.Enums;
using Microsoft.Extensions.Logging;

namespace Medbot.Commands
{
    internal class CommandsHandler
    {
        private readonly ILogger _logger;
        private readonly BotClient botClient;
        private readonly ExperienceManager _experienceManager;
        private readonly UsersManager _usersManager;
        private readonly BotDataManager _botDataManager;
        private readonly FollowersManager _followersManager;

        // TODO: Stop using BotClient object
        public CommandsHandler(UsersManager usersManager, BotDataManager botDataManager, ExperienceManager experienceManager, BotClient bot)
        {
            _logger = Logging.GetLogger<CommandsHandler>();

            botClient = bot;
            _experienceManager = experienceManager;
            _usersManager = usersManager;
            _botDataManager = botDataManager;

            _followersManager = new FollowersManager();
        }

        /// <summary>
        /// Executes specific command
        /// </summary>
        /// <param name="commandToExecute">Command to execute</param>
        /// <param name="sender">User who executed the command</param>
        /// <param name="args">List of parsed command arguments</param>
        /// <returns>Informative string</returns>
        internal string ExecuteCommand(Command commandToExecute, User sender, List<string> args)
        {
            if (commandToExecute.IsUserAllowedToExecute(sender))
                return ExecuteCommandInternal(commandToExecute, sender, args);

            _logger.LogInformation("User {sender} tried to execute command {command} without having permissions to do so!", sender.DisplayName, commandToExecute.CommandFormat);
            return _botDataManager.BotDictionary.InsufficientPermissions;
        }

        /// <summary>
        /// Decides which handler should operate the command
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="sender">User who sent the command</param>
        /// <param name="args">List of command arguments</param>
        /// <returns>String result</returns>
        private string ExecuteCommandInternal(Command command, User sender, List<string> args)
        {
            string result = String.Empty;

            switch (command.CommandType)
            {
                case CommandType.Points:
                    result = PointsHandler(sender, command, args);
                    break;
                case CommandType.EXP:
                    result = ExperienceHandler(sender, command, args);
                    break;
                case CommandType.Internal:
                    result = Task.Run(() => InternalHandler(sender, command, args)).Result;
                    break;
            }

            _logger.LogInformation("Command {command} executed by {user}.", command.CommandFormat, sender.DisplayName);
            return result;
        }

        /// <summary>
        /// Method handling Points command distribution
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="command">Command to execute</param>
        /// <param name="args">List of command arguments</param>
        /// <returns>String result</returns>
        private string PointsHandler(User sender, Command command, List<string> args)
        {
            switch (command.CommandHandlerType)
            {
                case CommandHandlerType.Info:
                    // Get points info !med |  0 input args
                    if (sender == null)
                    { // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which add points}
                        _logger.LogError("Failed to execute command {command}. Sender is null!", command.CommandFormat);
                        return String.Format(command.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, "N/A", "N/A");
                    }

                    // Success: 4 params: {0:User} {1:Total} {2:Currency plural} {3:Currency units}
                    return String.Format(command.SuccessMessage, sender.DisplayName, sender.Points, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);

                case CommandHandlerType.Add:
                    // Add points,  !addhoney 50 Bukk94 |  2 input args {0:Number of points} {1:User to which add points}
                    User receiver = null;
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException($"Command {command.CommandFormat} should contain some arguments. Found {args.Count}.");

                        if (long.Parse(args[0]) <= 0)
                        { // Can't add 0 or negative amount
                            command.ResetCommandCooldown();
                            return "";
                        }

                        receiver = _usersManager.FindOnlineUser(args[1]);
                        if (receiver == null)
                        { // user is not online
                            _botDataManager.AddUserPointsToFile(args[1], long.Parse(args[0]));
                        }
                        else
                        {
                            receiver.AddPoints(long.Parse(args[0]));
                            _usersManager.SaveData();
                        }

                        _logger.LogInformation("Sucessfully added {total} points to {sender}.", args[0], args[1]);

                        // Success: 4 params: {0:User} {1:Number of points} {2:Currency plural} {3:Currency units}
                        return String.Format(command.SuccessMessage, args[1], args[0], PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred during points addition.\n{ex}", ex);

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which add points} 
                        return String.Format(command.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], receiver != null ? receiver.DisplayName : args[1]);
                    }

                case CommandHandlerType.Remove:
                    //_Remove points, !removehoney 50 Bukk94 |  2 input args {0:Number of points} {1:User to which remove points}

                    User targetUser = null;
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException($"Command {command.CommandFormat} should contain some arguments. Found {args.Count}.");

                        if (long.Parse(args[0]) <= 0)
                        { // Can't remove 0 or negative amount
                            command.ResetCommandCooldown();
                            return "";
                        }

                        targetUser = _usersManager.FindOnlineUser(args[1]);
                        if (targetUser == null)
                        { // user is not online
                            _botDataManager.RemoveUserPointsFromFile(args[1], long.Parse(args[0]));
                        }
                        else
                        {
                            targetUser.RemovePoints(long.Parse(args[0]));
                            _usersManager.SaveData();
                        }

                        _logger.LogInformation("Sucessfully removed {total} points from {sender}.", args[0], args[1]);

                        // Success: 4 params: {0:User} {1:Number of points} {2:Currency plural} {3:Currency units}
                        return String.Format(command.SuccessMessage, args[1], args[0], PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred during points removal.\n{ex}", ex);

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        return String.Format(command.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], targetUser != null ? targetUser.DisplayName : args[1]);
                    }

                case CommandHandlerType.Trade:
                    // Trading,  !trade {0} {1}   |  2 input args {0:Number of points} {1:User to which trade points}
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException($"Command {command.CommandFormat} should contain some arguments. Found {args.Count}.");

                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        var target = _usersManager.FindOnlineUser(args[1]);
                        if (sender.Username.Equals(target.Username))
                        { // User is trying to send points to himself
                            command.ResetCommandCooldown();
                            return "";
                        }

                        _usersManager.Trade(long.Parse(args[0]), sender, target, args[1]);

                        _logger.LogInformation("{sender} successfully traded {count} points to {target}", sender.DisplayName, args[0], args[1]);

                        // Success: 6 params: {0:User} {1:Target User} {2:Number of points} {3:Currency units} {4:Currency name} {4: Currency plural}
                        return String.Format(command.SuccessMessage, sender.DisplayName, target != null ? target.DisplayName : args[1],
                                             args[0], PointsManager.CurrencyUnits, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural);
                    }
                    catch (PointsException ex)
                    {
                        // Fail 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        _logger.LogError("Error occurred during points trading.\n{ex}", ex);
                        return String.Format(command.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], args[1]);
                    }
                    catch (Exception ex)
                    {
                        // Fail 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        _logger.LogError("Critical error occurred during points trading.\n{ex}", ex);
                        return String.Format(command.ErrorMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], args[1]);
                    }

                case CommandHandlerType.Gamble:
                    try
                    {
                        if (args.Count < 1)
                            throw new IndexOutOfRangeException($"Command {command.CommandFormat} should contain some arguments. Found {args.Count}.");
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        int gambleValue = int.Parse(args[0]);
                        if (gambleValue > sender.Points)
                            throw new PointsException("User doesn't have enough points!");

                        Random rand = new Random(Guid.NewGuid().GetHashCode());
                        int random = rand.Next(1, 100);

                        // 99-100 - triple reward
                        if (random > 100 - _botDataManager.BotSettings.GambleBonusWinPercentage)
                        {
                            sender.AddPoints(gambleValue * 3);
                            _usersManager.SaveData();
                            return String.Format(command.SuccessMessage, gambleValue * 3, PointsManager.CurrencyUnits, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural);
                        }
                        else if (random > 100 - _botDataManager.BotSettings.GambleWinPercentage - _botDataManager.BotSettings.GambleBonusWinPercentage)
                        { // 79-98 - double reward
                            sender.AddPoints(gambleValue * 2);
                            _usersManager.SaveData();
                            return String.Format(command.SuccessMessage, gambleValue * 2, PointsManager.CurrencyUnits, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural);
                        }

                        // User lost
                        sender.RemovePoints(gambleValue);
                        _usersManager.SaveData();
                        return String.Format(command.FailMessage, args[0], PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (PointsException ex)
                    {
                        // Fail 4 params: {0: Number of points} {1:Currency Name} {2:Currency plural} {3:Currency units}
                        _logger.LogError("Error occurred during gamble command.\n{ex}", ex);
                        return String.Format(command.ErrorMessage, args[0], PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred during gamble command.\n{ex}", ex);
                        return "";
                    }
            }

            return "Unknown Points handler";
        }

        private string ExperienceHandler(User sender, Command command, List<string> args)
        {
            switch (command.CommandHandlerType)
            {
                // !rank    | 0 input args
                case CommandHandlerType.Info:
                    try
                    {
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        // Success: 5 params: {0:User} {1:rank level} {2: rank name} {3: user's XP} {4: XP needed to next level}
                        return String.Format(command.SuccessMessage, sender.DisplayName,
                                             sender.UserRank.RankLevel, sender.UserRank.RankName,
                                             sender.Experience, sender.NextRank() != null ? sender.NextRank().ExpRequired.ToString() : "??");
                    }
                    catch (Exception ex)
                    {
                        // Fail: 4 params: {0:User} {1:rank level} {2: rank name} {3: user's XP}
                        _logger.LogError("Critical error occurred during rank command.\n{ex}", ex);

                        if (sender == null)
                            return String.Format(command.FailMessage, "N/A", "N/A", "N/A", "N/A");

                        return String.Format(command.FailMessage, sender.DisplayName, sender.UserRank.RankLevel, sender.UserRank.RankName, sender.Experience);
                    }
                case CommandHandlerType.InfoSecond:
                    // !nextrank   | 0 input args
                    try
                    {
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        Rank nextRank = sender.NextRank();
                        if (nextRank == null)
                            throw new RanksException("User's next rank is null");

                        // Success: 5 params: {0:User} {1:next rank level} {2:next rank name} {3: XP needed to next level} {4: time to next rank}
                        return String.Format(command.SuccessMessage, sender.DisplayName,
                                            nextRank.RankLevel,
                                            nextRank.RankName,
                                            sender.ToNextRank(),
                                            sender.TimeToNextRank(_experienceManager.ActiveExperienceReward, _experienceManager.ExperienceTickInterval));
                    }
                    catch (RanksException ex)
                    {
                        // Fail: 4 params: {0:User} {1:next rank level} {2:next rank name} {3: user's XP}
                        _logger.LogError("Error occurred during rank information command.\n{ex}", ex);

                        if (sender == null)
                            return String.Format(command.FailMessage, "N/A", "N/A", "N/A", "N/A");

                        Rank nextRank = sender.NextRank();
                        return String.Format(command.FailMessage, sender.DisplayName,
                                             nextRank != null ? nextRank.RankLevel.ToString() : "N/A",
                                             nextRank != null ? nextRank.RankName : "N/A", sender.Experience);
                    }
                    catch (Exception ex)
                    {
                        // Fail: 4 params: {0:User} {1:next rank level} {2:next rank name} {3: user's XP}
                        _logger.LogError("Critical error occurred during rank information command.\n{ex}", ex);

                        if (sender == null)
                            return String.Format(command.FailMessage, "N/A", "N/A", "N/A", "N/A");

                        Rank nextRank = sender.NextRank();
                        return String.Format(command.ErrorMessage, sender.DisplayName,
                                             nextRank != null ? nextRank.RankLevel.ToString() : "N/A",
                                             nextRank != null ? nextRank.RankName : "N/A", sender.Experience);
                    }

                case CommandHandlerType.Add:
                    // Add experience,  !addexp 500 Bukk94 |  2 input args {0:Number of points} {1:User to which add points}
                    User receiver = null;
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException($"Command {command.CommandFormat} should contain some arguments. Found {args.Count}.");

                        if (long.Parse(args[0]) <= 0)
                        { // Can't add 0 or negative amount
                            command.ResetCommandCooldown();
                            return "";
                        }

                        receiver = _usersManager.FindOnlineUser(args[1]);
                        if (receiver == null)
                        { // user is not online
                            _botDataManager.AddUserExperienceToFile(args[1], long.Parse(args[0]));
                        }
                        else
                        {
                            receiver.AddExperience(long.Parse(args[0]));
                            bool newRank = receiver.CheckRankUp();
                            if (newRank && !String.IsNullOrEmpty(_botDataManager.BotDictionary.NewRankMessage))
                                botClient.SendChatMessage(String.Format(_botDataManager.BotDictionary.NewRankMessage, receiver.DisplayName,
                                                                receiver.UserRank.RankLevel, receiver.UserRank.RankName));

                            _usersManager.SaveData();
                        }

                        _logger.LogInformation("Sucessfully added {total} experience points to {sender}.", args[0], args[1]);

                        // Success: 2 params: {0:User} {1:Number of points}
                        return String.Format(command.SuccessMessage, args[1], args[0]);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred during experienece addition command.\n{ex}", ex);

                        // Fail: 1 param: {:User to which add points} 
                        return String.Format(receiver != null ? receiver.DisplayName : args[1]);
                    }
            }


            return "Unknown experience handler";
        }

        /// <summary>
        /// Method handling internal command distribution
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="command">Command to execute</param>
        /// <param name="args">List of command arguments</param>
        /// <returns>String result</returns>
        private async Task<string> InternalHandler(User sender, Command command, List<string> args)
        {
            switch (command.CommandHandlerType)
            {
                case CommandHandlerType.LastFollower:
                    // Last follower, !lastfollower |  0 input args
                    try
                    {
                        Follower last = await _followersManager.GetNewestFollower(_botDataManager.Login.ChannelId);
                        if (last == null) // Fail: 0 params
                            throw new NullReferenceException("Last follower has not been found");

                        // Success: 2 params: {0:User} {1:Date}
                        return String.Format(command.SuccessMessage, last.FollowerName, last.FollowedAt.ToShortDateString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred during last follower command.\n{ex}", ex);

                        return command.FailMessage;
                    }

                case CommandHandlerType.Random:
                    // Selects random user,  !random   !random {0}  | 0 or 1 input args {0: Active past X minutes}
                    try
                    {
                        User randomUser = null;
                        int totalUsers = 0;
                        if (args.Count == 0)
                        {
                            // Regular random
                            randomUser = _usersManager.SelectRandomUser();
                            totalUsers = _usersManager.TotalUsersOnline;
                        }
                        else
                        {
                            // Active users random
                            if (args.Count <= 0)
                                throw new IndexOutOfRangeException("Command " + command.CommandFormat + "should contain some arguments. Found " + args.Count);

                            var minutes = int.Parse(args[0]);
                            randomUser = _usersManager.SelectActiveRandomUser(int.Parse(args[0]));
                            totalUsers = _usersManager.GetActiveUsers(minutes).Count;
                        }

                        if (randomUser == null)
                            throw new NullReferenceException("No available users to select a random from.");

                        // Success: 1 param: {0:User} {1:number of people to draw}
                        return String.Format(command.SuccessMessage, randomUser.DisplayName, totalUsers);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred during random command.\n{ex}", ex);
                        return command.FailMessage;
                    }

                case CommandHandlerType.Color:
                    // !color on  !color off    | input args 1 {1: state}
                    if (args.Count != 1)
                    {
                        _logger.LogError("Error occurred while switching colors on/off. Arguments doesn't match!");
                        return command.ErrorMessage;
                    }

                    if (args[0].ToLower().Equals("on"))
                    { // Turn on colors
                        if (botClient.UseColoredMessages)
                        { // already on
                            command.ResetCommandCooldown();
                            return "";
                        }

                        botClient.UseColoredMessages = true;
                        _logger.LogInformation("Bot colored messages were turned ON.");
                        return command.SuccessMessage;
                    }
                    else if (args[0].ToLower().Equals("off"))
                    { // Turn off colors
                        if (!botClient.UseColoredMessages)
                        { // already off
                            command.ResetCommandCooldown();
                            return "";
                        }

                        botClient.UseColoredMessages = false;
                        _logger.LogInformation("Bot colored messages were turned OFF.");
                        return command.FailMessage; // FailMessage is here used for turning off
                    }
                    else
                    { // Error
                        _logger.LogError("Error occurred while switching colors on/off. Arguments doesn't match!");
                        return String.Format(command.ErrorMessage);
                    }

                case CommandHandlerType.ChangeColor:
                    // !color <name>
                    var match = Enum.GetNames(typeof(BotChatColors)).FirstOrDefault(color => color.ToLower().Equals(args[0].ToLower()));
                    if (match == null)
                        return "";

                    botClient.SendChatMessage($".color {match}", true);
                    return command.SuccessMessage;
                case CommandHandlerType.All:
                    // !medbot   !medbot mod  !medbot streamer         |  0 or 1 input args {1: mod/streamer}
                    if (args.Count == 0)
                    {
                        // Commands for normal users

                        List<string> usersCommands = new List<string>();
                        foreach (Command com in botClient.CommandsList)
                        {
                            if (!com.ModeratorPermissionRequired && !com.BroadcasterOnly)
                                usersCommands.Add(com.ToReadableFormat());
                        }

                        return String.Format(command.SuccessMessage, String.Join(", ", usersCommands));
                    }
                    else if (args.Count == 1)
                    {
                        // Commands for mods/streamers
                        List<string> usersCommands = new List<string>();

                        if (args[0].ToLower().Equals("mod") || args[0].ToLower().Equals("moderator"))
                        {
                            foreach (Command com in botClient.CommandsList)
                            {
                                if (com.ModeratorPermissionRequired && !com.BroadcasterOnly)
                                    usersCommands.Add(com.ToReadableFormat());
                            }
                            return String.Format(command.SuccessMessage, String.Join(", ", usersCommands));
                        }
                        else if (args[0].ToLower().Equals("streamer") || args[0].ToLower().Equals("broadcaster") || args[0].ToLower().Equals("owner"))
                        {
                            foreach (Command com in botClient.CommandsList)
                            {
                                if (!com.ModeratorPermissionRequired && com.BroadcasterOnly)
                                    usersCommands.Add(com.ToReadableFormat());
                            }
                            return String.Format(command.SuccessMessage, usersCommands.Count != 0 ? String.Join(", ", usersCommands) : "---");
                        }
                        else
                        { // args doesn't match, return empty
                            command.ResetCommandCooldown();
                            return command.FailMessage;
                        }
                    }
                    // there are more than 1 args, throw error
                    _logger.LogError("Error occurred while listing all commands. Arguments doesn't match!");
                    return String.Format(command.ErrorMessage);

                case CommandHandlerType.Leaderboard:
                    // !leaderboard & !leaderboard {1}     | input args 0 or 1  {1: currency name / xp / level}
                    try
                    {
                        if (args.Count > 1)
                            throw new IndexOutOfRangeException("Command " + command.CommandFormat + "shouldn't containg more than 1 argument. Found " + args.Count);

                        if (args.Count == 0)
                        { // Form leaderboard with top 3 with points & XP
                            List<TempUser> fullPointsLeaderboard = _botDataManager.GetPointsLeaderboard();
                            List<TempUser> fullXPLeaderboard = _botDataManager.GetExperienceLeaderboard();

                            if (fullPointsLeaderboard.Count <= 0 || fullXPLeaderboard.Count <= 0)
                                throw new PointsException("Leaderboard doesn't contain any records");

                            // Success 3 params - {0: currency plural} {1: list of top points users} {2: list of top XP users} 
                            return String.Format(command.SuccessMessage, PointsManager.CurrencyNamePlural,
                                                 String.Join(", ", FormLeaderboard(fullPointsLeaderboard)),
                                                 String.Join(", ", FormLeaderboard(fullXPLeaderboard)),
                                                 _botDataManager.BotSettings.LeaderboardTopNumber);
                        }
                        else
                        { // Form specific leaderboard
                            if (args[0].ToLower().Equals(PointsManager.CurrencyName.ToLower()) || args[0].ToLower().Equals("points"))
                            {
                                List<TempUser> fullLeaderboard = _botDataManager.GetPointsLeaderboard();
                                if (fullLeaderboard.Count <= 0)
                                    throw new PointsException("Leaderboard doesn't contain any records");

                                // Success 2 params - {0: currency plural} {1: list of top users} 
                                return String.Format(command.SuccessMessage, PointsManager.CurrencyNamePlural, String.Join(", ", FormLeaderboard(fullLeaderboard)), _botDataManager.BotSettings.LeaderboardTopNumber);
                            }
                            else if (args[0].ToLower().Equals("xp") || args[0].ToLower().Equals("exp") || args[0].ToLower().Equals("level"))
                            {
                                List<TempUser> fullLeaderboard = _botDataManager.GetExperienceLeaderboard();
                                if (fullLeaderboard.Count <= 0)
                                    throw new PointsException("Leaderboard doesn't contain any records");

                                // Success 2 params - {0: xp} {1: list of top users} 
                                return String.Format(command.SuccessMessage, "xp", String.Join(", ", FormLeaderboard(fullLeaderboard)), _botDataManager.BotSettings.LeaderboardTopNumber);
                            }
                            else
                            { // Arguments doens't match
                                command.ResetCommandCooldown();
                                // Fail: 1 param - {0: currency name} {1: currency plural} {2: currency units}
                                return String.Format(command.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred while forming leaderboard.\n{ex}", ex);

                        return command.ErrorMessage;
                    }
                case CommandHandlerType.FollowAge:
                    // !followage    | input args 0
                    try
                    {
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        var followDate = await _followersManager.GetFollowDate(_botDataManager.Login.ChannelId, sender);
                        if (followDate == null)
                            return "";

                        TimeSpan age = DateTime.Now - followDate.Value;
                        //TimeSpan age = DateTime.Now - new DateTime(2017, 3, 1, 5, 30, 20, 1);

                        // Success 2 params - {0: user/sender} {1: number of days} 
                        string ageFormatting = String.Empty;

                        int numberOfYears = 0;
                        if (age.TotalDays > 365)
                        {
                            numberOfYears = (int)Math.Round(age.TotalDays / 365);
                            ageFormatting += numberOfYears + "Y ";
                        }

                        ageFormatting += Math.Round(age.TotalDays - (numberOfYears * 365)) + "D";

                        return String.Format(command.SuccessMessage, sender.Username, ageFormatting);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Critical error occurred while getting follower's age.\n{ex}", ex);

                        return command.ErrorMessage;
                    }

                case CommandHandlerType.Help:
                    Command matchedCommand = botClient.CommandsList.FirstOrDefault(c => c.CommandFormat.Contains(args[0]));
                    if (matchedCommand == null)
                        return "";

                    // {0: Points name} {1: Points plural} {2: Points units}
                    return String.Format(matchedCommand.AboutMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
            }

            return "Unknown internal handler";
        }

        private List<string> FormLeaderboard(List<TempUser> fullLeaderboard)
        {
            List<string> leaderboard = new List<string>();

            foreach (TempUser u in fullLeaderboard.Take(_botDataManager.BotSettings.LeaderboardTopNumber))
            {
                leaderboard.Add(String.Format("{0} ({1})", u.Username, u.Data));
            }

            return leaderboard;
        }
    }
}
