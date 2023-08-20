using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using FinalAisle_Shared.Networking;
using FinalAisle_Shared.Networking.Packet;
using System.IO;
using System.Runtime.Versioning;
using System;

/// <summary>
/// Handles reponses to server-sent packets.
/// </summary>
public sealed class DataProcessor : MonoBehaviour
{
    public LoginButtons loginButtons;

    /// <summary>
    /// Parses the packet supplied by the given <see cref="PacketReceivedEventArgs"/> and determines an appropriate response.
    /// </summary>
    public void ParseInput(PacketReceivedEventArgs args)
    {
        if (args.Packet.Data is MessagePacketData)
        {
            // Each packet is broken down into an identifying "prefix", followed by the packet's contents (i.e., "details").
            var prefix = PacketDataUtils.GetPrefix(((MessagePacketData)args.Packet.Data).Message);
            var details = PacketDataUtils.GetStringData(((MessagePacketData)args.Packet.Data).Message);

            if (prefix.Equals(PacketDataUtils.EmailAlreadyTaken))
            {
                // TODO: This should be done with a Unity Coroutine instead
                var returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        loginButtons.RegisterUIWidgets.SetActive(true);
                        loginButtons.LoadingSpinner.SetActive(false);
                        loginButtons.RegisterWarningText.text = "Email already taken!";
                        loginButtons.SetColour(loginButtons.REmailInputField, 1.0f, 0.75f, 0.75f);
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.UsernameAlreadyTaken))
            {
                // TODO: This should be done with a Unity Coroutine instead
                var returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        loginButtons.RegisterUIWidgets.SetActive(true);
                        loginButtons.LoadingSpinner.SetActive(false);
                        loginButtons.RegisterWarningText.text = "Username already taken!";
                        loginButtons.SetColour(loginButtons.RUsernameInputField, 1.0f, 0.75f, 0.75f);
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.EmailVerifySent))
            {
                // TODO: This should be done with a Unity Coroutine instead
                var returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        loginButtons.LoginInputFields.SetTabMode(loginButtons.LoginInputFields.VerifyEmailMode);
                        loginButtons.RegisterUIWidgets.SetActive(false);
                        loginButtons.LoadingSpinner.SetActive(false);
                        loginButtons.VerifyEmailInputField.text = "";
                        loginButtons.VerifyEmailUIWidgets.SetActive(true);
                        loginButtons.LoginInputFields.SetTabMode(loginButtons.LoginInputFields.VerifyEmailMode);
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.EmailVerifySuccess))
            {
                // TODO: This should be done with a Unity Coroutine instead
                var returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        // TODO: Implement logic for when the email verification step has completed successfully
                    });
                });
                returnThread.Start();
            }
            else if (prefix.Equals(PacketDataUtils.EmailVerifyFail))
            {
                // TODO: This should be done with a Unity Coroutine instead
                var returnThread = new Thread(() =>
                {
                    Thread.Sleep(500);
                    UnityThread.executeInUpdate(() =>
                    {
                        if (int.Parse(details) <= 0) // No verification attempts left!
                        {
                            loginButtons.REmailInputField.text = "";
                            loginButtons.RUsernameInputField.text = "";
                            loginButtons.RPasswordInputField.text = "";
                            loginButtons.RegisterUIWidgets.SetActive(true);
                            loginButtons.LoadingSpinner.SetActive(false);
                            loginButtons.LoginInputFields.SetTabMode(loginButtons.LoginInputFields.RegisterMode);
                        }
                        else
                        {
                            loginButtons.VerifyEmailUIWidgets.SetActive(true);
                            loginButtons.LoadingSpinner.SetActive(false);
                            loginButtons.VerifyEmailWarningText.text = "Wrong code! (" + details + " tries left)";
                            loginButtons.SetColour(loginButtons.VerifyEmailInputField, 1.0f, 0.75f, 0.75f);
                        }
                    });
                });
                returnThread.Start();
            }
            
            if (prefix.Equals(PacketDataUtils.JoinLevel))
            {
                UnityThread.ExecuteInUpdate(() =>
                {
                    // TODO: Incomplete implementation
                    // otherPlayer = Instantiate(Library.NetworkRabbitPlayer, new Vector3(31, 8, 0), Quaternion.identity) as GameObject;
                    // otherPlayer.GetComponent<NetworkController>().Initialize();
                });
            }
            else if (prefix.Equals(PacketDataUtils.MovementInput))
            {
                UnityThread.ExecuteInUpdate(() =>
                {
                    // TODO: Incomplete implementation
                    // Vector2 movement = new Vector2(float.Parse(details.Substring(0, details.IndexOf('|'))), float.Parse(details.Substring(details.IndexOf('|') + 1)));
                    // otherPlayer.GetComponent<NetworkController>().RunOnUpdate(movement);
                    // otherPlayer.GetComponent<NetworkController>().RunOnFixedUpdate();
                });
            }
            else if (prefix.Equals(PacketDataUtils.MovementJump))
            {
                UnityThread.ExecuteInUpdate(() =>
                {
                    // TODO: Incomplete implementation
                    //otherPlayer.GetComponent<NetworkController>().jump = true;
                });
            }
            else if (prefix.Equals(PacketDataUtils.MovementRoll))
            {
                // TODO: Incomplete implementation
                UnityThread.ExecuteInUpdate(() =>
                {
                    //otherPlayer.GetComponent<NetworkController>().roll = true;
                });
            }
        }
    }
}
