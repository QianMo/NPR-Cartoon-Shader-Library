using FUnit.GameObjectExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FUnit
{
    public class SpringManager : MonoBehaviour
    {
        [Header("Properties")]
        public int simulationFrameRate = 60;
        [Range(0f, 1f)]
        public float dynamicRatio = 0.5f;
        public Vector3 gravity = new Vector3(0f, -10f, 0f);

        [Header("Ground Collision")]
        public bool collideWithGround = true;
        public float groundHeight = 0f;

        [Header("Bones")]
        public SpringBone[] springBones;

#if UNITY_EDITOR
        [Header("Gizmos")]
        public Color boneColor = Color.yellow;
        public Color colliderColor = Color.gray;
        public Color collisionColor = Color.red;
        public Color groundCollisionColor = Color.green;
        public float angleLimitDrawScale = 0.05f;

        public static bool onlyShowSelectedBones = true;
        public static bool showBoneSpheres = true;
        public static bool showColliders = true;
        public static bool showBoneNames = false;

        // Can't declare as const...
        public static Color DefaultBoneColor { get { return Color.yellow; } }
        public static Color DefaultColliderColor { get { return Color.gray; } }
        public static Color DefaultCollisionColor { get { return Color.red; } }
        public static Color DefaultGroundCollisionColor { get { return Color.green; } }
#endif

        // Tells the SpringManager which bones are moved by animation.
        // Can be called on a per-animation basis.
        public void UpdateBoneIsAnimatedStates(IList<string> animatedBoneNames)
        {
            if (boneIsAnimatedStates == null
                || boneIsAnimatedStates.Length != springBones.Length)
            {
                boneIsAnimatedStates = new bool[springBones.Length];
            }

            var boneCount = springBones.Length;
            for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
            {
                boneIsAnimatedStates[boneIndex] = animatedBoneNames.Contains(springBones[boneIndex].name);
            }
        }

        // Find SpringBones in children and assign them in depth order.
        // Note that the original list will be overwritten.
        public void FindSpringBones(bool includeInactive = false)
        {
            var boneDepthList = new List<BoneDepthPair>();
            var unsortedSpringBones = GetComponentsInChildren<SpringBone>(includeInactive);
            foreach (var springBone in unsortedSpringBones)
            {
                int boneDepth = GetObjectDepth(springBone.transform);
                boneDepthList.Add(new BoneDepthPair { bone = springBone, depth = boneDepth });
            }

            boneDepthList.Sort((a, b) => a.depth.CompareTo(b.depth));

            var sortedBones = new List<SpringBone>();
            foreach (var boneDepthPair in boneDepthList)
            {
                sortedBones.Add(boneDepthPair.bone);
            }
            springBones = sortedBones.ToArray();
        }

        // Removes any nulls from bone colliders
        public void CleanUpBoneColliders()
        {
            foreach (var bone in springBones)
            {
                bone.sphereColliders = GameObjectUtil.RemoveNulls(bone.sphereColliders).ToArray();
                bone.capsuleColliders = GameObjectUtil.RemoveNulls(bone.capsuleColliders).ToArray();
                bone.panelColliders = GameObjectUtil.RemoveNulls(bone.panelColliders).ToArray();
            }
        }

        // Get the depth of an object (number of consecutive parents)
        public static int GetObjectDepth(Transform inObject)
        {
            var depth = 0;
            var currentObject = inObject;
            while (currentObject != null)
            {
                currentObject = currentObject.parent;
                ++depth;
            }
            return depth;
        }

        // private

        private bool[] boneIsAnimatedStates;

        private struct BoneDepthPair
        {
            public SpringBone bone;
            public int depth;
        }

        private void Awake()
        {
            FindSpringBones(true);
        }

        private void Start()
        {
            springBones = springBones.Where(
                bone => bone != null && bone.transform.childCount > 0)
                .ToArray();
            boneIsAnimatedStates = new bool[springBones.Length];
        }

        private void LateUpdate()
        {
            var timeStep = (simulationFrameRate > 0) ? 
                (1f / simulationFrameRate) : 
                Time.deltaTime;

            var boneCount = springBones.Length;
            for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
            {
                var springBone = springBones[boneIndex];
                var sumOfForces = gravity;
                springBone.UpdateSpring(timeStep, sumOfForces);
                springBone.SatisfyConstraintsAndComputeRotation(
                    timeStep, boneIsAnimatedStates[boneIndex] ? dynamicRatio : 1f);
            }
        }

#if UNITY_EDITOR
        private Vector3[] groundPoints;

        private void OnDrawGizmos()
        {
            if (collideWithGround)
            {
                if (groundPoints == null)
                {
                    groundPoints = new Vector3[] {
                        new Vector3(-1f, 0f, -1f),
                        new Vector3( 1f, 0f, -1f),
                        new Vector3( 1f, 0f,  1f),
                        new Vector3(-1f, 0f,  1f)
                    };
                }

                var characterPosition = transform.position;
                var groundOrigin = new Vector3(characterPosition.x, groundHeight, characterPosition.z);
                var pointCount = groundPoints.Length;
                Gizmos.color = Color.yellow;
                for (int pointIndex = 0; pointIndex < pointCount; pointIndex++)
                {
                    var endPointIndex = (pointIndex + 1) % pointCount;
                    Gizmos.DrawLine(
                        groundOrigin + groundPoints[pointIndex],
                        groundOrigin + groundPoints[endPointIndex]);
                }
            }
        }
#endif
    }
}
