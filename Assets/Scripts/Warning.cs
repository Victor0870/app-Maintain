using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Warning : MonoBehaviour
{
    public GameObject warning1;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void CloseWarning()
    {
        warning1.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
