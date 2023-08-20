using Final_Aisle_Server.Network;
using System;
using System.Collections.Generic;
using System.IO;

namespace Final_Aisle_Server.Data
{
    /// <summary>
    /// Handles all data processing involved in user accounts and account credentials.<para/>
    /// <b>DISCLAIMER</b>: I had no understanding of databases when I wrote this :P (let alone SQL)
    /// </summary>
    public static class AccountDataHandler
    {
        private static readonly List<Tuple<string, string>> EmailList = new List<Tuple<string, string>>(); // <email, account UUID>
        private static readonly List<Tuple<string, string>> UsernameList = new List<Tuple<string, string>>(); // <username, account UUID>

        /// <summary>
        /// Stores various data relating to the new account creation process.
        /// The ordering of tuple data is (email, username, password, verification code, # tries left).<para/>
        /// Please change the five-value tuple into a class instead :>
        /// </summary>
        public static readonly Dictionary<NetworkUser, Tuple<string, string, string, string, int>> RegisterData = new Dictionary<NetworkUser, Tuple<string, string, string, string, int>>();

        /// <summary>
        /// Maximum number of verification attempts allowed
        /// </summary>
        public const int TotalVerifyEmailTries = 5;

        /// <summary>
        /// Startup initializations for the <see cref="AccountDataHandler"/>.
        /// </summary>
        public static void Init()
        {
            var emailString = FileUtils.ReadStringFromFile(FileUtils.AccountsDirectory, "Email List").Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var usernameString = FileUtils.ReadStringFromFile(FileUtils.AccountsDirectory, "Username List").Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (var i = 1; i < emailString.Length; i += 2)
            {
                var email = emailString[i - 1];
                var accountID = emailString[i];
                if (!string.IsNullOrEmpty(email)) EmailList.Add(new Tuple<string, string>(email, accountID));
            }
            
            for (var i = 1; i < usernameString.Length; i += 2)
            {
                var username = usernameString[i - 1];
                var accountID = usernameString[i];
                if (!String.IsNullOrEmpty(username)) UsernameList.Add(new Tuple<string, string>(username, accountID));
            }

            ConsoleLog.WriteSmallData("Loaded in " + EmailList.Count + " current email addresses and " + UsernameList.Count + " active usernames.");
        }

        /// <summary>
        /// Randomly generates a new account UUID.
        /// </summary>
        public static string GenerateNewAccountUUID()
        {
            string uuid, newDir;
            bool repeat;

            do
            {
                uuid = Guid.NewGuid().ToString();
                newDir = Path.Combine(FileUtils.AccountsDirectory, uuid);
                repeat = Directory.Exists(newDir);

            } while (repeat); // prevent overlapping UUIDs, though the chance is very slim

            Directory.CreateDirectory(newDir);
            return uuid;
        }

        /// <summary>
        /// Gets the corresponding account UUID from email using a binary search.
        /// </summary>
        public static string GetUUIDFromEmail(string email)
        {
            if (EmailList.Count == 0)
            {
                return "";
            }

            int left = 0, right = EmailList.Count;
            while (right != left)
            {
                var mid = (left + right) / 2;
                if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) > 0)
                {
                    right = mid;
                }
                else if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) < 0)
                {
                    left = mid + 1;
                }
                else
                {
                    return EmailList[mid].Item2;
                }
            }

            return "";
        }

        /// <summary>
        /// Gets the corresponding account UUID from username using a binary search.
        /// </summary>
        public static string GetUUIDFromUsername(string username)
        {
            if (UsernameList.Count == 0)
            {
                return "";
            }

            int left = 0, right = UsernameList.Count;
            while (right != left)
            {
                var mid = (left + right) / 2;
                if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) > 0)
                {
                    right = mid;
                }
                else if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) < 0)
                {
                    left = mid + 1;
                }
                else
                {
                    return UsernameList[mid].Item2;
                }
            }

            return "";
        }

        /// <summary>
        /// Adds a new account entry into the email and username lists using a binary insertion.
        /// </summary>
        public static void AddToEmailUsernameLists(string email, string username, string accountUUID)
        {
            int left = 0, right = EmailList.Count;
            while (right != left)
            {
                var mid = (left + right) / 2;
                if (EmailList[mid].Item1.ToUpper().CompareTo(email.ToUpper()) >= 0)
                {
                    right = mid;
                }
                else
                {
                    left = mid + 1;
                }
            }
            EmailList.Insert(left, new Tuple<string, string>(email, accountUUID));

            left = 0;
            right = UsernameList.Count;
            while (right != left)
            {
                var mid = (left + right) / 2;
                if (UsernameList[mid].Item1.ToUpper().CompareTo(username.ToUpper()) >= 0)
                {
                    right = mid;
                }
                else
                {
                    left = mid + 1;
                }
            }
            UsernameList.Insert(left, new Tuple<string, string>(username, accountUUID));

            SaveEmailUsernameLists();
        }

        /// <summary>
        /// Register a new account (and its credentials) into the database.
        /// </summary>
        public static void CreateNewAccount(NetworkUser user, string email, string username, string password)
        {
            var newAccount = UserAccount.CreateNewAccount(email, username, password);
            user.UserAccount = newAccount;
        }

        /// <summary>
        /// Generate a random six-digit alphanumeric verification code to be sent to the user by email
        /// </summary>
        public static string GenerateVerificationCode()
        {
            var verificationCode = "";
            var rand = new Random();

            for (var i = 0; i < 6; i++)
            { 
                // Generates random characters for verification code
                var num = (i == 2 ? rand.Next(10) : rand.Next(36));
                if (num < 10)
                    verificationCode += num.ToString();
                else
                {
                    num -= 10;
                    verificationCode += ((char)(num + 'A')).ToString();
                }
            }

            return verificationCode;
        }

        /// <summary>
        /// Saves the given user account data.
        /// </summary>
        public static void SaveUserData(UserAccount account, string email, string username, string password)
        {
            var accountFolder = FileUtils.CombineAndCreate(FileUtils.AccountsDirectory, account.AccountUUID);
            
            // TODO: Please do not write a plaintext password into a file :(
            FileUtils.WriteToFile(accountFolder, "Email", email);
            FileUtils.WriteToFile(accountFolder, "Username", username);
            FileUtils.WriteToFile(accountFolder, "Password", password);
        }

        /// <summary>
        /// Gets the username associated with the given account UUID.
        /// </summary>
        public static string GetUsername(string accountUUID)
        {
            var accountFolder = FileUtils.CombineAndCreate(FileUtils.AccountsDirectory, accountUUID);
            return FileUtils.ReadStringFromFile(accountFolder, "Username");
        }

        /// <summary>
        /// Determines if the given password matches with the specified account's password.
        /// </summary>
        public static bool CheckPassword(string accountUUID, string password)
        {
            var accountFolder = FileUtils.CombineAndCreate(FileUtils.AccountsDirectory, accountUUID);
            return FileUtils.ReadStringFromFile(accountFolder, "Password").Equals(password);
        }

        /// <summary>
        /// Saves the active list of emails and usernames to file.
        /// </summary>
        private static void SaveEmailUsernameLists()
        {
            var emailString = "";
            foreach (var t in EmailList)
            {
                emailString += t.Item1 + Environment.NewLine + t.Item2 + Environment.NewLine;
            }
            FileUtils.WriteToFile(FileUtils.AccountsDirectory, "Email List", emailString);

            var usernameString = "";
            foreach (var t in UsernameList)
            {
                usernameString += t.Item1 + Environment.NewLine + t.Item2 + Environment.NewLine;
            }
            FileUtils.WriteToFile(FileUtils.AccountsDirectory, "Username List", usernameString);
        }
    }
}
