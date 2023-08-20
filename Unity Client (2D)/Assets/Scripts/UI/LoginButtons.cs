using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FinalAisle_Shared.Networking;

/// <summary>
/// Manages the behaviour of login buttons on the game's title screen
/// </summary>
public sealed class LoginButtons : MonoBehaviour
{
    private Connection _connection;
    private DataProcessor _dataProcessor;
    
    public GameObject ConnectionObject;
    public LoginInputFields LoginInputFields;

    public GameObject RegisterWindow;
    public GameObject LoginWindow;
    
    public Button RegisterButton; // for switching over to "Register" screen
    public Button BackButton; // for switching back to "Login" screen
    public Button RegisterQueryButton; // for actually registering an account
    public Button VerifyEmailButton; // sends verification code for checking user ownership of the claimed email

    // Account registration input fields
    public InputField REmailInputField;
    public InputField RUsernameInputField;
    public InputField RPasswordInputField;

    // Login input fields
    public InputField LUsernameInputField;
    public InputField LPasswordInputField;

    // Email verification input fields
    public InputField VerifyEmailInputField;

    public Text RegisterWarningText;
    public Text VerifyEmailWarningText;

    // GameObjects used in transitioning between register screen and loading screen
    public GameObject RegisterUIWidgets;
    public GameObject VerifyEmailUIWidgets;
    public GameObject LoadingSpinner;

    public void Start()
    {
        _connection = ConnectionObject.GetComponent<Connection>();
        _dataProcessor = ConnectionObject.GetComponent<DataProcessor>();
        _dataProcessor.LoginButtons = this;
        
        LoginInputFields = this.gameObject.GetComponent<LoginInputFields>();

        BackToLoginScreen();

        RegisterButton.onClick.AddListener(OpenRegisterWindow);
        BackButton.onClick.AddListener(BackToLoginScreen);
        RegisterQueryButton.onClick.AddListener(TryCreateAccount);
        VerifyEmailButton.onClick.AddListener(SendEmailVerifyCode);

        LoadingSpinner.SetActive(false);
    }

    /// <summary>
    /// Opens the "Register New Account" window.
    /// </summary>
    public void OpenRegisterWindow()
    {
        SetColour(REmailInputField, 1, 1, 1);
        SetColour(RUsernameInputField, 1, 1, 1);
        SetColour(RPasswordInputField, 1, 1, 1);
        RegisterWarningText.text = "";

        LUsernameInputField.text = "";
        LPasswordInputField.text = "";

        RegisterUIWidgets.SetActive(true);
        VerifyEmailUIWidgets.SetActive(false);

        RegisterWindow.SetActive(true);
        LoginWindow.SetActive(false);

        LoginInputFields.SetTabMode(LoginInputFields.RegisterMode);
    }

    /// <summary>
    /// Closes the "Register New Account" window.
    /// </summary>
    public void BackToLoginScreen()
    {
        RegisterWindow.SetActive(false);
        LoginWindow.SetActive(true);

        REmailInputField.text = "";
        RUsernameInputField.text = "";
        RPasswordInputField.text = "";

        LoginInputFields.SetTabMode(LoginInputFields.LoginMode);
    }

    /// <summary>
    /// Sends a request to the server to create/register a new account.
    /// </summary>
    public void TryCreateAccount()
    {
        var emailText = REmailInputField.text;
        var usernameText = RUsernameInputField.text;
        var passwordText = RPasswordInputField.text;

        SetColour(REmailInputField, 1, 1, 1);
        SetColour(RUsernameInputField, 1, 1, 1);
        SetColour(RPasswordInputField, 1, 1, 1);

        if (String.IsNullOrEmpty(emailText))
        {
            SetColour(REmailInputField, 1.0f, 0.75f, 0.75f);
            return;
        }

        if (String.IsNullOrEmpty(usernameText))
        {
            SetColour(RUsernameInputField, 1.0f, 0.75f, 0.75f);
            return;
        }

        if (String.IsNullOrEmpty(passwordText) || passwordText.Length < 8)
        {
            SetColour(RPasswordInputField, 1.0f, 0.75f, 0.75f);
            RegisterWarningText.text = "Password too short!";
            return;
        }

        RegisterUIWidgets.SetActive(false);
        LoadingSpinner.SetActive(true);
        RegisterWarningText.text = "";
        LoginInputFields.DeselectAll();

        var sendString = PacketDataUtils.Condense(PacketDataUtils.TryNewAccount, emailText + " " + usernameText + " " + passwordText);
        _connection.SendData(sendString);
    }

    /// <summary>
    /// Sends the attempted email verification code to the server for approval.
    /// </summary>
    /// <returns></returns>
    public void SendEmailVerifyCode()
    {
        SetColour(VerifyEmailInputField, 1, 1, 1);
        VerifyEmailWarningText.text = "";

        var emailCode = VerifyEmailInputField.text;
        if (String.IsNullOrEmpty(emailCode))
        {
            SetColour(VerifyEmailInputField, 1.0f, 0.75f, 0.75f);
            return;
        }

        VerifyEmailUIWidgets.SetActive(false);
        LoadingSpinner.SetActive(true);
        VerifyEmailWarningText.text = "";
        LoginInputFields.DeselectAll();

        var sendString = PacketDataUtils.Condense(PacketDataUtils.TryVerifyEmail, emailCode);
        _connection.SendData(sendString);
    }

    /// <summary>
    /// Sets the RGB colour of the specified <see cref="InputField"/>.
    /// </summary>
    public void SetColour(InputField field, float r, float g, float b)
    {
        var cb = field.colors;
        cb.normalColor = new Color(r, g, b);
        
        field.colors = cb;
    }
}
