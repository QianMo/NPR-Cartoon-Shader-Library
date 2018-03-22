using UnityEngine;

namespace FUnit
{
    // Up is y-axis
    public class SpringCapsuleCollider : MonoBehaviour
    {
        public float radius = 0.1f;
        public float height = 0.4f;
        
        // If linkedRenderer is not null, the collider will be enabled 
        // based on whether the renderer is
        public Renderer linkedRenderer;

        public SpringBone.CollisionStatus CheckForCollisionAndReact
        (
            Vector3 moverHeadPosition, 
            ref Vector3 moverPosition, 
            float moverRadius
        )
        {
            if ((linkedRenderer != null
                && !linkedRenderer.enabled)
                || radius <= 0.0001f)
            {
                return SpringBone.CollisionStatus.NoCollision;
            }

            if (needToCacheTransform)
            {
                CacheTransform();
            }

            // Lower than start cap
            var localHeadPosition = worldToLocal.MultiplyPoint3x4(moverHeadPosition);
            var localMoverPosition = worldToLocal.MultiplyPoint3x4(moverPosition);
            var localMoverRadius = moverRadius * radiusScale;

            var moverIsAboveTop = localMoverPosition.y >= height;
            var useSphereCheck = (localMoverPosition.y <= 0f) | moverIsAboveTop;
            if (useSphereCheck)
            {
                var sphereOrigin = new Vector3(0f, 0f, 0f);
                sphereOrigin.y = moverIsAboveTop ? height : 0f;

                var combinedRadius = localMoverRadius + radius;
                if ((localMoverPosition - sphereOrigin).sqrMagnitude >= combinedRadius * combinedRadius)
                {
                    // Not colliding
                    return SpringBone.CollisionStatus.NoCollision;
                }

                var originToHead = localHeadPosition - sphereOrigin;
                var isHeadEmbedded = originToHead.sqrMagnitude <= radius * radius;

#if UNITY_EDITOR
                RecordSphereCollision(
                    sphereOrigin,
                    localMoverPosition,
                    moverRadius,
                    isHeadEmbedded ? 
                        SpringBone.CollisionStatus.HeadIsEmbedded : 
                        SpringBone.CollisionStatus.TailCollision);
#endif

                if (isHeadEmbedded)
                {
                    // The head is inside the sphere, so just try to push the tail out
                    localMoverPosition =
                        sphereOrigin + (localMoverPosition - sphereOrigin).normalized * combinedRadius;
                    moverPosition = transform.TransformPoint(localMoverPosition);
                    return SpringBone.CollisionStatus.HeadIsEmbedded;
                }

                var localHeadRadius = (localMoverPosition - localHeadPosition).magnitude;
                var intersection = new Circle3();
                if (SpringSphereCollider.ComputeIntersection(
                    localHeadPosition,
                    localHeadRadius,
                    sphereOrigin,
                    combinedRadius,
                    ref intersection))
                {
                    localMoverPosition = SpringSphereCollider.ComputeNewTailPosition(intersection, localMoverPosition);
                    moverPosition = transform.TransformPoint(localMoverPosition);
                }

                return SpringBone.CollisionStatus.TailCollision;
            }

            // Cylinder
            var collisionStatus = CheckForCylinderCollisionAndReact(
                localHeadPosition, ref moverPosition, localMoverRadius, localMoverPosition);
            return collisionStatus;
        }

#if UNITY_EDITOR
        public static void GetRingPoints
        (
            Vector3 origin,
            Vector3 sideVector,
            Vector3 forwardVector,
            float scale,
            ref Vector3[] ringPoints
        )
        {
            var lastPoint = origin + scale * forwardVector;
            var pointCount = ringPoints.Length;
            var deltaAngle = 2f * Mathf.PI / (pointCount - 1);
            var angle = deltaAngle;
            ringPoints[0] = lastPoint;
            for (var iteration = 1; iteration < pointCount; ++iteration)
            {
                var newPoint = origin + scale * GetAngleVector(sideVector, forwardVector, angle);
                ringPoints[iteration] = newPoint;
                lastPoint = newPoint;
                angle += deltaAngle;
            }
        }

        public void DrawGizmos(Color drawColor)
        {
            const int PointCount = 16;

            UnityEditor.Handles.color = drawColor;

            if (ringPoints == null || ringPoints.Length != PointCount)
            {
                ringPoints = new Vector3[PointCount];
                endRingPoints = new Vector3[PointCount];
            }

            var worldRadius = transform.TransformDirection(radius, 0f, 0f).magnitude;

            var startCapOrigin = transform.position;
            var endCapOrigin = GetEndCapOriginInWorldSpace();
            AngleLimits.DrawAngleLimit(startCapOrigin, transform.up, transform.forward, -180f, worldRadius, drawColor);
            AngleLimits.DrawAngleLimit(startCapOrigin, transform.up, transform.right, -180f, worldRadius, drawColor);
            AngleLimits.DrawAngleLimit(endCapOrigin, transform.up, transform.forward, 180f, worldRadius, drawColor);
            AngleLimits.DrawAngleLimit(endCapOrigin, transform.up, transform.right, 180f, worldRadius, drawColor);

            GetRingPoints(startCapOrigin, transform.right, transform.forward, worldRadius, ref ringPoints);
            var startToEnd = endCapOrigin - startCapOrigin;
            for (int pointIndex = 0; pointIndex < PointCount; pointIndex++)
            {
                endRingPoints[pointIndex] = ringPoints[pointIndex] + startToEnd;
            }
            
            for (int pointIndex = 1; pointIndex < PointCount; pointIndex++)
            {
                UnityEditor.Handles.DrawLine(ringPoints[pointIndex - 1], ringPoints[pointIndex]);
                UnityEditor.Handles.DrawLine(endRingPoints[pointIndex - 1], endRingPoints[pointIndex]);
            }

            for (int pointIndex = 0; pointIndex < PointCount; pointIndex++)
            {
                UnityEditor.Handles.DrawLine(ringPoints[pointIndex], endRingPoints[pointIndex]);
            }

            if (colliderDebug != null)
            {
                colliderDebug.DrawGizmos();
            }
        }
#endif

        // private

        private Matrix4x4 worldToLocal;
        private float radiusScale;
        private bool needToCacheTransform = true;

        private void CacheTransform()
        {
            worldToLocal = transform.worldToLocalMatrix;
            radiusScale = worldToLocal.MultiplyVector(new Vector3(1f, 0f, 0f)).magnitude;
            needToCacheTransform = false;
        }

        private void Update()
        {
            needToCacheTransform = true;
#if UNITY_EDITOR
            colliderDebug.ClearCollisions();
#endif
        }

        private bool ContainsPointLocal(Vector3 point, float combinedRadius)
        {
            var combinedRadiusSquared = combinedRadius * combinedRadius;
            return (point.x * point.x + point.z * point.z) <= combinedRadiusSquared;
        }

        private SpringBone.CollisionStatus CheckForCylinderCollisionAndReact
        (
            Vector3 localHeadPosition,
            ref Vector3 worldMoverPosition,
            float localMoverRadius,
            Vector3 localSpherePosition
        )
        {
            var originToMover = new Vector2(localSpherePosition.x, localSpherePosition.z);
            var combinedRadius = radius + localMoverRadius;
            var collisionStatus = SpringBone.CollisionStatus.NoCollision;
            var collided = originToMover.sqrMagnitude <= combinedRadius * combinedRadius;
            if (collided)
            {

                var normal = originToMover.normalized;
                originToMover = combinedRadius * normal;
                var newLocalMoverPosition = new Vector3(originToMover.x, localSpherePosition.y, originToMover.y);
                worldMoverPosition = transform.TransformPoint(newLocalMoverPosition);

                var originToHead = new Vector2(localHeadPosition.x, localHeadPosition.z);
                collisionStatus = (originToHead.sqrMagnitude <= radius * radius) ?
                    SpringBone.CollisionStatus.HeadIsEmbedded :
                    SpringBone.CollisionStatus.TailCollision;
#if UNITY_EDITOR
                RecordCylinderCollision(
                    localSpherePosition, 
                    new Vector3(normal.x, 0f, normal.y), 
                    localMoverRadius, 
                    collisionStatus);
#endif
            }
            return collisionStatus;
        }

        private Vector3 GetEndCapOriginInWorldSpace()
        {
            return transform.TransformPoint(0f, height, 0f);
        }

        private bool IntersectsLineSegment
        (
            Vector3 pointA,
            Vector3 pointB,
            float segmentRadius
        )
        {
            if (SpringSphereCollider.IntersectsLineSegment(
                transform, new Vector3(0f, 0f, 0f), radius, pointA, pointB, segmentRadius))
            {
                return true;
            }

            if (SpringSphereCollider.IntersectsLineSegment(
                transform, new Vector3(0f, height, 0f), radius, pointA, pointB, segmentRadius))
            {
                return true;
            }

            var localPointA = transform.InverseTransformPoint(pointA);
            var localPointB = transform.InverseTransformPoint(pointB);
            var otherRadius = transform.InverseTransformDirection(segmentRadius, 0f, 0f).magnitude;
            var combinedRadius = radius + otherRadius;

            var intersectionPoint = new Vector3(0f, 0f, 0f);
            return ComputeCylinderIntersection(localPointA, localPointB, combinedRadius, ref intersectionPoint)
                && intersectionPoint.y > 0f
                && intersectionPoint.y < height;
        }

        private static bool ComputeCylinderIntersection
        (
            Vector3 localFixedPoint,
            Vector3 localMovingPoint,
            float combinedRadius,
            ref Vector3 intersectionPoint
        )
        {
            float t1;
            float t2;
            var projectedFixedPoint = new Vector2(localFixedPoint.x, localFixedPoint.z);
            var projectedMovingPoint = new Vector2(localMovingPoint.x, localMovingPoint.z);
            if (!SpringSphereCollider.FindLineSegmentIntersection(
                new Vector2(0f, 0f),
                combinedRadius,
                projectedFixedPoint,
                projectedMovingPoint,
                out t1,
                out t2)
                || t1 >= 1f
                || t2 <= 0f)
            {
                return false;
            }

            intersectionPoint = localFixedPoint + t1 * (localMovingPoint - localFixedPoint);
            return true;
        }

        private static bool FindTangentPoint
        (
            Transform transform,
            Vector3 localSphereOrigin,
            float localSphereRadius,
            Vector3 worldFixedPoint,
            Vector3 worldMovingPoint,
            float worldSegmentRadius,
            ref Vector3 tangentPoint
        )
        {
            var fixedPoint = transform.InverseTransformPoint(worldFixedPoint) - localSphereOrigin;
            var movingPoint = transform.InverseTransformPoint(worldMovingPoint) - localSphereOrigin;
            var otherRadius = transform.InverseTransformDirection(worldSegmentRadius, 0f, 0f).magnitude;
            var combinedRadius = localSphereRadius + otherRadius;

            var ta = new Vector2(0f, 0f);
            var tb = new Vector2(0f, 0f);
            var dd = fixedPoint.magnitude;
            if (!Circle3.FindCircleTangentPoints(dd, combinedRadius, ref ta, ref tb))
            {
                // The fixed point is inside the sphere!
                return false;
            }

            // todo: It seems like we should be able to rotate based on the angle in 3D directly somehow?
            var xVector = fixedPoint / dd;
            var fixedToMoving = movingPoint - fixedPoint;
            var yVector = (fixedToMoving - Vector3.Project(fixedToMoving, xVector)).normalized;
            var tangentPoint1 = ta.x * xVector + ta.y * yVector;
            var tangentPoint2 = tb.x * xVector + tb.y * yVector;
            var isTangentPoint1CloserToMover = (tangentPoint1 - movingPoint).sqrMagnitude < (tangentPoint2 - movingPoint).sqrMagnitude;
            var localTangentPoint = isTangentPoint1CloserToMover ? tangentPoint1 : tangentPoint2;
            tangentPoint = transform.TransformPoint(localTangentPoint + localSphereOrigin);
            return true;
        }

        private struct ContactPoint
        {
            public Vector3 position;
            public Vector3 normal;
        }

        private static bool ComputeContactPoint
        (
            Vector3 localHeadPoint,
            Vector3 localTailPoint,
            float combinedRadius,
            float capsuleLength,
            ref ContactPoint contactPoint
        )
        {
            // For cylinder only

            var projectedHead = new Vector3(localHeadPoint.x, 0f, localHeadPoint.z);
            var projectedTail = new Vector3(localTailPoint.x, 0f, localTailPoint.z);
            var rayVector = projectedTail - projectedHead;

            // http://mathworld.wolfram.com/Circle-CircleIntersection.html
            // First find the intersection of the projected circles of the sphere and the cylinder seen from above
            var cylinderToHead = projectedHead;
            var d = cylinderToHead.magnitude;
            if (d <= 0f)
            {
                // The head lies along the cylinder axis!
                return false;
            }

            var dSqr = d * d;
            var radius1 = combinedRadius;
            var radius1Sqr = radius1 * radius1;
            var radius2Sqr = rayVector.sqrMagnitude;

            var subTerm = dSqr - radius2Sqr + radius1Sqr;
            var denominator = 0.5f / d;
            var x = subTerm * denominator;
            var y = denominator * Mathf.Sqrt(4f * dSqr * radius1Sqr - subTerm * subTerm);

            var xVector = cylinderToHead / d;
            var yVector = new Vector3(-xVector.z, 0f, xVector.x);
            var xB = x * xVector;
            var yB = y * yVector;

            var newProjectedPosition = xB + yB;
            var boneLengthSqr = (localTailPoint - localHeadPoint).sqrMagnitude;
            newProjectedPosition.y = Mathf.Sqrt(boneLengthSqr - (d - x) * (d - x));

            contactPoint.position = newProjectedPosition;
            contactPoint.normal = xVector;
            return true;
        }

#if UNITY_EDITOR
        private SpringManager manager;
        private Vector3[] ringPoints;
        private Vector3[] endRingPoints;
        private SpringColliderDebug colliderDebug;

        private static Vector3 GetAngleVector(Vector3 sideVector, Vector3 forwardVector, float radians)
        {
            return Mathf.Sin(radians) * sideVector + Mathf.Cos(radians) * forwardVector;
        }

        private void Awake()
        {
            manager = GetComponentInParent<SpringManager>();
            colliderDebug = new SpringColliderDebug();
        }

        // Box for picking in the editor
        private void DrawPickBox()
        {
            Gizmos.color = new Color(0f, 0f, 0f, 0f);
            var start = transform.position;
            var end = GetEndCapOriginInWorldSpace();
            var center = 0.5f * (start + end);
            var worldRadius = 1.5f * transform.TransformDirection(radius, 0f, 0f).magnitude;
            var size = new Vector3(
                Mathf.Abs(end.x - start.x) + worldRadius,
                Mathf.Abs(end.y - start.y) + worldRadius,
                Mathf.Abs(end.z - start.z) + worldRadius);
            Gizmos.DrawCube(center, size);
        }

        private void OnDrawGizmos()
        {
            if (!enabled)
            {
                return;
            }

            DrawPickBox();

            if (SpringManager.showColliders
                && !SpringBone.SelectionContainsSpringBones())
            {
                DrawGizmos((manager != null) ? manager.colliderColor : Color.gray);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (SpringManager.onlyShowSelectedBones
                && !UnityEditor.Selection.Contains(gameObject))
            {
                return;
            }

            DrawGizmos(enabled ? Color.white : Color.gray);
        }

        private void RecordCylinderCollision
        (
            Vector3 localMoverPosition,
            Vector3 localNormal,
            float localMoverRadius,
            SpringBone.CollisionStatus collisionStatus
        )
        {
            var originToContactPoint = radius * localNormal;
            var localContactPoint = new Vector3(originToContactPoint.x, localMoverPosition.y, originToContactPoint.z);
            var worldContactPoint = transform.TransformPoint(localContactPoint);
            var worldNormal = transform.TransformDirection(localNormal).normalized;
            var worldRadius = transform.TransformDirection(localMoverRadius, 0f, 0f).magnitude;
            colliderDebug.RecordCollision(
                worldContactPoint,
                worldNormal,
                worldRadius,
                collisionStatus);
        }

        private void RecordSphereCollision
        (
            Vector3 localSphereOrigin,
            Vector3 localMoverPosition,
            float worldMoverRadius,
            SpringBone.CollisionStatus collisionStatus
        )
        {
            var localNormal = (localMoverPosition - localSphereOrigin).normalized;
            var localContactPoint = localSphereOrigin + localNormal * radius;
            var worldNormal = transform.TransformDirection(localNormal).normalized;
            var worldContactPoint = transform.TransformPoint(localContactPoint);
            colliderDebug.RecordCollision(worldContactPoint, worldNormal, worldMoverRadius, collisionStatus);
        }
#endif
    }
}