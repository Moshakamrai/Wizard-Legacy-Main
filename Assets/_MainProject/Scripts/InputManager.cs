using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //For Leviosa 
        if (Input.GetKeyDown(KeyCode.Q))
        {
            LevitateObjectTest.Instance.FireLeviosa();
        }
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    SectumSperaTest.Instance.FireSectrumSpera();
        //}
        if (Input.GetKeyDown(KeyCode.Z))
        {
            GameManager.Instance.FireBombardo();
        }
    }
}
