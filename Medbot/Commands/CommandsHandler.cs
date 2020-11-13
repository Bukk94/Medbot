﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Medbot.LoggingNS;
using Medbot.Followers;
using Medbot.ExpSystem;
using Medbot.Exceptions;
using Medbot.Internal;
using Medbot.Points;
using Medbot.Users;
using Medbot.Commands.Enums;

namespace Medbot.Commands
{
    internal static class CommandsHandler
    {
        private static ExperienceManager expObject;
        private static BotClient botClient;
        private static UsersManager _usersManager;
        private static BotDataManager _botDataManager;

        // TODO: !Followerinfo (or smth)

        /// <summary>
        /// Initializes Commands Handler, passing XP object
        /// </summary>
        /// <param name="exp">Object of Experiences class</param>
        internal static void Initialize(UsersManager usersManager, BotDataManager botDataManager, ExperienceManager exp, BotClient bot)
        {
            // TODO: Change this to regular constructor
            expObject = exp;
            botClient = bot;
            _usersManager = usersManager;
            _botDataManager = botDataManager;
        }

        /// <summary>
        /// Decides which handler should operate the command
        /// </summary>
        /// <param name="methodType">Method type</param>
        /// <param name="cmd">Command to execute</param>
        /// <param name="sender">User who sent the command</param>
        /// <param name="args">Command arguments</param>
        /// <returns>String result</returns>
        public static string ExecuteMethod(CommandType methodType, Command cmd, User sender, List<string> args)
        {
            string result = String.Empty;

            switch (methodType)
            {
                case CommandType.Points:
                    result = PointsHandler(sender, cmd, args);
                    break;
                case CommandType.EXP:
                    result = ExperienceHandler(sender, cmd, args);
                    break;
                case CommandType.Internal:
                    result = InternalHandler(sender, cmd, args).Result;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Method handling Points command distribution
        /// </summary>
        /// <param name="sender">User who sent the command</param>
        /// <param name="cmd">Command to execute</param>
        /// <param name="args">Command arguments</param>
        /// <returns>String result</returns>
        private static string PointsHandler(User sender, Command cmd, List<string> args)
        {
            switch (cmd.CommandHandlerType)
            {
                case CommandHandlerType.Info:
                    // Get points info !med |  0 input args
                    if (sender == null)
                    { // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which add points}
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), String.Format("{0} - Sender is null", cmd.CommandFormat));
                        return String.Format(cmd.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, "N/A", "N/A");
                    }

                    Logging.LogEvent(MethodBase.GetCurrentMethod(),
                                                 String.Format("{0}  Args: None # Points printed for user {1} # Value: {2}", cmd.CommandFormat, sender.DisplayName, sender.Points));

                    // Success: 4 params: {0:User} {1:Total} {2:Currency plural} {3:Currency units}
                    return String.Format(cmd.SuccessMessage, sender.DisplayName, sender.Points, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);

                case CommandHandlerType.Add:
                    // Add points,  !addhoney 50 Bukk94 |  2 input args {0:Number of points} {1:User to which add points}
                    User receiver = null;
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException("Command " + cmd.CommandFormat + "should contain some arguments. Found " + args.Count);

                        if (long.Parse(args[0]) <= 0)
                        { // Can't add 0 or negative amount
                            cmd.ResetCommandCooldown();
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
                        Logging.LogEvent(MethodBase.GetCurrentMethod(),
                                                 String.Format("{0}  Args: {1}, {2} - Points successfully added", cmd.CommandFormat, args[0], args[1]));

                        // Success: 4 params: {0:User} {1:Number of points} {2:Currency plural} {3:Currency units}
                        return String.Format(cmd.SuccessMessage, args[1], args[0], PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which add points} 
                        return String.Format(cmd.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], receiver != null ? receiver.DisplayName : args[1]);
                    }

                case CommandHandlerType.Remove:
                    //_Remove points, !removehoney 50 Bukk94 |  2 input args {0:Number of points} {1:User to which remove points}

                    User targetUser = null;
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException("Command " + cmd.CommandFormat + "should contain some arguments. Found " + args.Count);

                        if (long.Parse(args[0]) <= 0)
                        { // Can't remove 0 or negative amount
                            cmd.ResetCommandCooldown();
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

                        Logging.LogEvent(MethodBase.GetCurrentMethod(),
                                                 String.Format("{0}  Args: {1}, {2} - Points successfully removed", cmd.CommandFormat, args[0], args[1]));

                        // Success: 4 params: {0:User} {1:Number of points} {2:Currency plural} {3:Currency units}
                        return String.Format(cmd.SuccessMessage, args[1], args[0], PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());

                        // Fail: 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        return String.Format(cmd.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], targetUser != null ? targetUser.DisplayName : args[1]);
                    }

                case CommandHandlerType.Trade:
                    // Trading,  !trade {0} {1}   |  2 input args {0:Number of points} {1:User to which trade points}
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException("Command " + cmd.CommandFormat + "should contain some arguments. Found " + args.Count);
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        var target = _usersManager.FindOnlineUser(args[1]);
                        if (sender.Username.Equals(target.Username))
                        { // User is trying to send points to himself
                            cmd.ResetCommandCooldown();
                            return "";
                        }

                        _usersManager.Trade(long.Parse(args[0]), sender, target, args[1]);

                        Logging.LogEvent(MethodBase.GetCurrentMethod(),
                                                 String.Format("{0}  Args: {1}, {2} - Points successfully traded", cmd.CommandFormat, args[0], args[1]));

                        // Success: 6 params: {0:User} {1:Target User} {2:Number of points} {3:Currency units} {4:Currency name} {4: Currency plural}
                        return String.Format(cmd.SuccessMessage, sender.DisplayName, target != null ? target.DisplayName : args[1],
                                             args[0], PointsManager.CurrencyUnits, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural);
                    }
                    catch (PointsException ex)
                    {
                        // Fail 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        return String.Format(cmd.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], args[1]);
                    }
                    catch (Exception ex)
                    {
                        // Fail 5 params: {0:Currency name} {1:Currency plural} {2:Currency units} {3:Number of points} {4:User to which remove points} 
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        return String.Format(cmd.ErrorMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits, args[0], args[1]);
                    }

                case CommandHandlerType.Gamble:
                    try
                    {
                        if (args.Count < 1)
                            throw new IndexOutOfRangeException("Command " + cmd.CommandFormat + "should contain some arguments. Found " + args.Count);
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        int gambleValue = int.Parse(args[0]);
                        if (gambleValue > sender.Points)
                            throw new PointsException("User doesn't have enough points");

                        Random rand = new Random(Guid.NewGuid().GetHashCode());
                        int random = rand.Next(1, 100);

                        // 99-100 - triple reward
                        if (random > 100 - BotDictionary.GambleBonusWinPercentage)
                        {
                            sender.AddPoints(gambleValue * 3);
                            _usersManager.SaveData();
                            return String.Format(cmd.SuccessMessage, gambleValue * 3, PointsManager.CurrencyUnits, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural);
                        }
                        else if (random > 100 - BotDictionary.GambleWinPercentage - BotDictionary.GambleBonusWinPercentage)
                        { // 79-98 - double reward
                            sender.AddPoints(gambleValue * 2);
                            _usersManager.SaveData();
                            return String.Format(cmd.SuccessMessage, gambleValue * 2, PointsManager.CurrencyUnits, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural);
                        }

                        // User lost
                        sender.RemovePoints(gambleValue);
                        _usersManager.SaveData();
                        return String.Format(cmd.FailMessage, args[0], PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (PointsException ex)
                    {
                        // Fail 4 params: {0: Number of points} {1:Currency Name} {2:Currency plural} {3:Currency units}
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        return String.Format(cmd.ErrorMessage, args[0], PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        return "";
                    }
            }

            return "Unknown Points handler";
        }

        private static string ExperienceHandler(User sender, Command cmd, List<string> args)
        {
            switch (cmd.CommandHandlerType)
            {
                // !rank    | 0 input args
                case CommandHandlerType.Info:
                    try
                    {
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        // Success: 5 params: {0:User} {1:rank level} {2: rank name} {3: user's XP} {4: XP needed to next level}
                        return String.Format(cmd.SuccessMessage, sender.DisplayName,
                                             sender.UserRank.RankLevel, sender.UserRank.RankName,
                                             sender.Experience, sender.NextRank() != null ? sender.NextRank().ExpRequired.ToString() : "??");
                    }
                    catch (Exception ex)
                    {
                        // Fail: 4 params: {0:User} {1:rank level} {2: rank name} {3: user's XP}
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        if (sender == null)
                            return String.Format(cmd.FailMessage, "N/A", "N/A", "N/A", "N/A");

                        return String.Format(cmd.FailMessage, sender.DisplayName, sender.UserRank.RankLevel, sender.UserRank.RankName, sender.Experience);
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
                        return String.Format(cmd.SuccessMessage, sender.DisplayName,
                                            nextRank.RankLevel,
                                            nextRank.RankName,
                                            sender.ToNextRank(),
                                            sender.TimeToNextRank(expObject.ActiveExperience, expObject.ExperienceInterval));
                    }
                    catch (RanksException ex)
                    {
                        // Fail: 4 params: {0:User} {1:next rank level} {2:next rank name} {3: user's XP}
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        if (sender == null)
                            return String.Format(cmd.FailMessage, "N/A", "N/A", "N/A", "N/A");

                        Rank nextRank = sender.NextRank();
                        return String.Format(cmd.FailMessage, sender.DisplayName,
                                             nextRank != null ? nextRank.RankLevel.ToString() : "N/A",
                                             nextRank != null ? nextRank.RankName : "N/A", sender.Experience);
                    }
                    catch (Exception ex)
                    {
                        // Fail: 4 params: {0:User} {1:next rank level} {2:next rank name} {3: user's XP}
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        if (sender == null)
                            return String.Format(cmd.FailMessage, "N/A", "N/A", "N/A", "N/A");

                        Rank nextRank = sender.NextRank();
                        return String.Format(cmd.ErrorMessage, sender.DisplayName,
                                             nextRank != null ? nextRank.RankLevel.ToString() : "N/A",
                                             nextRank != null ? nextRank.RankName : "N/A", sender.Experience);
                    }

                case CommandHandlerType.Add:
                    // Add experience,  !addexp 500 Bukk94 |  2 input args {0:Number of points} {1:User to which add points}
                    User receiver = null;
                    try
                    {
                        if (args.Count < 2)
                            throw new IndexOutOfRangeException("Command " + cmd.CommandFormat + "should contain some arguments. Found " + args.Count);

                        if (long.Parse(args[0]) <= 0)
                        { // Can't add 0 or negative amount
                            cmd.ResetCommandCooldown();
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
                            if (newRank && !String.IsNullOrEmpty(BotDictionary.NewRankMessage))
                                botClient.SendChatMessage(String.Format(BotDictionary.NewRankMessage, receiver.DisplayName,
                                                                receiver.UserRank.RankLevel, receiver.UserRank.RankName));

                            _usersManager.SaveData();
                        }
                        Logging.LogEvent(MethodBase.GetCurrentMethod(),
                                                 String.Format("{0}  Args: {1}, {2} - Experience successfully added", cmd.CommandFormat, args[0], args[1]));

                        // Success: 2 params: {0:User} {1:Number of points}
                        return String.Format(cmd.SuccessMessage, args[1], args[0]);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());

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
        /// <param name="cmd">Command to execute</param>
        /// <param name="args">Command arguments</param>
        /// <returns>String result</returns>
        private static async Task<string> InternalHandler(User sender, Command cmd, List<string> args)
        {
            switch (cmd.CommandHandlerType)
            {
                case CommandHandlerType.LastFollower:
                    // Last follower, !lastfollower |  0 input args
                    try
                    {
                        Follow last = FollowersManager.GetNewestFollower(Login.Channel, Login.ClientID).Result;
                        if (last == null) // Fail: 0 params
                            throw new NullReferenceException("Last follower has not been found");

                        // Success: 3 params: {0:User} {1:Date} {2:Notification (bool)}
                        return String.Format(cmd.SuccessMessage, last.Follower.DisplayName, last.CreatedAt.ToShortDateString(), last.Notifications ? BotDictionary.Yes : BotDictionary.No);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        return cmd.FailMessage;
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
                                throw new IndexOutOfRangeException("Command " + cmd.CommandFormat + "should contain some arguments. Found " + args.Count);

                            var minutes = int.Parse(args[0]);
                            randomUser = _usersManager.SelectActiveRandomUser(int.Parse(args[0]));
                            totalUsers = _usersManager.GetActiveUsers(minutes).Count;
                        }

                        if (randomUser == null)
                            throw new NullReferenceException("No available users to select a random from.");

                        // Success: 1 param: {0:User} {1:number of people to draw}
                        return String.Format(cmd.SuccessMessage, randomUser.DisplayName, totalUsers);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), ex.ToString());
                        return cmd.FailMessage;
                    }

                case CommandHandlerType.Color:
                    // !color on  !color off    | input args 1 {1: state}
                    if (args.Count != 1)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), "ERROR while switching colors on/off. Arguments doesn't match");
                        return cmd.ErrorMessage;
                    }

                    if (args[0].ToLower().Equals("on"))
                    { // Turn on colors
                        if (botClient.UseColoredMessages)
                        { // already on
                            cmd.ResetCommandCooldown();
                            return "";
                        }

                        botClient.UseColoredMessages = true;
                        Logging.LogEvent(MethodBase.GetCurrentMethod(), "Colored message were turned ON");
                        return cmd.SuccessMessage;
                    }
                    else if (args[0].ToLower().Equals("off"))
                    { // Turn off colors
                        if (!botClient.UseColoredMessages)
                        { // already off
                            cmd.ResetCommandCooldown();
                            return "";
                        }

                        botClient.UseColoredMessages = false;
                        Logging.LogEvent(MethodBase.GetCurrentMethod(), "Colored message were turned OFF");
                        return cmd.FailMessage; // FailMessage is here used for turning off
                    }
                    else
                    { // Error
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), "ERROR while switching colors on/off. Arguments doesn't match");
                        return String.Format(cmd.ErrorMessage);
                    }

                case CommandHandlerType.ChangeColor:
                    // !color <name>
                    var match = Enum.GetNames(typeof(BotChatColors)).FirstOrDefault(color => color.ToLower().Equals(args[0].ToLower()));
                    if (match == null)
                        return "";

                    botClient.SendChatMessage($".color {match}", true);
                    return cmd.SuccessMessage;
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

                        return String.Format(cmd.SuccessMessage, String.Join(", ", usersCommands));
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
                            return String.Format(cmd.SuccessMessage, String.Join(", ", usersCommands));
                        }
                        else if (args[0].ToLower().Equals("streamer") || args[0].ToLower().Equals("broadcaster") || args[0].ToLower().Equals("owner"))
                        {
                            foreach (Command com in botClient.CommandsList)
                            {
                                if (!com.ModeratorPermissionRequired && com.BroadcasterOnly)
                                    usersCommands.Add(com.ToReadableFormat());
                            }
                            return String.Format(cmd.SuccessMessage, usersCommands.Count != 0 ? String.Join(", ", usersCommands) : "---");
                        }
                        else
                        { // args doesn't match, return empty
                            cmd.ResetCommandCooldown();
                            return cmd.FailMessage;
                        }
                    }
                    // there are more than 1 args, throw errro
                    Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), "ERROR while listing all commands. Arguments doesn't match");
                    return String.Format(cmd.ErrorMessage);

                case CommandHandlerType.Leaderboard:
                    // !leaderboard & !leaderboard {1}     | input args 0 or 1  {1: currency name / xp / level}
                    try
                    {
                        if (args.Count > 1)
                            throw new IndexOutOfRangeException("Command " + cmd.CommandFormat + "shouldn't containg more than 1 argument. Found " + args.Count);

                        if (args.Count == 0)
                        { // Form leaderboard with top 3 with points & XP
                            List<TempUser> fullPointsLeaderboard = _botDataManager.GetPointsLeaderboard();
                            List<TempUser> fullXPLeaderboard = _botDataManager.GetExperienceLeaderboard();

                            if (fullPointsLeaderboard.Count <= 0 || fullXPLeaderboard.Count <= 0)
                                throw new PointsException("Leaderboard doesn't contain any records");

                            // Success 3 params - {0: currency plural} {1: list of top points users} {2: list of top XP users} 
                            return String.Format(cmd.SuccessMessage, PointsManager.CurrencyNamePlural,
                                                 String.Join(", ", FormLeaderboard(fullPointsLeaderboard)),
                                                 String.Join(", ", FormLeaderboard(fullXPLeaderboard)),
                                                 BotDictionary.LeaderboardTopNumber);
                        }
                        else
                        { // Form specific leaderboard
                            if (args[0].ToLower().Equals(PointsManager.CurrencyName.ToLower()) || args[0].ToLower().Equals("points"))
                            {
                                List<TempUser> fullLeaderboard = _botDataManager.GetPointsLeaderboard();
                                if (fullLeaderboard.Count <= 0)
                                    throw new PointsException("Leaderboard doesn't contain any records");

                                // Success 2 params - {0: currency plural} {1: list of top users} 
                                return String.Format(cmd.SuccessMessage, PointsManager.CurrencyNamePlural, String.Join(", ", FormLeaderboard(fullLeaderboard)), BotDictionary.LeaderboardTopNumber);
                            }
                            else if (args[0].ToLower().Equals("xp") || args[0].ToLower().Equals("exp") || args[0].ToLower().Equals("level"))
                            {
                                List<TempUser> fullLeaderboard = _botDataManager.GetExperienceLeaderboard();
                                if (fullLeaderboard.Count <= 0)
                                    throw new PointsException("Leaderboard doesn't contain any records");

                                // Success 2 params - {0: xp} {1: list of top users} 
                                return String.Format(cmd.SuccessMessage, "xp", String.Join(", ", FormLeaderboard(fullLeaderboard)), BotDictionary.LeaderboardTopNumber);
                            }
                            else
                            { // Arguments doens't match
                                cmd.ResetCommandCooldown();
                                // Fail: 1 param - {0: currency name} {1: currency plural} {2: currency units}
                                return String.Format(cmd.FailMessage, PointsManager.CurrencyName, PointsManager.CurrencyNamePlural, PointsManager.CurrencyUnits);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), "ERROR while forming leaderboard!\n" + ex.ToString());
                        return cmd.ErrorMessage;
                    }
                case CommandHandlerType.FollowAge:
                    // !followage    | input args 0
                    try
                    {
                        if (sender == null)
                            throw new NullReferenceException("Something went wrong, sender is null");

                        Follower followerData = await FollowersManager.GetFollowerFollowsInfo(Login.Channel, "bukk94", Login.ClientID);
                        if (followerData == null)
                            return "";

                        TimeSpan age = DateTime.Now - followerData.CreatedAt;
                        //TimeSpan age = DateTime.Now - new DateTime(2017, 3, 1, 5, 30, 20, 1);

                        // Success 2 params - {0: user/sender} {1: number of days} 
                        string ageFormatting = String.Empty;
                        if (age.TotalDays > 365)
                            ageFormatting += Math.Round(age.TotalDays / 365) + "Y ";

                        ageFormatting += Math.Round(age.TotalDays) + "D";

                        return String.Format(cmd.SuccessMessage, sender.Username, ageFormatting);
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(CommandsHandler), MethodBase.GetCurrentMethod(), "ERROR while getting follower's age\n" + ex.ToString());
                        return cmd.ErrorMessage;
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

        private static List<string> FormLeaderboard(List<TempUser> fullLeaderboard)
        {
            List<string> leaderboard = new List<string>();

            foreach (TempUser u in fullLeaderboard.Take(BotDictionary.LeaderboardTopNumber))
            {
                leaderboard.Add(String.Format("{0} ({1})", u.Username, u.Data));
            }

            return leaderboard;
        }
    }
}
