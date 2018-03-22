using UnityEngine;

namespace FUnit
{
    public class SpringSphereCollider : MonoBehaviour
    {
        public float radius = 0.1f;

        // If linkedRenderer is not null, the collider will be enabled 
        // based on whether the renderer is
        public Renderer linkedRenderer;

        public bool Contains(Vector3 worldPosition)
        {
            var localPosition = transform.InverseTransformPoint(worldPosition);
            return localPosition.sqrMagnitude <= radius * radius;
        }

        public SpringBone.CollisionStatus CheckForCollisionAndReact
        (
            Vector3 headPosition,
            ref Vector3 tailPosition,
            float tailRadius
        )
        {
            var localHeadPosition = transform.InverseTransformPoint(headPosition);
            var localTailPosition = transform.InverseTransformPoint(tailPosition);
            var localTailRadius = transform.InverseTransformDirection(tailRadius, 0f, 0f).magnitude;

#if UNITY_EDITOR
            var originalLocalTailPosition = localTailPosition;
#endif

            var result = CheckForCollisionAndReact(
                localHeadPosition,
                ref localTailPosition,
                localTailRadius,
                new Vector3(0f, 0f, 0f),
                radius);

            if (result != SpringBone.CollisionStatus.NoCollision)
            {
#if UNITY_EDITOR
                RecordCollision(originalLocalTailPosition, tailRadius, result);
#endif
                tailPosition = transform.TransformPoint(localTailPosition);
            }

            return result;
        }

        public static SpringBone.CollisionStatus CheckForCollisionAndReact
        (
            Vector3 localHeadPosition,
            ref Vector3 localTailPosition,
            float localTailRadius,
            Vector3 sphereLocalOrigin,
            float sphereRadius
        )
        {
            var combinedRadius = sphereRadius + localTailRadius;
            if ((localTailPosition - sphereLocalOrigin).sqrMagnitude >= combinedRadius * combinedRadius)
            {
                // Not colliding
                return SpringBone.CollisionStatus.NoCollision;
            }

            var originToHead = localHeadPosition - sphereLocalOrigin;
            if (originToHead.sqrMagnitude <= sphereRadius * sphereRadius)
            {
                // The head is inside the sphere, so just try to push the tail out
                localTailPosition = 
                    sphereLocalOrigin + (localTailPosition - sphereLocalOrigin).normalized * combinedRadius;
                return SpringBone.CollisionStatus.HeadIsEmbedded;
            }

            var localHeadRadius = (localTailPosition - localHeadPosition).magnitude;
            var intersection = new Circle3();
            if (ComputeIntersection(
                localHeadPosition,
                localHeadRadius,
                sphereLocalOrigin,
                combinedRadius,
                ref intersection))
            {
                localTailPosition = ComputeNewTailPosition(intersection, localTailPosition);
            }

            return SpringBone.CollisionStatus.TailCollision;
        }

        // Line segment

        public static bool FindTangentPoint
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

        public static bool FindLineSegmentIntersection
        (
            Vector2 circleOrigin,
            float combinedRadius,
            Vector2 segmentPointA,
            Vector2 segmentPointB,
            out float t1,
            out float t2
        )
        {
            var combinedRadiusSquared = combinedRadius * combinedRadius;

            // https://math.stackexchange.com/a/929240
            var ca = segmentPointA - circleOrigin;
            var ab = segmentPointB - segmentPointA;
            var caDotAb = Vector2.Dot(ca, ab);
            var caSquared = ca.sqrMagnitude;
            var abSquared = ab.sqrMagnitude;

            var discriminant = 4f * caDotAb * caDotAb
                - 4f * abSquared * (caSquared - combinedRadiusSquared);
            if (discriminant < 0f)
            {
                t1 = t2 = 0f;
                return false;
            }

            var discriminantRoot = Mathf.Sqrt(discriminant);
            var twoCaAb = -2f * caDotAb;
            var tA = (twoCaAb + discriminantRoot) / (2f * abSquared);
            var tB = (twoCaAb - discriminantRoot) / (2f * abSquared);
            t1 = (tA < tB) ? tA : tB;
            t2 = (tA > tB) ? tA : tB;
            return true;
        }

        public static bool FindLineSegmentIntersection
        (
            Transform transform,
            Vector3 localSphereOrigin,
            float localSphereRadius,
            Vector3 worldPointA,
            Vector3 worldPointB,
            float worldRadius,
            out float t1,
            out float t2
        )
        {
            var pointA = transform.InverseTransformPoint(worldPointA);
            var pointB = transform.InverseTransformPoint(worldPointB);
            var otherRadius = transform.InverseTransformDirection(worldRadius, 0f, 0f).magnitude;
            var combinedRadius = localSphereRadius + otherRadius;
            var combinedRadiusSquared = combinedRadius * combinedRadius;

            // https://math.stackexchange.com/a/929240
            var ca = pointA - localSphereOrigin;
            var ab = pointB - pointA;
            var caDotAb = Vector3.Dot(ca, ab);
            var caSquared = ca.sqrMagnitude;
            var abSquared = ab.sqrMagnitude;

            var discriminant = 4f * caDotAb * caDotAb
                - 4f * abSquared * (caSquared - combinedRadiusSquared);
            if (discriminant < 0f)
            {
                t1 = t2 = 0f;
                return false;
            }

            var discriminantRoot = Mathf.Sqrt(discriminant);
            var twoCaAb = -2f * caDotAb;
            var tA = (twoCaAb + discriminantRoot) / (2f * abSquared);
            var tB = (twoCaAb - discriminantRoot) / (2f * abSquared);
            t1 = (tA < tB) ? tA : tB;
            t2 = (tA > tB) ? tA : tB;
            return true;
        }

        public static bool IntersectsLineSegment
        (
            Transform transform,
            Vector3 localSphereOrigin,
            float localSphereRadius,
            Vector3 pointA,
            Vector3 pointB,
            float segmentRadius
        )
        {
            float t1;
            float t2;
            return FindLineSegmentIntersection(
                transform, localSphereOrigin, localSphereRadius, pointA, pointB, segmentRadius, out t1, out t2)
                && t1 <= 1f
                && t2 >= 0f;
        }

        public bool IntersectsLineSegment(Vector3 pointA, Vector3 pointB, float segmentRadius)
        {
            float t1;
            float t2;
            return FindLineSegmentIntersection(
                transform, new Vector3(0f, 0f, 0f), radius, pointA, pointB, segmentRadius, out t1, out t2)
                && t1 <= 1f
                && t2 >= 0f;
        }

        public SpringBone.CollisionStatus CheckForLineSegmentCollisionAndReact
        (
            Vector3 fixedPosition,
            ref Vector3 moverPosition,
            float lineSegmentRadius
        )
        {
            if (!IntersectsLineSegment(fixedPosition, moverPosition, lineSegmentRadius))
            {
                return SpringBone.CollisionStatus.NoCollision;
            }

            var tangentPoint = new Vector3(0f, 0f, 0f);
            if (!FindTangentPoint(
                transform, new Vector3(0f, 0f, 0f), radius, fixedPosition, moverPosition, lineSegmentRadius, ref tangentPoint))
            {
                return SpringBone.CollisionStatus.HeadIsEmbedded;
            }

            var fixedToTangent = tangentPoint - fixedPosition;
            if (fixedToTangent.sqrMagnitude > 0.01f)
            {
                var originalLength = (moverPosition - fixedPosition).magnitude;
                var tangentDirection = fixedToTangent.normalized;
                moverPosition = fixedPosition + originalLength * tangentDirection;
            }
            return SpringBone.CollisionStatus.TailCollision;
        }

        // private

        // http://mathworld.wolfram.com/Sphere-SphereIntersection.html
        public static bool ComputeIntersection
        (
            Vector3 originA,
            float radiusA,
            Vector3 originB,
            float radiusB,
            ref Circle3 intersection
        )
        {
            var aToB = originB - originA;
            var dSqr = aToB.sqrMagnitude;
            var d = Mathf.Sqrt(dSqr);
            if (d <= 0f)
            {
                return false;
            }

            var radiusASqr = radiusA * radiusA;
            var radiusBSqr = radiusB * radiusB;

            // Assume a is at the origin and b is at (d, 0 0)
            var denominator = 0.5f / d;
            var subTerm = dSqr - radiusBSqr + radiusASqr;
            var x = subTerm * denominator;
            var squaredTerm = subTerm * subTerm;
            var intersectionRadius = Mathf.Sqrt(4f * dSqr * radiusASqr - squaredTerm) * denominator;

            var upVector = aToB / d;
            var origin = originA + x * upVector;

            intersection.origin = origin;
            intersection.upVector = upVector;
            intersection.radius = intersectionRadius;

            return true;
        }

        public static Vector3 ComputeNewTailPosition(Circle3 intersection, Vector3 tailPosition)
        {
            // http://stackoverflow.com/questions/300871/best-way-to-find-a-point-on-a-circle-closest-to-a-given-point
            // Project child's position onto the plane
            var newTailPosition = tailPosition
                - Vector3.Dot(intersection.upVector, tailPosition - intersection.origin) * intersection.upVector;
            var v = newTailPosition - intersection.origin;
            var newPosition = intersection.origin + intersection.radius * v.normalized;
            return newPosition;
        }


#if UNITY_EDITOR
        public void DrawGizmos(Color drawColor)
        {
            var worldRadius = transform.TransformDirection(radius, 0f, 0f).magnitude;
            // For picking
            Gizmos.color = new Color(0f, 0f, 0f, 0f);
            Gizmos.DrawWireSphere(transform.position, worldRadius);

            UnityEditor.Handles.color = drawColor;
            UnityEditor.Handles.RadiusHandle(Quaternion.identity, transform.position, worldRadius);
            if (colliderDebug != null)
            {
                colliderDebug.DrawGizmos();
            }
        }

        private SpringManager manager;
        private SpringColliderDebug colliderDebug;

        private void Awake()
        {
            manager = GetComponentInParent<SpringManager>();
            colliderDebug = new SpringColliderDebug();
        }

        private void Update()
        {
            colliderDebug.ClearCollisions();
        }

        private void OnDrawGizmos()
        {
            if (SpringManager.showColliders
                && !SpringBone.SelectionContainsSpringBones())
            {
                DrawGizmos((manager != null) ? manager.colliderColor : Color.gray);
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(Color.white);
        }

        private void RecordCollision
        (
            Vector3 localMoverPosition,
            float worldMoverRadius,
            SpringBone.CollisionStatus collisionStatus
        )
        {
            var localNormal = (localMoverPosition).normalized;
            var localContactPoint = localNormal * radius;
            var worldNormal = transform.TransformDirection(localNormal).normalized;
            var worldContactPoint = transform.TransformPoint(localContactPoint);
            colliderDebug.RecordCollision(worldContactPoint, worldNormal, worldMoverRadius, collisionStatus);
        }
#endif
    }
}
