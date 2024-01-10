using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldSpell : MonoBehaviour
{
    // Start is called before the first frame update

    [SerializeField]
    private Transform castPoint;
    [SerializeField]
    private GameObject shieldedObject;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ParticleManager.Instance.PlayParticle("Shield1", shieldedObject.transform.position, shieldedObject.transform.rotation, shieldedObject.transform);
        }
    }
}
