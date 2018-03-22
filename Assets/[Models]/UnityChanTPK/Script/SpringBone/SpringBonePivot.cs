using UnityEngine;

namespace FUnit
{
    public class SpringBonePivot : MonoBehaviour
    {
#if UNITY_EDITOR
        private const float DrawScale = 0.05f;

        private void OnDrawGizmos()
        {
            if (!SpringManager.onlyShowSelectedBones)
            {
                HandlesUtil.DrawTransform(transform, DrawScale);
            }
        }

        private void OnDrawGizmosSelected()
        {
            HandlesUtil.DrawTransform(transform, DrawScale);
        }
#endif
    }
}