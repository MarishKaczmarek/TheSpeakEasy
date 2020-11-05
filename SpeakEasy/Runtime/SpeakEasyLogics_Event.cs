using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
//[CreateAssetMenu(fileName = "SpeakEasyTree", menuName = "Elyse/SpeakEasy Events/")]
public class SpeakEasyLogics_Event : ScriptableObject
{
    public virtual void OnEvent()
    {
        //This currently does nothing.
    }
}
