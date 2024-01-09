using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    private static GameManager instance;

    [SerializeField]
    private GameObject mainPlayer;

    [SerializeField]
    private BombardoTest bombarbuTest;

    #region Vars
    public float slowdownFactor; // Adjust the slowdown factor
    public float slowdownDuration; // Adjust the duration of the slowdown
    public float cameraSpeedMultiplier ; // Adjust the camera speed multiplier during slowdown

    private float originalTimeScale;
    public bool slowEffect;

    public List<Transform> outlinedObjects = new List<Transform>();
    public int outlinedObjectCount = 0;
    #endregion
    // Ensure only one instance of the class exists
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                // If no instance exists, create one
                GameObject singletonObject = new GameObject("GameManager");
                instance = singletonObject.AddComponent<GameManager>();
            }

            return instance;
        }
    }
    vThirdPersonCamera cameraScript;
    void Awake()
    {
        // Ensure only one instance of the class exists
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        originalTimeScale = Time.timeScale;
        cameraScript = Camera.main.GetComponent<vThirdPersonCamera>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(SlowMotion());
        }
        //if (outlinedObjectCount == 4)
        //{
        //    slowEffect = false;
        //}

    }

    public void SlowMotionStart()
    {
        StartCoroutine(SlowMotion());
    }

    IEnumerator SlowMotion()
    {
        Time.timeScale = slowdownFactor;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
        slowEffect = true;
        // Disable camera movement slowdown
       
        if (cameraScript != null)
        {
            cameraScript.SetCameraSpeedMultiplier(cameraSpeedMultiplier);
        }

        //while (slowEffect)
        //{
            
        //    //yield return new WaitForSeconds(slowdownDuration);
        //    //slowEffect = false;
        //}

        yield return new WaitForSeconds(slowdownDuration);
        slowEffect = false;

        if (!bombarbuTest.spellCasted)
        {
            bombarbuTest.BombardoCastSpell();
        }
        if (cameraScript != null)
        {
            cameraScript.ResetCameraSpeedMultiplier();
        }
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;

    }

    public void FocusOnObjects(GameObject cam,  float rotationSpeed, float focusDuration)
    {
        
        StartCoroutine(FocusCoroutine(cam, mainPlayer.transform, rotationSpeed, focusDuration));
    }

    IEnumerator FocusCoroutine(GameObject cam, Transform mainPlayer, float rotationSpeed, float focusDuration)
    {
        // Calculate the center point of the outlined objects
        Vector3 centerPoint = CalculateCenterPoint();

        // Get the initial rotation and position of the camera and mainPlayer
        Quaternion initialCamRotation = cam.transform.rotation;
        Vector3 initialCamPosition = cam.transform.position;
        Quaternion initialPlayerRotation = mainPlayer.rotation;

        // Set the camera and mainPlayer to look at the center point smoothly
        float elapsedTime = 0f;
        while (elapsedTime < focusDuration)
        {
            // Interpolate camera rotation
            cam.transform.rotation = Quaternion.Slerp(initialCamRotation, Quaternion.LookRotation(centerPoint - cam.transform.position), elapsedTime / focusDuration);

            // Interpolate camera position
            //cam.transform.position = Vector3.Lerp(initialCamPosition, centerPoint, elapsedTime / focusDuration);

            // Interpolate player rotation
            //mainPlayer.rotation = Quaternion.Slerp(initialPlayerRotation, Quaternion.LookRotation(centerPoint - mainPlayer.position), elapsedTime / focusDuration);
            //mainPlayer.LookAt(centerPoint);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the camera and player end up exactly at the desired rotation and position
        cameraScript.lockCamera = false;
    }

    Vector3 CalculateCenterPoint()
    {
        Vector3 centerPoint = Vector3.zero;

        foreach (Transform target in outlinedObjects)
        {
            centerPoint += target.position;
        }

        // Calculate the average position to find the center
        centerPoint /= outlinedObjects.Count;

        return centerPoint;
    }

}
