using FUnit.GameObjectExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FUnit
{
    public class SpringBone : MonoBehaviour
    {
        public enum CollisionStatus
        {
            NoCollision,
            HeadIsEmbedded,
            TailCollision
        }

        [Range(0f, 5000f)]
        public float stiffnessForce = 0.01f;
        [Range(0f, 1f)]
        public float dragForce = 0.4f;
        public Vector3 springForce = new Vector3(0.0f, -0.0001f, 0.0f);
        [Range(0f, 1f)]
        public float windInfluence = 1f;

        public Transform pivotNode;
        public float angularStiffness = 100f;
        public AngleLimits yAngleLimits = new AngleLimits();
        public AngleLimits zAngleLimits = new AngleLimits();

        public Transform[] lengthLimitTargets;

        [Range(0f, 0.5f)]
        public float radius = 0.05f;
        public SpringSphereCollider[] sphereColliders;
        public SpringCapsuleCollider[] capsuleColliders;
        public SpringPanelCollider[] panelColliders;

        private const float SmoothingThreshold = 0.1f;

        public Vector3 BoneAxis { get { return boneAxis; } }

        // Copies dynamics properties to the target.
        // Excludes child, boneAxis, target nodes, and private members.
        public void CopyDynamicsPropertiesTo(SpringBone target)
        {
            target.stiffnessForce = stiffnessForce;
            target.dragForce = dragForce;
            target.springForce = springForce;

            target.angularStiffness = angularStiffness;
            yAngleLimits.CopyTo(target.yAngleLimits);
            zAngleLimits.CopyTo(target.zAngleLimits);

            target.radius = radius;
            target.sphereColliders = (SpringSphereCollider[])sphereColliders.Clone();
            target.capsuleColliders = (SpringCapsuleCollider[])capsuleColliders.Clone();
            target.panelColliders = (SpringPanelCollider[])capsuleColliders.Clone();
        }

        // Automatically assign spring bone child
        public void AutoAssignChild()
        {
            var childPosition = ComputeChildPosition();
            var localChildPosition = transform.InverseTransformPoint(childPosition);
            boneAxis = localChildPosition.normalized;
        }

        public Vector3 ComputeChildPosition()
        {
            var children = GetValidChildren(transform);
            var childCount = children.Count;
            if (childCount == 1)
            {
                return children[0].position;
            }

            if (childCount == 0)
            {
                // This should never happen
                Debug.LogWarning("SpringBone「" + name + "」に有効な子供がありません");
                return transform.position + transform.right * -0.1f;
            }

            var initialTailPosition = new Vector3(0f, 0f, 0f);
            var averageDistance = 0f;
            var selfPosition = transform.position;
            for (int childIndex = 0; childIndex < childCount; childIndex++)
            {
                var childPosition = children[childIndex].position;
                initialTailPosition += childPosition;
                averageDistance += (childPosition - selfPosition).magnitude;
            }

            averageDistance /= (float)childCount;
            initialTailPosition /= (float)childCount;
            var selfToInitial = initialTailPosition - selfPosition;
            selfToInitial.Normalize();
            initialTailPosition = selfPosition + averageDistance * selfToInitial;
            return initialTailPosition;
        }

        public void RemoveAllColliders()
        {
            sphereColliders = new SpringSphereCollider[0];
            capsuleColliders = new SpringCapsuleCollider[0];
            panelColliders = new SpringPanelCollider[0];
        }

        public void UpdateSpring(float deltaTime, Vector3 externalForce)
        {
            skinAnimationLocalRotation = transform.localRotation;
    
            var baseWorldRotation = transform.parent.rotation * initialLocalRotation;
            var orientedInitialPosition = transform.position + baseWorldRotation * boneAxis * springLength;
            
            // Hooke's law: force to push us to equilibrium
            var force = stiffnessForce * (orientedInitialPosition - currTipPos);
            force += springForce + externalForce;
            var sqrDt = deltaTime * deltaTime;
            force *= 0.5f * sqrDt;

            // Verlet
            Vector3 temp = currTipPos;
            force += (1f - dragForce) * (currTipPos - prevTipPos);
            currTipPos += force;
            prevTipPos = temp;

            // Inlined because FixBoneLength is slow
            var headPosition = transform.position;
            var targetToSelf = currTipPos - headPosition;
            var magnitude = targetToSelf.magnitude;
            if (magnitude <= 0.001f)
            {
                targetToSelf = transform.TransformDirection(boneAxis);
            }
            else
            {
                targetToSelf /= magnitude;
            }
            currTipPos = headPosition + springLength * targetToSelf;

            collisionWithColliderFrameCount = Mathf.Max(0, collisionWithColliderFrameCount - 1);
        }

        public void SatisfyConstraintsAndComputeRotation(float deltaTime, float dynamicRatio)
        {
            currTipPos = ApplyLengthLimits(deltaTime);

            var hadCollision = false;

            if ((collisionWithColliderFrameCount == 0)
                & manager.collideWithGround)
            {
                hadCollision = CheckForGroundCollision();
            }

            if (!hadCollision)
            {
                hadCollision = CheckForCollision();
            }

            ApplyAngleLimits(deltaTime);

            // ComputeRotation

            if (float.IsNaN(currTipPos.x)
                | float.IsNaN(currTipPos.y)
                | float.IsNaN(currTipPos.z))
            {
#if UNITY_EDITOR
                Debug.DebugBreak();
#endif
                var baseWorldRotation = transform.parent.rotation * initialLocalRotation;
                currTipPos = transform.position + baseWorldRotation * boneAxis * springLength;
                prevTipPos = currTipPos;
            }

            actualLocalRotation = ComputeRotation(currTipPos);
            transform.localRotation = Quaternion.Lerp(skinAnimationLocalRotation, actualLocalRotation, dynamicRatio);
        }

#if UNITY_EDITOR
        public static bool SelectionContainsSpringBones()
        {
            var selectedObjects = UnityEditor.Selection.gameObjects;
            return selectedObjects.Any(item => item.GetComponent<SpringBone>() != null);
        }
#endif

        // private

        private SpringManager manager;
        private Vector3 boneAxis = new Vector3(-1.0f, 0.0f, 0.0f);
        private float springLength;
        private Quaternion skinAnimationLocalRotation;
        private Quaternion initialLocalRotation;
        private Quaternion actualLocalRotation;
        private Vector3 currTipPos;
        private Vector3 prevTipPos;

        private int collisionWithColliderFrameCount;
        private float[] lengthsToLimitTargets;
        private Vector3[] previousPositions;

        private static IList<Transform> GetValidChildren(Transform parent)
        {
            // Ignore SpringBonePivots
            var childCount = parent.childCount;
            var children = new List<Transform>(childCount);
            for (int childIndex = 0; childIndex < childCount; childIndex++)
            {
                var child = parent.GetChild(childIndex);
                if (child.GetComponent<SpringBonePivot>() == null)
                {
                    children.Add(child);
                }
            }
            return children;
        }

        private Vector3 ApplyLengthLimits(float deltaTime)
        {
            var targetCount = lengthLimitTargets.Length;
            if (targetCount == 0)
            {
                return currTipPos;
            }

            const float SpringConstant = 0.5f;
            var accelerationMultiplier = SpringConstant * deltaTime * deltaTime;
            var movement = new Vector3(0f, 0f, 0f);
            for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                var targetPosition = lengthLimitTargets[targetIndex].position;
                var lengthToLimitTarget = lengthsToLimitTargets[targetIndex];
                var currentToTarget = currTipPos - targetPosition;
                var currentDistanceSquared = currentToTarget.sqrMagnitude;

                // Hooke's Law
                var currentDistance = Mathf.Sqrt(currentDistanceSquared);
                var distanceFromEquilibrium = currentDistance - lengthToLimitTarget;
                movement -= accelerationMultiplier * distanceFromEquilibrium * currentToTarget.normalized;
            }
            return currTipPos + movement;
        }

        private void ApplyAngleLimits(float deltaTime)
        {
            if ((!yAngleLimits.active && !zAngleLimits.active)
                || pivotNode == null)
            {
                return;
            }

            var origin = transform.position;
            var vector = currTipPos - origin;
            var pivot = GetPivotTransform();
            var forward = -pivot.right;

            if (yAngleLimits.active)
            {
                yAngleLimits.ConstrainVector(
                    -pivot.up, -pivot.forward, forward, angularStiffness, deltaTime, ref vector);
            }
            if (zAngleLimits.active)
            {
                zAngleLimits.ConstrainVector(
                    -pivot.forward, -pivot.up, forward, angularStiffness, deltaTime, ref vector);
            }

            currTipPos = origin + vector;
        }

        private void Awake()
        {
            AutoAssignChild();

            initialLocalRotation = transform.localRotation;
            actualLocalRotation = initialLocalRotation;
            manager = transform.GetComponentInParent<SpringManager>();

            sphereColliders = sphereColliders.Where(item => item != null).ToArray();
            capsuleColliders = capsuleColliders.Where(item => item != null).ToArray();
            panelColliders = panelColliders.Where(item => item != null).ToArray();

            if (lengthLimitTargets == null)
            {
                lengthLimitTargets = new Transform[0];
            }
            else
            {
                lengthLimitTargets = GameObjectUtil.RemoveNulls(lengthLimitTargets).ToArray();
            }

            InitializeSpringLengthAndTipPosition();
        }

        private Transform GetPivotTransform()
        {
            if (pivotNode == null)
            {
                pivotNode = transform.parent ?? transform;
            }
            return pivotNode;
        }

        private bool CheckForCollision()
        {
            var headPosition = transform.position;
            var scaledRadius = transform.TransformDirection(radius, 0f, 0f).magnitude;

            var hadCollision = false;

            for (int i = 0; i < capsuleColliders.Length; ++i)
            {
                var collider = capsuleColliders[i];
                if (collider.enabled)
                {
                    var currentCollisionStatus = collider.CheckForCollisionAndReact(
                        headPosition, ref currTipPos, scaledRadius);
                    hadCollision |= currentCollisionStatus != CollisionStatus.NoCollision;
                }
            }

            for (int i = 0; i < sphereColliders.Length; ++i)
            {
                var collider = sphereColliders[i];
                if (collider.enabled)
                {
                    var currentCollisionStatus = collider.CheckForCollisionAndReact(
                        headPosition, ref currTipPos, scaledRadius);
                    hadCollision |= currentCollisionStatus != CollisionStatus.NoCollision;
                }
            } 
            
            var colliderCount = panelColliders.Length;
            for (int colliderIndex = 0; colliderIndex < colliderCount; colliderIndex++)
            {
                var collider = panelColliders[colliderIndex];
                if (collider.enabled)
                {
                    var currentCollisionStatus = collider.CheckForCollisionAndReact(
                        headPosition, springLength, ref currTipPos, scaledRadius);
                    hadCollision |= currentCollisionStatus != CollisionStatus.NoCollision;
                }
            }

            if (hadCollision)
            {
                prevTipPos = currTipPos;
                collisionWithColliderFrameCount = 5;
            }

            return hadCollision;
        }

        private bool CheckForGroundCollision(bool preserveImpulse = true)
        {
            // Todo: this assumes a flat ground parallel to the xz plane
            var worldHeadPosition = transform.position;
            var worldTailPosition = currTipPos;
            var worldRadius = transform.TransformDirection(radius, 0f, 0f).magnitude;
            var worldLength = (currTipPos - worldHeadPosition).magnitude;
            var groundHeight = manager.groundHeight;
            worldHeadPosition.y -= groundHeight;
            worldTailPosition.y -= groundHeight;
            var collidingWithGround = SpringPanelCollider.CheckForCollisionWithAlignedPlaneAndReact(
                worldHeadPosition, worldLength, ref worldTailPosition, worldRadius, SpringPanelCollider.Axis.Y);
            if (collidingWithGround != CollisionStatus.NoCollision)
            {
                worldTailPosition.y += groundHeight;
                currTipPos = worldTailPosition;
                prevTipPos = currTipPos;

                FixBoneLength();
            }
            return collidingWithGround != CollisionStatus.NoCollision;
        }

        private void FixBoneLength()
        {
            currTipPos = FixLengthToTarget(currTipPos, transform.position, 0.5f * springLength, springLength);
        }

        private Vector3 FixLengthToTarget
        (
            Vector3 selfPosition,
            Vector3 targetPosition,
            float minLength,
            float maxLength
        )
        {
            var targetToSelf = selfPosition - targetPosition;
            var magnitude = targetToSelf.magnitude;
            if (magnitude <= 0.001f)
            {
                return selfPosition + transform.TransformDirection(boneAxis) * minLength;
            }
            var newMagnitude = (magnitude < minLength) ? minLength : magnitude;
            newMagnitude = (newMagnitude > maxLength) ? maxLength : newMagnitude;
            return targetPosition + (newMagnitude / magnitude) * targetToSelf;
        }

        private void InitializeSpringLengthAndTipPosition()
        {
            var childPos = ComputeChildPosition();
            springLength = Vector3.Distance(transform.position, childPos);
            currTipPos = childPos;
            prevTipPos = childPos;

            var targetCount = lengthLimitTargets.Length;
            lengthsToLimitTargets = new float[targetCount];
            for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                lengthsToLimitTargets[targetIndex] =
                    (lengthLimitTargets[targetIndex].position - childPos).magnitude;
            }
        }

        private Quaternion ComputeRotation(Vector3 tipPosition)
        {
            var baseWorldRotation = transform.parent.rotation * initialLocalRotation;
            var worldBoneVector = tipPosition - transform.position;
            var localBoneVector = Quaternion.Inverse(baseWorldRotation) * worldBoneVector;
            localBoneVector.Normalize();

            var aimRotation = Quaternion.FromToRotation(boneAxis, localBoneVector);
            var outputRotation = initialLocalRotation * aimRotation;

            return outputRotation;
        }

#if UNITY_EDITOR
        // Rendering of Gizmos / Handles in the editor
        private const float CollisionFlashTime = 0.8f;
        private float collisionFlashTimeRemaining = 0f;

        private void DrawSpringBone(Color drawColor)
        {
            manager = manager ?? gameObject.GetComponentInParent<SpringManager>();
            var childPosition = ComputeChildPosition();
            UnityEditor.Handles.color = drawColor;
            if (manager == null || SpringManager.showBoneSpheres)
            {
                var worldRadius = transform.TransformDirection(radius, 0f, 0f).magnitude;
                // For picking
                Gizmos.color = new Color(0f, 0f, 0f, 0f);
                Gizmos.DrawSphere(childPosition, worldRadius);
                UnityEditor.Handles.RadiusHandle(Quaternion.identity, childPosition, worldRadius);
            }
            UnityEditor.Handles.DrawLine(transform.position, childPosition);

            if (IsRootInsideCollider())
            {
                const float GizmoSize = 0.015f;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, new Vector3(GizmoSize, GizmoSize, GizmoSize));
            }
        }

        private bool IsRootInsideCollider()
        {
            var rootPosition = transform.position;
            if (sphereColliders != null)
            {
                var colliderCount = sphereColliders.Length;
                for (int colliderIndex = 0; colliderIndex < colliderCount; colliderIndex++)
                {
                    if (sphereColliders[colliderIndex] != null
                        && sphereColliders[colliderIndex].Contains(rootPosition))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Color GetDrawColor(bool isSelected)
        {
            var hasManager = manager != null;
            var boneColor = hasManager ? manager.boneColor : SpringManager.DefaultBoneColor;
            boneColor = isSelected ? Color.white : boneColor;
            var collisionColor = hasManager ? manager.collisionColor : SpringManager.DefaultCollisionColor;

            var drawColor = (collisionFlashTimeRemaining > 0f) ? collisionColor : boneColor;
            return drawColor;
        }

        private float GetDrawScale()
        {
            return (manager != null) ? manager.angleLimitDrawScale : 0.3f;
        }

        private void DrawAngleLimits(float drawScale)
        {
            var position = transform.position;
            var pivot = GetPivotTransform();
            var forward = -pivot.right;
            var angleLimitGizmos = new[] {
                new { angleLimits = yAngleLimits, side = -pivot.up, color = new Color(0.2f, 1f, 0.2f) },
                new { angleLimits = zAngleLimits, side = -pivot.forward, color = new Color(0.7f, 0.7f, 1f) }
            };

            foreach (var gizmo in angleLimitGizmos.Where(gizmo => gizmo.angleLimits.active))
            {
                gizmo.angleLimits.DrawLimits(position, gizmo.side, forward, drawScale, gizmo.color);
            }
        }

        private void DrawLinesToLimitTargets()
        {
            if (lengthLimitTargets == null
                || lengthsToLimitTargets == null
                || lengthLimitTargets.Length != lengthsToLimitTargets.Length)
            {
                return;
            }

            var SelfToLimitColor = new Color(1f, 1f, 1f);
            var SelfToTargetColor = new Color(0.5f, 0.6f, 0.5f);
            var ExceededLimitColor = new Color(1f, 0.5f, 0.5f);

            var targetCount = lengthLimitTargets.Length;
            var selfPosition = ComputeChildPosition();
            for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
            {
                var target = lengthLimitTargets[targetIndex];
                var distance = lengthsToLimitTargets[targetIndex];
                if (target != null)
                {
                    var targetPosition = target.position;
                    var selfToTarget = targetPosition - selfPosition;
                    var limitPosition = selfPosition + distance * selfToTarget.normalized;

                    Gizmos.color = SelfToLimitColor;
                    Gizmos.DrawLine(limitPosition, selfPosition);
                    Gizmos.color = (selfToTarget.sqrMagnitude > distance * distance) ?
                        ExceededLimitColor : SelfToTargetColor;
                    Gizmos.DrawLine(targetPosition, limitPosition);
                }
            }
        }

        private void OnDrawGizmos()
        {
            var selectedObjects = UnityEditor.Selection.gameObjects;

            var pivotIsSelected = pivotNode != null && selectedObjects.Contains(pivotNode.gameObject);
            var anyColliderIsSelected = false;
            if (sphereColliders != null
                && capsuleColliders != null
                && panelColliders != null)
            {
                var sphereColliderParents = sphereColliders.Where(item => item != null).Select(item => item.gameObject);
                var capsuleColliderParents = capsuleColliders.Where(item => item != null).Select(item => item.gameObject);
                var panelColliderParents = panelColliders.Where(item => item != null).Select(item => item.gameObject);
                anyColliderIsSelected = selectedObjects.Any(
                    selectedItem => capsuleColliderParents.Contains(selectedItem)
                        || sphereColliderParents.Contains(selectedItem)
                        || panelColliderParents.Contains(selectedItem));
            }

            var shouldDraw = !SpringManager.onlyShowSelectedBones
                    || pivotIsSelected
                    || anyColliderIsSelected;
            if (shouldDraw)
            {
                var drawScale = GetDrawScale();
                DrawAngleLimits(drawScale);
                DrawSpringBone(GetDrawColor(false));

                if (lengthLimitTargets != null
                    && lengthLimitTargets.Length > 0)
                {
                    DrawLinesToLimitTargets();
                }
            }

            if (SpringManager.showBoneNames)
            {
                UnityEditor.Handles.Label(transform.position, name);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Don't draw us if we're a child of the selection because it's too much visual noise
            if (!UnityEditor.Selection.gameObjects.Contains(gameObject))
            {
                return;
            }

            var drawScale = GetDrawScale();
            if (pivotNode != transform.parent)
            {
                HandlesUtil.DrawTransform(pivotNode, drawScale);
            }
            DrawAngleLimits(drawScale);

            var drawColor = GetDrawColor(true);
            DrawSpringBone(drawColor);

            var ColliderColor = new Color(0.8f, 0.8f, 1f);
            if (sphereColliders != null)
            {
                foreach (var collider in sphereColliders.Where(collider => collider != null))
                {
                    collider.DrawGizmos(ColliderColor);
                }
            }

            if (capsuleColliders != null)
            {
                foreach (var collider in capsuleColliders.Where(collider => collider != null))
                {
                    collider.DrawGizmos(ColliderColor);
                }
            }

            if (panelColliders != null)
            {
                foreach (var collider in panelColliders.Where(collider => collider != null))
                {
                    collider.DrawGizmos(ColliderColor);
                }
            }

            if (lengthLimitTargets != null
                && lengthLimitTargets.Length > 0)
            {
                DrawLinesToLimitTargets();
                for (int targetIndex = 0; targetIndex < lengthLimitTargets.Length; targetIndex++)
                {
                    var target = lengthLimitTargets[targetIndex];
                    if (target != null)
                    {
                        var targetSpringBone = target.GetComponent<SpringBone>();
                        if (targetSpringBone != null)
                        {
                            var LimitTargetColor = new Color(0.5f, 1f, 0.5f);
                            Gizmos.color = LimitTargetColor;
                            if (SpringManager.showBoneSpheres)
                            {
                                Gizmos.DrawWireSphere(target.position, targetSpringBone.radius);
                            }
                        }
                    }
                }
            }
        }
#endif
    }
}
