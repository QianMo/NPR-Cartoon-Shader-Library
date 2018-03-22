using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FUnit
{
    [CustomEditor(typeof(SpringCapsuleCollider))]
    [CanEditMultipleObjects]
    public class SpringCapsuleColliderInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var collider = (SpringCapsuleCollider)target;
            var affectedSpringBones = FindSpringBonesUsingCollider(collider);
            var affectedBoneCount = affectedSpringBones.Count();
            if (affectedBoneCount > 0)
            {
                const float Spacing = 8f;

                if (affectedBoneCount > 1)
                {
                    GUILayout.Space(Spacing);
                    if (GUILayout.Button("スプリングボーンを全選択"))
                    {
                        Selection.objects = affectedSpringBones
                            .Select(bone => bone.gameObject)
                            .ToArray();
                    }
                }
                GUILayout.Space(Spacing);
               
                foreach (var springBone in affectedSpringBones)
                {
                    if (GUILayout.Button(springBone.name))
                    {
                        Selection.objects = new Object[] { springBone.gameObject };
                    }
                }
                GUILayout.Space(Spacing);
            }
        }

        private static IEnumerable<SpringBone> FindSpringBonesUsingCollider(SpringCapsuleCollider collider)
        {
            var rootObject = collider.transform.root;
            return rootObject.GetComponentsInChildren<SpringBone>(true)
                .Where(bone => bone.capsuleColliders.Contains(collider));
        }
    }
}