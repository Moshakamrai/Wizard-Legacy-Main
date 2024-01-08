using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevitateObjectTest : MonoBehaviour
{
    // Singleton instance
    private static LevitateObjectTest _instance;

    // Public property to access the instance
    public static LevitateObjectTest Instance
    {
        get
        {
            // If the instance is null, find the existing instance in the scene
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevitateObjectTest>();

                // If no instance is found, create a new GameObject and attach the script
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("LevitateObjectTestSingleton");
                    _instance = singletonObject.AddComponent<LevitateObjectTest>();
                }
            }

            return _instance;
        }
    }

    public GameObject[] objectMoveAble;
    public GameObject[] targetObject;

    public bool castedSpell;
    public int storedIndex;

    void Awake()
    {
        // Ensure there is only one instance in the scene
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Use the center of the camera's viewport as the ray origin
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                GameObject targetObject = hit.collider.gameObject;

                int objectIndex = GetObjectIndex(clickedObject);
                if (objectIndex != -1)
                {
                    // Perform actions for the clicked moveable object
                    Debug.Log("Clicked on a moveable object. Array Index: " + objectIndex);
                }
            }
        }
    }

    public void ButtonFireTest()
    {
        // Use the center of the camera's viewport as the ray origin
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject clickedObject = hit.collider.gameObject;
            GameObject targetObject = hit.collider.gameObject;

            int objectIndex = GetObjectIndex(clickedObject);
            if (objectIndex != -1)
            {
                // Perform actions for the clicked moveable object
                Debug.Log("Clicked on a moveable object. Array Index: " + objectIndex);
            }
        }
    }

    int GetObjectIndex(GameObject obj)
    {
        if (!castedSpell)
        {
            for (int i = 0; i < objectMoveAble.Length; i++)
            {
                if (objectMoveAble[i] == obj)
                {
                    StartLevitate(i);
                    storedIndex = i;
                    castedSpell = true;
                    return i;
                }
            }
        }
        else if (obj == GameObject.FindGameObjectWithTag("CollideObject"))
        {
            castedSpell = false;
            objectMoveAble[storedIndex].gameObject.GetComponent<LevitateScript>().FlyTowards(obj);
        }
        // Return -1 if the object is not found in the array
        return -1;
    }

    public void StartLevitate(int i)
    {
        objectMoveAble[i].gameObject.GetComponent<LevitateScript>().LevitateUpwards();
    }
}
