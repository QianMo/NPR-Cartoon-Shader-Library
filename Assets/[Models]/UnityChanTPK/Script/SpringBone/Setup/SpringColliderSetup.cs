using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FUnit
{
    public static class SpringColliderSetup
    {
        public static IEnumerable<System.Type> GetColliderTypes()
        {
            return new System.Type[]
            {
                typeof(SpringSphereCollider),
                typeof(SpringCapsuleCollider),
                typeof(SpringPanelCollider)
            };
        }

        public static void DestroySpringColliders(GameObject colliderRoot)
        {
            DestroyComponentsOfType<SpringSphereCollider>(colliderRoot);
            DestroyComponentsOfType<SpringCapsuleCollider>(colliderRoot);
            DestroyComponentsOfType<SpringPanelCollider>(colliderRoot);

            var springBones = colliderRoot.GetComponentsInChildren<SpringBone>(true);
            foreach (var springBone in springBones)
            {
                springBone.sphereColliders = RemoveNullItems(springBone.sphereColliders).ToArray();
                springBone.capsuleColliders = RemoveNullItems(springBone.capsuleColliders).ToArray();
                springBone.panelColliders = RemoveNullItems(springBone.panelColliders).ToArray();
            }
        }

        // private

        private static IEnumerable<T> RemoveNullItems<T>(IEnumerable<T> sourceList)
        {
            return (sourceList != null) ?
                sourceList.Where(item => item != null) :
                new T[0];
        }

        private static void DestroyComponentsOfType<T>(GameObject rootObject) where T : Component
        {
            var components = rootObject.GetComponentsInChildren<T>(true);
            System.Array.ForEach(components, item => GameObject.DestroyImmediate(item));
        }
    }
}