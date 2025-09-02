using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker;

public class FsmControl : MonoBehaviour
{
    private PlayMakerFSM fsm;
    // Start is called before the first frame update
    void Start()
    {
        // find the PlayMakerFSM on a GameObject 
        fsm = GameObject.FindWithTag("FsmCtrl").GetComponent<PlayMakerFSM>();

        // getting fsm variables by name
        FsmString floatVariable = fsm.FsmVariables.GetFsmString("Section");

        // setting fsm variable value
        floatVariable.Value = PlayerPrefs.GetString("Section");


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
