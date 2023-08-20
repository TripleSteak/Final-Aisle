#define ENABLE_UPDATE_FUNCTION_CALLBACK
#define ENABLE_LATEUPDATE_FUNCTION_CALLBACK
#define ENABLE_FIXEDUPDATE_FUNCTION_CALLBACK

using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Allows scripts running on other threads to access the main Unity thread.<para/>
/// Taken and modified from https://github.com/microsoft/SeeingVRtoolkit/blob/master/Assets/SeeingVR/Scripts/UnityThread.cs. Credit where due.
/// </summary>
public sealed class UnityThread : MonoBehaviour
{
    // Singleton _instance 
    private static UnityThread _instance = null;

    // Holds Actions queued up from another Thread.
    private static readonly List<Action> _actionQueuesUpdateFunc = new();
    private static readonly List<Action> _actionQueuesLateUpdateFunc = new();
    private static readonly List<Action> _actionQueuesFixedUpdateFunc = new();

    // Holds a copy of each Action that is about to be executed.
    private readonly List<Action> _actionCopiedQueueUpdateFunc = new();
    private readonly List<Action> _actionCopiedQueueLateUpdateFunc = new();
    private readonly List<Action> _actionCopiedQueueFixedUpdateFunc = new();

    // Used to know if we have new Action function to execute. This prevents the use of the lock keyword every frame.
    private volatile static bool _noActionQueueToExecuteUpdateFunc = true;
    private volatile static bool _noActionQueueToExecuteLateUpdateFunc = true;
    private volatile static bool _noActionQueueToExecuteFixedUpdateFunc = true;
    
    /// <summary>
    /// Used to initialize the <see cref="UnityThread"/>.
    /// Call before the invocation of any other methods in this class!
    /// </summary>
    public static void InitUnityThread(bool visible = false)
    {
        if (_instance != null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            // Add an invisible game object to the scene
            var obj = new GameObject("MainThreadExecuter");
            if (!visible)
            {
                obj.hideFlags = HideFlags.HideAndDontSave;
            }

            DontDestroyOnLoad(obj);
            _instance = obj.AddComponent<UnityThread>();
        }
    }

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    public void Update()
    {
        if (_noActionQueueToExecuteUpdateFunc)
        {
            return;
        }

        // Clear the old actions from the _actionCopiedQueueUpdateFunc queue
        _actionCopiedQueueUpdateFunc.Clear();
        lock (_actionQueuesUpdateFunc)
        {
            // Copy _actionQueuesUpdateFunc to the _actionCopiedQueueUpdateFunc variable
            _actionCopiedQueueUpdateFunc.AddRange(_actionQueuesUpdateFunc);

            // Now clear the _actionQueuesUpdateFunc since we've done copying it
            _actionQueuesUpdateFunc.Clear();
            _noActionQueueToExecuteUpdateFunc = true;
        }

        // Loop and execute the functions from the _actionCopiedQueueUpdateFunc
        foreach (var action in _actionCopiedQueueUpdateFunc)
        {
            action.Invoke();
        }
    }
    
    public void LateUpdate()
    {
        if (_noActionQueueToExecuteLateUpdateFunc)
        {
            return;
        }

        // Clear the old actions from the _actionCopiedQueueLateUpdateFunc queue
        _actionCopiedQueueLateUpdateFunc.Clear();
        lock (_actionQueuesLateUpdateFunc)
        {
            // Copy _actionQueuesLateUpdateFunc to the _actionCopiedQueueLateUpdateFunc variable
            _actionCopiedQueueLateUpdateFunc.AddRange(_actionQueuesLateUpdateFunc);
            
            // Now clear the _actionQueuesLateUpdateFunc since we've done copying it
            _actionQueuesLateUpdateFunc.Clear();
            _noActionQueueToExecuteLateUpdateFunc = true;
        }

        // Loop and execute the functions from the _actionCopiedQueueLateUpdateFunc
        foreach (var action in _actionCopiedQueueLateUpdateFunc)
        {
            action.Invoke();
        }
    }

    public void FixedUpdate()
    {
        if (_noActionQueueToExecuteFixedUpdateFunc)
        {
            return;
        }

        // Clear the old actions from the _actionCopiedQueueFixedUpdateFunc queue
        _actionCopiedQueueFixedUpdateFunc.Clear();
        lock (_actionQueuesFixedUpdateFunc)
        {
            // Copy _actionQueuesFixedUpdateFunc to the _actionCopiedQueueFixedUpdateFunc variable
            _actionCopiedQueueFixedUpdateFunc.AddRange(_actionQueuesFixedUpdateFunc);
            
            // Now clear the _actionQueuesFixedUpdateFunc since we've done copying it
            _actionQueuesFixedUpdateFunc.Clear();
            _noActionQueueToExecuteFixedUpdateFunc = true;
        }

        // Loop and execute the functions from the _actionCopiedQueueFixedUpdateFunc
        foreach (var action in _actionCopiedQueueFixedUpdateFunc)
        {
            action.Invoke();
        }
    }

    public void OnDisable()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Queues up an <see cref="Action"/> to be executed in the next Update() call.
    /// </summary>
    public void ExecuteInUpdate(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        lock (_actionQueuesUpdateFunc)
        {
            _actionQueuesUpdateFunc.Add(action);
            _noActionQueueToExecuteUpdateFunc = false;
        }
    }

    /// <summary>
    /// Queues up an <see cref="Action"/> to be executed in the next LateUpdate() call.
    /// </summary>
    public void ExecuteInLateUpdate(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        lock (_actionQueuesLateUpdateFunc)
        {
            _actionQueuesLateUpdateFunc.Add(action);
            _noActionQueueToExecuteLateUpdateFunc = false;
        }
    }

    /// <summary>
    /// Queues up an <see cref="Action"/> to be executed in the next FixedUpdate() call.
    /// </summary>
    public void ExecuteInFixedUpdate(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        lock (_actionQueuesFixedUpdateFunc)
        {
            _actionQueuesFixedUpdateFunc.Add(action);
            _noActionQueueToExecuteFixedUpdateFunc = false;
        }
    }
}