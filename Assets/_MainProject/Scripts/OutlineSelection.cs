using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class OutlineSelection : MonoBehaviour
{
    private Transform highlight;
    private Transform selection;
    private RaycastHit raycastHit;

    [SerializeField]
    private LevitateObjectTest levitateObjectTest;

    [SerializeField]
    private SectumSperaTest sectumSperaTest;



    void Update()
    {
        // Highlight
        if (highlight != null)
        {
            highlight.gameObject.GetComponent<Outline>().enabled = false;
            highlight = null;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!EventSystem.current.IsPointerOverGameObject() && Physics.Raycast(ray, out raycastHit)) // Make sure you have EventSystem in the hierarchy before using EventSystem
        {
            highlight = raycastHit.transform;

            // Check if the highlighted object has the tag "MoveAble" or "CollideObject"
            if ((highlight.CompareTag("MoveAble") && highlight != selection))
            {
                if (highlight.gameObject.GetComponent<Outline>() != null)
                {
                    highlight.gameObject.GetComponent<Outline>().enabled = true;
                }
                else
                {
                    Outline outline = highlight.gameObject.AddComponent<Outline>();
                    outline.enabled = true;
                    highlight.gameObject.GetComponent<Outline>().OutlineColor = Color.green;
                    highlight.gameObject.GetComponent<Outline>().OutlineWidth = 6.0f;
                }
            }
            else
            {
                highlight = null;
            }
        }

        if (levitateObjectTest.castedSpell)
        {
            // Check if the raycast hits an object with the tag "CollideObject"
            if (Physics.Raycast(ray, out raycastHit) && raycastHit.collider.CompareTag("CollideObject"))
            {
                // Highlight the object with the "CollideObject" tag
                if (raycastHit.transform.gameObject.GetComponent<Outline>() != null)
                {
                    raycastHit.transform.gameObject.GetComponent<Outline>().enabled = true;
                }
                else
                {
                    Outline outline = raycastHit.transform.gameObject.AddComponent<Outline>();
                    outline.enabled = false;
                    raycastHit.transform.gameObject.GetComponent<Outline>().OutlineColor = Color.blue;
                    raycastHit.transform.gameObject.GetComponent<Outline>().OutlineWidth = 6.0f;
                }
            }
        }

        if (GameManager.Instance.slowEffect)
        {
            // Check if the raycast hits an object with the tag "CollideObject"
            if (Physics.Raycast(ray, out raycastHit) && raycastHit.collider.CompareTag("CollideObject"))
            {
                // Check if the object is not already outlined
                if (!GameManager.Instance.outlinedObjects.Contains(raycastHit.transform))
                {
                    // Highlight the object with the "CollideObject" tag
                    if (raycastHit.transform.gameObject.GetComponent<Outline>() != null)
                    {
                        raycastHit.transform.gameObject.GetComponent<Outline>().enabled = true;
                    }
                    else
                    {
                        Outline outline = raycastHit.transform.gameObject.AddComponent<Outline>();
                        outline.enabled = true;
                        raycastHit.transform.gameObject.GetComponent<Outline>().OutlineColor = Color.red;
                        raycastHit.transform.gameObject.GetComponent<Outline>().OutlineWidth = 6.0f;

                        // Add the outlined object's transform to the list
                        GameManager.Instance.outlinedObjects.Add(raycastHit.transform);
                        GameManager.Instance.outlinedObjectCount++;

                        // Check if the count is 4
                        if (GameManager.Instance.outlinedObjectCount == 4)
                        {
                            // Do something when 4 objects are outlined
                            Debug.Log("outline should be off");
                            GameManager.Instance.slowEffect = false;
                            
                        }
                    }
                }
            }
           
        }

        // Selection
        if (Input.GetMouseButtonDown(0))
        {
            if (highlight)
            {
                if (selection != null)
                {
                    selection.gameObject.GetComponent<Outline>().enabled = false;
                }
                selection = raycastHit.transform;
                selection.gameObject.GetComponent<Outline>().enabled = true;
                highlight = null;
            }
            else
            {
                if (selection)
                {
                    selection.gameObject.GetComponent<Outline>().enabled = false;
                    selection = null;
                }
            }
        }
    }
}
