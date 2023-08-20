using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Primary 2D side-scroling camera for observing the client's own player.
/// </summary>
public sealed class Level2DCamera : MonoBehaviour
{
    /*
     * Screen boundaries are represented as a percentage of screen dimensions (always centered).
     * If the player is within the central rectangle, no camera movement will occur.
     */
    [SerializeField]
    private float boundWidth;
    
    [SerializeField]
    private float boundHeight;

    [SerializeField]
    private float levelWidth;
    
    [SerializeField]
    private float levelHeight;
    
    /*
     * The speed at which the parallax background moves, relative to the main camera (as a decimal from 0 to 1).
     * "Background1" and "Background2" refer to different layers of the background.
     */
    [SerializeField]
    [Range(0, 1)] 
    private float panningBackground1Speed;
    
    [SerializeField]
    [Range(0, 1)]
    private float panningBackground2Speed;
    
    [SerializeField]
    private GameObject panningBackground1;
    
    [SerializeField]
    private GameObject panningBackground2;
    
    [SerializeField]
    private Transform playerTransform;

    private float _cameraWidth;
    private float _cameraHeight;
    private Transform _cameraTransform;

    public void Start()
    {
        var cameraHalfSize = GetComponent<Camera>().orthographicSize;
        
        _cameraHeight = cameraHalfSize * 2;
        _cameraWidth = cameraHalfSize * 2 * GetComponent<Camera>().aspect;

        _cameraTransform = GetComponent<Transform>();
    }

    public void FixedUpdate()
    {
        // Adjust camera position to follow player
        var targetCameraX = -1;
        var targetCameraY = -1;
        var cameraPos = _cameraTransform.position;
        
        // Camera movement value, in blocks
        var deltaX = 0;
        var deltaY = 0;

        if (playerTransform.position.x < _cameraTransform.position.x - _cameraWidth * boundWidth / 2) // camera too far right
        {
            targetCameraX = playerTransform.position.x + _cameraWidth * boundWidth / 2;
        }
        else if (playerTransform.position.x > _cameraTransform.position.x + _cameraWidth * boundWidth / 2) // camera too far left
        {
            targetCameraX = playerTransform.position.x - _cameraWidth * boundWidth / 2;
        }

        if (playerTransform.position.y < _cameraTransform.position.y - _cameraHeight * boundHeight / 2) // camera too high
        {
            targetCameraY = playerTransform.position.y + _cameraHeight * boundHeight / 2;
        }
        else if (playerTransform.position.y > _cameraTransform.position.y + _cameraWidth * boundHeight / 2) // camera too low
        {
            targetCameraY = playerTransform.position.y - _cameraWidth * boundHeight / 2;
        }

        // Ensure camera repositioning stays on screen
        if (targetCameraX != -1 && targetCameraX > _cameraWidth / 2 && targetCameraX < levelWidth - _cameraWidth / 2)
        {
            deltaX = targetCameraX - cameraPos.x;
            cameraPos.x = targetCameraX;
        }
        
        if (targetCameraY != -1 && targetCameraY > _cameraHeight / 2 && targetCameraY < levelHeight - _cameraHeight / 2)
        {
            deltaY = targetCameraY - cameraPos.y;
            cameraPos.y = targetCameraY;
        }

        panningBackground1.transform.position = panningBackground1.transform.position + new Vector3(deltaX * panningBackground1Speed, deltaY * panningBackground2Speed, 0);
        panningBackground2.transform.position = panningBackground2.transform.position + new Vector3(deltaX * panningBackground2Speed, deltaY * panningBackground2Speed, 0);

        _cameraTransform.position = cameraPos;
    }
}
