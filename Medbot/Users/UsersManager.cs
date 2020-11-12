using Medbot.Events;
using Medbot.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medbot.Users
{
    internal class UsersManager
    {
        private FilesControl _filesControl;

        /// <summary>
        /// List of currently online users
        /// </summary>
        public List<User> OnlineUsers { get; private set; }

        /// <summary>
        /// List of usernames that are blacklisted from receiveing points, EXP, and ranks
        /// </summary>
        public List<string> UserBlacklist { get; set; }

        public bool IsAnyUserOnline => OnlineUsers.Any();

        public int TotalUsersOnline => OnlineUsers.Count;

        #region Events
        /// <summary>
        /// Activates when user joines the channel
        /// </summary>
        public event EventHandler<OnUserArgs> OnUserJoined;

        /// <summary>
        /// Activates when user disconnects from the channel
        /// </summary>
        public event EventHandler<OnUserArgs> OnUserDisconnected;
        #endregion

        public UsersManager()
        {
            OnlineUsers = new List<User>();
            UserBlacklist = new List<string>();
        }

        internal void Initialize(FilesControl filesControl)
        {
            _filesControl = filesControl;
        }

        /// <summary>
        /// Tries to add user to Online List, if already exists, find him and return
        /// </summary>
        /// <param name="chatLine">Full chat line</param>
        /// <returns>Sender user</returns>
        public User JoinUser(string chatLine)
        {
            string userName = Parsing.ParseUsername(chatLine);

            if (!OnlineUsers.Any(x => x.Username == userName))
            { // User is not in the list
                var newUser = new User(userName);

                _filesControl.LoadUserData(ref newUser);
                OnlineUsers.Add(newUser);

                OnUserJoined?.Invoke(this, new OnUserArgs { User = newUser });
                Console.WriteLine("User " + userName + " JOINED");
                return newUser;
            }

            return FindOnlineUser(userName);
        }

        /// <summary>
        /// User disconnected, remove from OnlineUsers and save all points
        /// </summary>
        /// <param name="user"></param>
        public void DisconnectUser(string user)
        {
            var disconnectingUser = FindOnlineUser(user);
            if (disconnectingUser == null)
                return;

            _filesControl.SaveData();
            // TODO: Pass object to remove method?
            OnlineUsers.RemoveAll(x => x.Username == user);
            OnUserDisconnected?.Invoke(this, new OnUserArgs { User = disconnectingUser });
            Console.WriteLine("User " + user + " LEFT");
        }

        /// <summary>
        /// Finds user from Online Users list by its name
        /// </summary>
        /// <param name="username">Username to find</param>
        /// <returns>User object or null</returns>
        public User FindOnlineUser(string username)
        {
            return OnlineUsers.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsUserBlacklisted(User user)
        {
            return UserBlacklist.Contains(user.Username);
        }

        /// <summary>
        /// Clears list of online users
        /// </summary>
        public void ClearOnlineUsers()
        {
            OnlineUsers.Clear();
        }

        public List<User> GetActiveUsers(int minutes)
        {
            return OnlineUsers.Where(x => x.LastMessage.HasValue &&
                                          DateTime.Now - x.LastMessage < TimeSpan.FromMinutes(minutes))
                              .ToList();
        }

        /// <summary>
        /// Selects random online user
        /// </summary>
        /// <returns>Returns random user</returns>
        public User SelectRandomUser()
        {
            return SelectRandomUser(OnlineUsers);
        }

        /// <summary>
        /// Selects random active online user
        /// </summary>
        /// <param name="minutes">Minutes for active user definition</param>
        /// <returns>Returns random user</returns>
        public User SelectActiveRandomUser(int minutes)
        {
            return SelectRandomUser(GetActiveUsers(minutes));
        }

        private User SelectRandomUser(List<User> usersList)
        {
            if (usersList.Count() <= 0)
                return null;
            else if (usersList.Count() == 1)
                return usersList.First();

            Random rand = new Random(Guid.NewGuid().GetHashCode());
            int random = rand.Next(0, usersList.Count());
            return usersList.ElementAt(random);
        }
    }
}
