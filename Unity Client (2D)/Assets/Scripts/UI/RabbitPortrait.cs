using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders a portrait of the client's rabbit in the top left corner of the screen.
/// Will render the rabbit portrait with the correct physical features!
/// </summary>
public sealed class RabbitPortrait : MonoBehaviour
{
    [Tooltip("The GameObject of the actual player character whose portrait is being represented")]
    [SerializeField]
    public GameObject rabbitPlayer;
    
    private Animator _animator;

    public void Start()
    {
        // Update colours of the player's rabbit portrait
        gameObject.transform.Find("Back of Ears").GetComponent<SpriteRenderer>().color = rabbitPlayer.transform.Find("Back of Ear Colour").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Colour 1").GetComponent<SpriteRenderer>().color = rabbitPlayer.transform.Find("Body Colour 1").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Colour 2").GetComponent<SpriteRenderer>().color = rabbitPlayer.transform.Find("Body Colour 2").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Eyes").GetComponent<SpriteRenderer>().color = rabbitPlayer.transform.Find("Eyes").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Eyes Shiny").GetComponent<SpriteRenderer>().color = rabbitPlayer.transform.Find("Eyes Shiny").GetComponent<SpriteRenderer>().color;
        gameObject.transform.Find("Inner Ear").GetComponent<SpriteRenderer>().color = rabbitPlayer.transform.Find("Inner Ear Colour").GetComponent<SpriteRenderer>().color;

        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Causes the rabbit portrait to blink.
    /// </summary>
    public void Blink()
    {
        _animator.SetTrigger("Blink");
    }
}
