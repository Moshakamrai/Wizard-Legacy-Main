using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OutlineSelection : MonoBehaviour
{
    private Transform highlight;
    private RaycastHit raycastHit;

    [SerializeField] private LevitateObjectTest levitateObjectTest;
    [SerializeField] private SectumSperaTest sectumSperaTest;

    void Update()
    {
        if (highlight != null)
        {
            ToggleOutline(highlight, false, Color.clear);
            highlight = null;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out raycastHit))
        {
            highlight = raycastHit.transform;

            if (highlight.CompareTag("MoveAble") )
            {
                ToggleOutline(highlight, true, Color.green);
            }
            else
            {
                highlight = null;
            }
        }

        if (levitateObjectTest.castedSpell || GameManager.Instance.slowEffect)
        {
            HandleRaycastInteraction(ray, "CollideObject", Color.red);
        }

        if (GameManager.Instance.slowEffect)
        {
            HandleRaycastInteraction(ray, "CollideObject", Color.red, GameManager.Instance.outlinedObjects, 4);
        }
    }

    private void ToggleOutline(Transform objTransform, bool enable, Color outlineColor)
    {
        Outline outline = objTransform.GetComponent<Outline>();
        if (outline == null)
        {
            outline = objTransform.gameObject.AddComponent<Outline>();
            outline.OutlineWidth = 3.5f;
        }

        outline.enabled = enable;
        outline.OutlineColor = outlineColor;
    }

    private void HandleRaycastInteraction(Ray ray, string tag, Color outlineColor)
    {
        if (Physics.Raycast(ray, out raycastHit))
        {
            if (raycastHit.collider.CompareTag(tag))
            {
                ToggleOutline(raycastHit.transform, true, outlineColor);
            }
            else
            {
                // Turn off outline if the raycast doesn't hit the object with the specified tag
                ToggleOutline(raycastHit.transform, false, Color.clear);
            }
        }
    }


    private void HandleRaycastInteraction(Ray ray, string tag, Color outlineColor, List<Transform> outlinedObjects, int targetCount)
    {
        if (Physics.Raycast(ray, out raycastHit) && raycastHit.collider.CompareTag(tag))
        {
            if (!outlinedObjects.Contains(raycastHit.transform))
            {
                ToggleOutline(raycastHit.transform, true, outlineColor);

                outlinedObjects.Add(raycastHit.transform);
                GameManager.Instance.outlinedObjectCount++;

                if (GameManager.Instance.outlinedObjectCount == targetCount)
                {
                    Debug.Log("Outline should be turned off");
                    GameManager.Instance.slowEffect = false;
                }
            }
        }
    }
}
