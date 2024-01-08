using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.GameObjectExamples.BaseDemo.ObjectPicking.Code
{
    public class HelpPopupOnStarterAssetsSymbolNotPresent : MonoBehaviour
    {
        [SerializeField] private GameObject UiElementToHide = null;


        private void Awake()
        {
#if STARTER_ASSETS_PACKAGES_CHECKED
         this.UiElementToHide.SetActive(false);
#else
        UiElementToHide.SetActive(true);
#endif
        }

        private void OnDrawGizmos()
        {
#if STARTER_ASSETS_PACKAGES_CHECKED
           this.UiElementToHide.SetActive(false);
#else
           UiElementToHide.SetActive(true);
#endif
        }
    }
}