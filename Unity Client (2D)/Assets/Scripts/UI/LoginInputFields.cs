using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the behaviour of login input fields on the game's title screen.
/// </summary>
public sealed class LoginInputFields : MonoBehaviour
{
    private LoginButtons _loginButtons;
    private int _tabMode = 0;

    // TODO: Convert to enum
    public const int LoginMode = 0;
    public const int RegisterMode = 1;
    public const int VerifyEmailMode = 2;

    private List<InputField> _loginFields;
    private List<InputField> _registerFields;
    private List<InputField> _verifyEmailFields;

    public void Start()
    {
        _loginButtons = this.gameObject.GetComponent<LoginButtons>();

        _loginFields = new List<InputField> { _loginButtons.LUsernameInputField, _loginButtons.LPasswordInputField };
        _registerFields = new List<InputField> { _loginButtons.REmailInputField, _loginButtons.RUsernameInputField, _loginButtons.RPasswordInputField };
        _verifyEmailFields = new List<InputField> { };
    }

    public void Update()
    {
        // Logic for "tabbing" between different InputFields
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var fieldIndex = -1;

            // Determine the current fieldIndex based on the currently-selected InputField, if applicable
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                if (_tabMode == RegisterMode)
                {
                    for (var i = 0; i < _registerFields.Count; i++)
                    {
                        if (_registerFields[i] == EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                        {
                            fieldIndex = i;
                            break;
                        }
                        
                    }
                }
                else if (_tabMode == LoginMode)
                {
                    for (var i = 0; i < _loginFields.Count; i++) 
                    {
                        if (_loginFields[i] == EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())
                        {
                            fieldIndex = i;
                            break;
                        }
                        
                    }
                }
            }

            // Increment the fieldIndex to select the "next" InputField
            fieldIndex++;
            if (_tabMode == RegisterMode && fieldIndex >= _registerFields.Count)
            {
                fieldIndex = 0;
            }
            else if (_tabMode == LoginMode && fieldIndex >= _loginFields.Count)
            {
                fieldIndex = 0;
            }

            // Perform InputField selection from the updated fieldIndex value
            if (_tabMode == RegisterMode)
            {
                _registerFields[fieldIndex].Select();
            }
            else if (_tabMode == LoginMode)
            {
                _loginFields[fieldIndex].Select();
            }
            else if (_tabMode == VerifyEmailMode)
            {
                _verifyEmailFields[fieldIndex].Select();
            }
        }
    }

    /// <summary>
    /// Deselects all InputFields and sets a new tabbing mode.
    /// </summary>
    /// <param name="newMode"></param>
    /// <returns></returns>
    public void SetTabMode(int newMode)
    {
        DeselectAll();

        _tabMode = newMode;
    }

    /// <summary>
    /// Deselects all InputFields on screen (to reset tabbing).
    /// </summary>
    public void DeselectAll()
    {
        try
        {
            if (_tabMode == RegisterMode)
            {
                foreach (InputField field in _registerFields)
                {
                    field.OnDeselect(new BaseEventData(EventSystem.current));
                }
            }
            else if (_tabMode == LoginMode)
            {
                foreach (InputField field in _loginFields)
                {
                    field.OnDeselect(new BaseEventData(EventSystem.current));
                }
            }
            else if (_tabMode == VerifyEmailMode)
            {
                foreach (InputField field in _verifyEmailFields)
                {
                    field.OnDeselect(new BaseEventData(EventSystem.current));
                }
            }
        }
        catch (Exception)
        {
            // No action required, as a failure to deselect likely means the InputField in question does not end up in the "selected" state
        }
    }
}
