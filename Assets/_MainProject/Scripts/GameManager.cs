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
    private BombardoTest bombardoTest;

    #region Variables
    public float slowdownFactor = 0.1f; // Adjust the slowdown factor
    public float slowdownDuration ; // Adjust the duration of the slowdown
    public float cameraSpeedMultiplier = 0.5f; // Adjust the camera speed multiplier during slowdown

    private float originalTimeScale;
    public bool slowEffect;

    public Transform outlinedObject; // Single target object
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
        slowdownDuration = 0.4f;
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    slowEffect = true;
        //    StartCoroutine(SlowMotion());
        //}
    }
    public void FireBombardo()
    {
        slowEffect = true;
        StartCoroutine(SlowMotion());
    }
    public void SlowMotionStart()
    {
        StartCoroutine(SlowMotion());
    }

    IEnumerator SlowMotion()
    {
 
        yield return new WaitForSeconds(slowdownDuration);
        Time.timeScale = slowdownFactor;
        Time.fixedDeltaTime = Time.timeScale * 0.01f;
        if (cameraScript != null)
        {
            cameraScript.SetCameraSpeedMultiplier(cameraSpeedMultiplier);
        }
        slowEffect = false;
        if (!bombardoTest.spellCasted)
        {
            bombardoTest.BombardoCastSpell();
        }

        if (cameraScript != null)
        {
            cameraScript.ResetCameraSpeedMultiplier();
        }

        yield return new WaitForSeconds(slowdownDuration);

        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = Time.timeScale * 0.01f;
    }

    public void FocusOnObject(GameObject cam, float rotationSpeed, float focusDuration)
    {
        StartCoroutine(FocusCoroutine(cam, mainPlayer.transform, rotationSpeed, focusDuration, outlinedObject));
    }

    public void FocusOnObject(GameObject cam, float rotationSpeed, float focusDuration, Transform targetObject)
    {
        StartCoroutine(FocusCoroutine(cam, mainPlayer.transform, rotationSpeed, focusDuration, targetObject));
    }

    IEnumerator FocusCoroutine(GameObject cam, Transform mainPlayer, float rotationSpeed, float focusDuration, Transform targetObject)
    {
        Quaternion initialCamRotation = cam.transform.rotation;
        Vector3 initialCamPosition = cam.transform.position;
        float elapsedTime = 0f;
        //float zoomFactor = 0.1f; // Adjust the zoom factor

        while (elapsedTime < focusDuration)
        {
            // Zoom in towards the targetObject
            //float currentZoom = Mathf.Lerp(1f, zoomFactor, elapsedTime / focusDuration);
            //cam.transform.position = Vector3.Lerp(initialCamPosition, targetObject.position, elapsedTime / focusDuration);
            cam.transform.rotation = Quaternion.Slerp(initialCamRotation, Quaternion.LookRotation(targetObject.position - cam.transform.position), elapsedTime / focusDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cameraScript.lockCamera = false;
    }


}
