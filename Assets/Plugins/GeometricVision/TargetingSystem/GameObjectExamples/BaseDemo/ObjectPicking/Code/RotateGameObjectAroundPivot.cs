using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code
{
    public class RotateGameObjectAroundPivot : MonoBehaviour
    {
        [SerializeField]
        private float rotateSpeed  = 10f;
        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            this.transform.RotateAround(this.transform.position, Vector3.up, this.rotateSpeed * Time.deltaTime);
        }
    }
}
