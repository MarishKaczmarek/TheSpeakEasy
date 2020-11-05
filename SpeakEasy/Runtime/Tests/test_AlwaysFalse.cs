using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(fileName = "test_AlwaysFalse", menuName = "Elyse/SpeakEasy Tests/Debugs/test_AlwaysFalse")]
public class test_AlwaysFalse : SpeakEasyLogics_Test
{
    public override bool OnTest()
    {
        Debug.Log("Running ALWAYS_FALSE");
        return false;
    }
}
