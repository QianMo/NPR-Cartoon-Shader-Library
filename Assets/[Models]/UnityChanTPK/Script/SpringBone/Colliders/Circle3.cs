using UnityEngine;

namespace FUnit
{
    // 3D circle
    public struct Circle3
    {
        public Vector3 origin;
        public Vector3 upVector;
        public float radius;

        // Find tangent points on a circle that go through a point 
        // at distanceToOrigin along the circle's local x-axis
        public static bool FindCircleTangentPoints
        (
            float distanceToOrigin,
            float radius,
            ref Vector2 ta,
            ref Vector2 tb
        )
        {
            // http://jsfiddle.net/zxqCw/1/
            var dd = distanceToOrigin;
            if (dd <= radius)
            {
                // The point is inside the circle!
                return false;
            }

            var radiusOverDD = radius / dd;
            var a = Mathf.Asin(radiusOverDD);
            var b = Mathf.PI; // Mathf.Atan2(0f, -dd);
            var t = b - a;
            ta = radius * new Vector2(Mathf.Sin(t), -Mathf.Cos(t));
            t = b + a;
            tb = radius * new Vector2(-Mathf.Sin(t), Mathf.Cos(t));
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

#if UNITY_EDITOR
        public void DrawGizmos(float maximumRadians = 2f * Mathf.PI)
        {
            var sideVector1 = new Vector3(upVector.z, upVector.x, upVector.y);
            var sideVector2 = Vector3.Cross(upVector, sideVector1);
            const int PointCount = 32;
            var points = new Vector3[PointCount];
            var deltaAngle = maximumRadians / PointCount;
            for (int i = 0; i < PointCount; i++)
            {
                var angle = i * deltaAngle;
                points[i] = origin + radius * (Mathf.Cos(angle) * sideVector1 + Mathf.Sin(angle) * sideVector2);
            }

            for (int i = 0; i < PointCount; i++)
            {
                Gizmos.DrawLine(points[i], points[(i + 1) % PointCount]);
            }
            Gizmos.DrawLine(origin, origin + 0.1f * upVector);
        }
#endif
    }
}