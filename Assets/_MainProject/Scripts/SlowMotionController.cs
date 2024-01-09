using UnityEngine;
using System.Collections;

public class SlowMotionController : MonoBehaviour
{
    public float slowdownFactor = 0.2f; // Adjust the slowdown factor
    public float slowdownDuration; // Adjust the duration of the slowdown
    public float cameraSpeedMultiplier; // Adjust the camera speed multiplier during slowdown

    private float originalTimeScale;

    void Start()
    {
        originalTimeScale = Time.timeScale;
        cameraSpeedMultiplier = 4f;
        slowdownDuration = 2f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(SlowMotion());
        }
    }

    public void SlowMotionStart()
    {
        StartCoroutine(SlowMotion());
    }

    IEnumerator SlowMotion()
    {
        Time.timeScale = slowdownFactor;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;

        // Disable camera movement slowdown
        vThirdPersonCamera cameraScript = Camera.main.GetComponent<vThirdPersonCamera>();
        if (cameraScript != null)
        {
            cameraScript.SetCameraSpeedMultiplier(cameraSpeedMultiplier);
        }

        yield return new WaitForSeconds(slowdownDuration);

        // Enable camera movement slowdown
        if (cameraScript != null)
        {
            cameraScript.ResetCameraSpeedMultiplier();
        }

        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;
    }
}
