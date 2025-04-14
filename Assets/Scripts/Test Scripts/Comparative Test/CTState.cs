using CoreResources.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTest_State : StateHistory<CState_StateMachine, CTest_State>
{
}

/// <summary>
/// First state to load, initializes menus 
/// and application specifics.
/// </summary>
public class CTState_InitMenu : CTest_State
{
    public override void OnEnter()
    {
    }

    public override void OnExit()
    {
    }
}

/// <summary>
/// State for starting application 1, 
/// automatically closes application 2
/// on entry.
/// </summary>
public class CTState_Application1 : CTest_State
{
    public override void OnEnter()
    {
        CTGameManager.OnPlayEvent?.Invoke(true);
    }

    public override void OnExit()
    {
        CTGameManager.OnExitEvent?.Invoke();
    }
}

/// <summary>
/// State for starting application 2, 
/// automatically closes application 1
/// on entry.
/// </summary>
public class CTState_Application2 : CTest_State
{
    public override void OnEnter()
    {
        CTGameManager.OnPlayEvent?.Invoke(false);
    }

    public override void OnExit()
    {
        CTGameManager.OnExitEvent?.Invoke();
    }
}

/// <summary>
/// State loaded before application 
/// quitting. Handles application cleanup.
/// </summary>
public class CTState_ApplicationQuit : CTest_State
{
    public override void OnEnter()
    {

    }

    public override void OnExit()
    {

    }
}