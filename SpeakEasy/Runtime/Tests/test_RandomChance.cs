using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
[CreateAssetMenu(fileName = "test_RandomChance", menuName = "Elyse/SpeakEasy Tests/Debugs/test_RandomChance")]
public class test_RandomChance : SpeakEasyLogics_Test
{
    public override bool OnTest()
    {
        Debug.Log("Running RANDOM CHANCE");

        int i = UnityEngine.Random.Range(0, 50);

        Debug.Log("Roll result: " + i);

        if (i >= 25)
        {
            return true;
        }

        else return false;
    }
}
