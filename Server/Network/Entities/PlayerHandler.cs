using Final_Aisle_Shared.Network;
using System.Collections.Generic;
using System.Linq;

namespace Final_Aisle_Server.Network.Entities
{
    /// <summary>
    /// Static class used for relaying information about player movements/actions to relevant clients.
    /// </summary>
    public static class PlayerHandler
    {
        /// <summary>
        /// List of users that are currently <b>in-game</b> (meaning a character has been selected).
        /// </summary>
        public static List<NetworkUser> OnlineUsers = new List<NetworkUser>();
        
        public static void MovementInput(NetworkUser user, float moveX, float moveY) => SendToOthers(user, PacketDataUtils.MovementInput, new List<object> { user.ID, moveX, moveY });
        public static void MovementRoll(NetworkUser user, float rotation) => SendToOthers(user, PacketDataUtils.MovementRoll, new List<object> { user.ID, rotation });
        public static void MovementJump(NetworkUser user) => SendToOthers(user, PacketDataUtils.MovementJump, user.ID);
        public static void MovementToggleProne(NetworkUser user, bool proneState) => SendToOthers(user, PacketDataUtils.MovementToggleProne, new List<object> { user.ID, proneState });
        
        public static void TransformPosition(NetworkUser user, float posX, float posY, float posZ)
        {
            /*
             * TODO: Perform player position verification here, for security purposes.
             * - Can the player have gotten to this position so quickly from the previous position?
             * - Can the player legally be here (not stuck in the ground, for example)?
             */

            SendToOthers(user, PacketDataUtils.TransformPosition, new List<object> { user.ID, posX, posY, posZ });
        }

        public static void TransformRotation(NetworkUser user, float rotation) => SendToOthers(user, PacketDataUtils.TransformRotation, new List<object> { user.ID, rotation });

        /// <summary>
        /// Officially logs in the player (with an active character), adding them to the list of online players
        /// </summary>
        public static void LogIn(NetworkUser user)
        {
            ConsoleLog.WriteBigIO(user.UserAccount.Username + " has successfully logged in from " + user.Connection.Socket.RemoteEndPoint);
            user.LoggedIn = true;

            OnlineUsers.Add(user);
            SendToOthers(user, PacketDataUtils.PlayerConnected, (List<object>)new List<object> { user.ID }.Concat(((IPacketSerializable)user.UserAccount.GetActiveCharacter()).GetSerializableComponents())); // tell existing players that a new player joined
        }

        /// <summary>
        /// Call this method after the player has successfully logged in to (client-side) register all already-registered players into the new player's game.
        /// This is necessary for newly-joined players to be able to see other players that were already online.
        /// </summary>
        /// <param name="user"></param>
        public static void PostLogIn(NetworkUser user)
        {
            foreach (var u in OnlineUsers)
            {
                // Don't send the player's data to themselves
                if (u != user)
                {
                    Program.SendComposite(user, PacketDataUtils.PlayerConnected, (List<object>)new List<object> { u.ID.ToString() }.Concat(((IPacketSerializable)u.UserAccount.GetActiveCharacter()).GetSerializableComponents())); // tell new player of all existing players
                }
            }
        }

        /// <summary>
        /// Will log out the given player if he/she is already logged in.
        /// </summary>
        public static void LogOut(NetworkUser user)
        {
            user.LoggedIn = false;
            
            if (PlayerHandler.OnlineUsers.Contains(user))
            {
                PlayerHandler.OnlineUsers.Remove(user);
                ConsoleLog.WriteBigIO(user.UserAccount.Username + " has logged out.");
            }

            // Inform all existing players that a player has left
            foreach (NetworkUser u in OnlineUsers)
            {
                Program.SendInt(u, PacketDataUtils.PlayerDisconnected, user.ID);
            }
        }

        /// <summary>
        /// Sends the given message to all players that are not the given user sender.
        /// </summary>
        private static void SendToOthers(NetworkUser sender, string key, object message)
        {
            foreach (var u in OnlineUsers)
            { 
                // Ensure that this is not the same user; don't send to self!
                if (u.ID != sender.ID)
                {
                    if (message is bool boolean)
                    {
                        Program.SendBool(u, key, boolean);
                    }
                    else if (message is List<object> list)
                    {
                        Program.SendComposite(u, key, list);
                    }
                    else if (message is double @double)
                    {
                        Program.SendDouble(u, key, @double);
                    }
                    else if (message is float @float)
                    {
                        Program.SendFloat(u, key, @float);
                    }
                    else if (message is int @int)
                    {
                        Program.SendInt(u, key, @int);
                    }
                    else if (message is string @string)
                    {
                        Program.SendString(u, key, @string);
                    }
                    else if (message is object obj)
                    {
                        Program.SendEnum(u, key, obj);
                    }
                }
            }
        }
    }
}
