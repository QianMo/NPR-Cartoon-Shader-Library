using UnityEngine;

namespace FUnit
{
    public class SpringPanelCollider : MonoBehaviour
    {
        public float width = 0.25f;
        public float height = 0.25f;

        // If linkedRenderer is not null, the collider will be enabled 
        // based on whether the renderer is
        public Renderer linkedRenderer;

        public Vector3 GetPlaneNormal()
        {
            return transform.forward;
        }

        public SpringBone.CollisionStatus CheckForCollisionAndReact
        (
            Vector3 headPosition, 
            float length, 
            ref Vector3 tailPosition, 
            float tailRadius
        )
        {
            if (linkedRenderer != null
                && !linkedRenderer.enabled)
            {
                return SpringBone.CollisionStatus.NoCollision;
            }

            var localHeadPosition = transform.InverseTransformPoint(headPosition);
            var localLength = transform.InverseTransformDirection(length, 0f, 0f).magnitude;
            var localTailPosition = transform.InverseTransformPoint(tailPosition);
            var localTailRadius = transform.InverseTransformDirection(tailRadius, 0f, 0f).magnitude;

            var adjustedWidth = 0.5f * width + localTailRadius;
            var adjustedHeight = 0.5f * height + localTailRadius;

            var tailOutOfBounds = (localTailPosition.y <= -adjustedHeight)
                || (localTailPosition.y >= adjustedHeight)
                || (localTailPosition.x <= -adjustedWidth)
                || (localTailPosition.x >= adjustedWidth);

            var headOutOfBounds = (localHeadPosition.y <= -adjustedHeight)
                || (localHeadPosition.y >= adjustedHeight)
                || (localHeadPosition.x <= -adjustedWidth)
                || (localHeadPosition.x >= adjustedWidth);

            if (tailOutOfBounds && headOutOfBounds)
            {
                return SpringBone.CollisionStatus.NoCollision;
            }

            var collisionStatus = CheckForCollisionWithAlignedPlaneAndReact(
                localHeadPosition, localLength, ref localTailPosition, localTailRadius, Axis.Z);
            if (collisionStatus != SpringBone.CollisionStatus.NoCollision)
            {
#if UNITY_EDITOR
                RecordCollision(tailPosition, tailRadius, collisionStatus);
#endif
                tailPosition = transform.TransformPoint(localTailPosition);
            }
            return collisionStatus;
        }

        public enum Axis
        {
            X = 0,
            Y,
            Z, 
            AxisCount
        }

        public static SpringBone.CollisionStatus CheckForCollisionWithAlignedPlaneAndReact
        (
            Vector3 localHeadPosition,
            float localLength,
            ref Vector3 localTailPosition,
            float localTailRadius,
            Axis upAxis
        )
        {
            var zIndex = (int)upAxis;

            if (localTailPosition[zIndex] >= localTailRadius)
            {
                return SpringBone.CollisionStatus.NoCollision;
            }

            var collisionStatus = SpringBone.CollisionStatus.TailCollision;
            var newLocalTailPosition = localHeadPosition;
            if (localHeadPosition[zIndex] + localLength <= localTailRadius)
            {
                // Bone is completely embedded
                newLocalTailPosition[zIndex] += localLength;
                collisionStatus = SpringBone.CollisionStatus.HeadIsEmbedded;
            }
            else
            {
                var xIndex = (zIndex + 1) % (int)Axis.AxisCount;
                var yIndex = (zIndex + 2) % (int)Axis.AxisCount;

                var heightAboveRadius = localHeadPosition[zIndex] - localTailRadius;
                var projectionLength = Mathf.Sqrt(localLength * localLength - heightAboveRadius * heightAboveRadius);
                var localBoneVector = localTailPosition - localHeadPosition;
                var projectionVector = new Vector2(localBoneVector[xIndex], localBoneVector[yIndex]);
                var projectionVectorLength = projectionVector.magnitude;
                if (projectionVectorLength > 0.001f)
                {
                    var projection = (projectionLength / projectionVectorLength) * projectionVector;
                    newLocalTailPosition[xIndex] += projection.x;
                    newLocalTailPosition[yIndex] += projection.y;
                    newLocalTailPosition[zIndex] = localTailRadius;
                }
            }
            localTailPosition = newLocalTailPosition;
            return collisionStatus;
        }

#if UNITY_EDITOR
        public void DrawGizmos(Color color)
        {
            const int PointCount = 4;

            if (localGizmoPoints == null
                || worldGizmoPoints == null
                || localGizmoPoints.Length < PointCount
                || worldGizmoPoints.Length < PointCount)
            {
                localGizmoPoints = new Vector3[PointCount];
                worldGizmoPoints = new Vector3[PointCount];
            }

            var halfWidth = 0.5f * width;
            var halfHeight = 0.5f * height;
            localGizmoPoints[0] = new Vector3(-halfWidth, -halfHeight, 0f);
            localGizmoPoints[1] = new Vector3( halfWidth, -halfHeight, 0f);
            localGizmoPoints[2] = new Vector3( halfWidth,  halfHeight, 0f);
            localGizmoPoints[3] = new Vector3(-halfWidth,  halfHeight, 0f);

            for (int pointIndex = 0; pointIndex < PointCount; pointIndex++)
            {
                worldGizmoPoints[pointIndex] = transform.TransformPoint(localGizmoPoints[pointIndex]);
            }

            UnityEditor.Handles.color = color;
            for (int pointIndex = 0; pointIndex < PointCount; pointIndex++)
            {
                var endPointIndex = (pointIndex + 1) % PointCount;
                UnityEditor.Handles.DrawLine(worldGizmoPoints[pointIndex], worldGizmoPoints[endPointIndex]);
                UnityEditor.Handles.DrawLine(worldGizmoPoints[pointIndex], worldGizmoPoints[pointIndex] - 0.15f * transform.forward);
            }

            HandlesUtil.DrawArrow(transform.position, transform.position + transform.forward * 0.1f, color, 0.1f);

            if (colliderDebug != null)
            {
                colliderDebug.DrawGizmos();
            }
        }

        private SpringManager manager;
        private Vector3[] localGizmoPoints;
        private Vector3[] worldGizmoPoints;
        private SpringColliderDebug colliderDebug;

        private void OnDrawGizmos()
        {
            if (!enabled)
            {
                return;
            }

            if (manager == null)
            {
                manager = GetComponentInParent<SpringManager>();
            }

            if (SpringManager.showColliders
                && !SpringBone.SelectionContainsSpringBones())
            {
                DrawGizmos((manager != null) ? manager.colliderColor : Color.gray);
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(enabled ? Color.white : Color.gray);
        }

        private void RecordCollision
        (
            Vector3 tailPosition, 
            float tailRadius, 
            SpringBone.CollisionStatus collisionStatus
        )
        {
            var planeNormal = GetPlaneNormal();
            var planeOrigin = transform.position;
            var planeToCollision = tailPosition - planeOrigin;
            var normalProjection = Vector3.Dot(planeToCollision, planeNormal) * planeNormal;
            var projectedCollisionPoint = tailPosition - normalProjection;
            colliderDebug.RecordCollision(
                projectedCollisionPoint, planeNormal, tailRadius, collisionStatus);
        }

        private void Awake()
        {
            colliderDebug = new SpringColliderDebug();
        }

        private void Update()
        {
            colliderDebug.ClearCollisions();
        }
#endif
    }
}