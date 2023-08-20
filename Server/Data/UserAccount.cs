using Final_Aisle_Shared.Game.Player;
using System;
using System.Collections.Generic;

namespace Final_Aisle_Server.Data
{
    /// <summary>
    /// Object that corresponds to the user's gameplay account, as opposed to the network user.
    /// </summary>
    public sealed class UserAccount
    {
        /// <summary>
        /// Unique 128-bit identifier generated once per account.
        /// </summary>
        public string AccountUUID { get; }
        
        /// <summary>
        /// The player's account username (not the per-character display name).
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// The number of playable characters associated with the user's account.
        /// </summary>
        public int UsedCharacterSlots { set; get; }
        
        /// <summary>
        /// The maximum number of playable characters the user's account may have at any given time.
        /// </summary>
        public int TotalCharacterSlots { set; get; }

        /// <summary>
        /// A list containing all of the player's characters, list is sorted by main level & experience.
        /// </summary>
        public List<PlayerCharacter> CharacterList;
        
        /// <summary>
        /// The index of the character with which the user is currently playing.
        /// </summary>
        public int ActiveCharacterIndex { set; get; } 

        public UserAccount(string accountUUID, string username)
        {
            AccountUUID = accountUUID;
            Username = username;

            CharacterList = new List<PlayerCharacter>();
            CharacterList.Add(new PlayerCharacter(accountUUID, username + "'s character", PlayerClass.Warrior, PlayerRace.Turtle)); // temporary new character creation

            UsedCharacterSlots = CharacterList.Count;
            ActiveCharacterIndex = 0;
        }

        /// <summary>
        /// Returns the player's currently active character.
        /// </summary>
        public PlayerCharacter GetActiveCharacter()
        {
            try
            {
                return CharacterList[ActiveCharacterIndex];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a blank <see cref="UserAccount"/> with the given account data.
        /// </summary>
        public static UserAccount CreateNewAccount(string email, string username, string password)
        {
            var account = new UserAccount(AccountDataHandler.GenerateNewAccountUUID(), username);

            AccountDataHandler.AddToEmailUsernameLists(email, username, account.AccountUUID);
            AccountDataHandler.SaveUserData(account, email, username, password);

            return account;
        }

        /// <summary>
        /// Loads the account associated with the given account UUID.
        /// </summary>
        public static UserAccount LoadAccount(string accountUUID)
        {
            var username = AccountDataHandler.GetUsername(accountUUID);
            var account = new UserAccount(accountUUID, username);

            return account;
        }
    }
}
