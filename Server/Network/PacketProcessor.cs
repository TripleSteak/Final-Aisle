using Final_Aisle_Server.Data;
using Final_Aisle_Server.Network.Entities;
using Final_Aisle_Server.Network.EventArgs;
using System;

using static Final_Aisle_Shared.Network.PacketDataUtils;

namespace Final_Aisle_Server.Network
{
    /// <summary>
    /// Handles how to respond to client-sent <see cref="Packet"/>s.
    /// Selection statements are written in decreasing order of receival frequency.
    /// </summary>
    public static class PacketProcessor
    {
        /// <summary>
        /// Parses the given <see cref="PacketEventArgs"/> and decides on an appropriate response.
        /// </summary>
        public static void ParseInput(PacketEventArgs args)
        {
            var user = args.User; // user who sent the packet
            var packet = args.Packet;
            var composite = packet.GetComposite(); // may be null, use only when you know the packet is of type "composite"
            var key = packet.GetKey(); // key, existence is universal across all packet types

            if (user.SecureConnectionEstablished)
            { 
                // Data can be safely transported, since AES encryption has already been set up correctly
                if (user.LoggedIn)
                { 
                    // Actions where player is already logged in
                    if (key.Equals(TransformPosition))
                    {
                        PlayerHandler.TransformPosition(user, composite.GetFloat(0), composite.GetFloat(1), composite.GetFloat(2));
                    }
                    else if (key.Equals(TransformRotation))
                    {
                        PlayerHandler.TransformRotation(user, packet.GetFloat());
                    }
                    else if (key.Equals(MovementInput))
                    {
                        PlayerHandler.MovementInput(user, composite.GetFloat(0), composite.GetFloat(1));
                    }
                    else if (key.Equals(MovementRoll))
                    {
                        PlayerHandler.MovementRoll(user, packet.GetFloat());
                    }
                    else if (key.Equals(MovementJump))
                    {
                        PlayerHandler.MovementJump(user);
                    }
                    else if (key.Equals(MovementToggleProne))
                    {
                        PlayerHandler.MovementToggleProne(user, packet.GetBool());
                    }
                    else if (key.Equals(PlayerPostConnect))
                    {
                        PlayerHandler.PostLogIn(user);
                    }
                }
                else if (key.Equals(TryLogin))
                {
                    var identifier = composite.GetString(0);
                    var password = composite.GetString(1);

                    var successful = true;
                    var accountUUID = AccountDataHandler.GetUUIDFromEmail(identifier);
                    
                    if (string.IsNullOrEmpty(accountUUID)) {
                        // Check username if email doesn't exist
                        accountUUID = AccountDataHandler.GetUUIDFromUsername(identifier);
                    }

                    if (string.IsNullOrEmpty(accountUUID))
                    {
                        // Neither email nor username exists
                        successful = false;
                    }

                    if (successful)
                    { 
                        // Check if we have a valid set of user account credentials
                        successful = AccountDataHandler.CheckPassword(accountUUID, password);

                        if (successful)
                        { 
                            // Correct password!
                            user.UserAccount = UserAccount.LoadAccount(accountUUID);

                            Program.SendEmpty(user, LoginSuccess);
                            PlayerHandler.LogIn(user);

                            return;
                        }
                    }

                    // If this code is reached, it should mean that login credentials were incorrect somewhere
                    Program.SendEmpty(user, LoginFail);
                    return;
                }
                else if (key.Equals(TryNewAccount))
                {
                    var email = composite.GetString(0);
                    var username = composite.GetString(1);
                    var password = composite.GetString(2);

                    ConsoleLog.WriteSmallIO("User from " + user.Connection.Socket.RemoteEndPoint + " would like to create an account with the following details:");
                    ConsoleLog.WriteSmallIO("\t   Email: " + email);
                    ConsoleLog.WriteSmallIO("\tUsername: " + username);
                    ConsoleLog.WriteSmallIO("\tPassword: " + new string('*', password.Length));

                    if (!String.IsNullOrEmpty(AccountDataHandler.GetUUIDFromEmail(email)))
                    {
                        Program.SendEmpty(user, EmailAlreadyTaken); // email exists
                        ConsoleLog.WriteSmallIO("Email already taken!");
                    }
                    else if (!String.IsNullOrEmpty(AccountDataHandler.GetUUIDFromUsername(username)))
                    {
                        Program.SendEmpty(user, UsernameAlreadyTaken); // username exists
                        ConsoleLog.WriteSmallIO("Username already taken!");
                    }
                    else
                    {
                        Program.SendEmpty(user, EmailVerifySent);

                        var verifyCode = "";
                        if (AccountDataHandler.RegisterData.ContainsKey(user)) verifyCode = AccountDataHandler.RegisterData[user].Item4;
                        if (string.IsNullOrEmpty(verifyCode)) verifyCode = AccountDataHandler.GenerateVerificationCode();
                        
                        // Store temporary login information
                        AccountDataHandler.RegisterData[user] = new Tuple<string, string, string, string, int>(email, username, password, verifyCode, AccountDataHandler.TotalVerifyEmailTries); 

                        // Attempt to send verification email to listed email address
                        EmailHandler.SendVerificationMail(email, username, verifyCode);
                    }

                }
                else if (key.Equals(TryVerifyEmail))
                {
                    if (!AccountDataHandler.RegisterData.ContainsKey(user) || !packet.GetString().Equals(AccountDataHandler.RegisterData[user].Item4, StringComparison.InvariantCultureIgnoreCase))
                    { 
                        // Verification attempt failed
                        ConsoleLog.WriteSmallIO("User at email " + AccountDataHandler.RegisterData[user].Item1 + " failed verification with attempted code " + packet.GetString());
                        AccountDataHandler.RegisterData[user] = new Tuple<string, string, string, string, int>(AccountDataHandler.RegisterData[user].Item1, AccountDataHandler.RegisterData[user].Item2, AccountDataHandler.RegisterData[user].Item3, AccountDataHandler.RegisterData[user].Item4, AccountDataHandler.RegisterData[user].Item5 - 1);
                        
                        if (AccountDataHandler.RegisterData[user].Item5 <= 0)
                        {
                            // The user is out of tries
                            AccountDataHandler.RegisterData.Remove(user);
                            Program.SendInt(user, EmailVerifyFail, 0); // 0 tries left
                        }
                        else {
                            // send # tries left
                            Program.SendInt(user, EmailVerifyFail, AccountDataHandler.RegisterData[user].Item5); 
                        }
                    }
                    else
                    { 
                        // Verification attempt succeeded
                        ConsoleLog.WriteSmallIO("User at email " + AccountDataHandler.RegisterData[user].Item1 + " succeeded in email verification with code " + packet.GetString());
                        Program.SendEmpty(user, EmailVerifySuccess);

                        // Save new account credentials
                        AccountDataHandler.CreateNewAccount(user, AccountDataHandler.RegisterData[user].Item1, AccountDataHandler.RegisterData[user].Item2, AccountDataHandler.RegisterData[user].Item3);
                        AccountDataHandler.RegisterData.Remove(user);

                        PlayerHandler.LogIn(user);
                    }
                }
            }
            else if (key.Equals(SecureConnectionEstablished))
            { 
                // AES system established
                user.SecureConnectionEstablished = true;
                ConsoleLog.WriteSmallIO("Secure connection successfully established with " + user.Connection.Socket.RemoteEndPoint);
            }
        }

    }
}
