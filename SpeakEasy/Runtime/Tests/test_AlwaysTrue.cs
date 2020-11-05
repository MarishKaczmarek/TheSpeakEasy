using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "test_AlwaysTrue", menuName = "Elyse/SpeakEasy Tests/Debugs/test_AlwaysTrue")]
public class test_AlwaysTrue : SpeakEasyLogics_Test
{
    public override bool OnTest()
    {
        Debug.Log("Running ALWAYS_TRUE");
        return true;
    }
}
