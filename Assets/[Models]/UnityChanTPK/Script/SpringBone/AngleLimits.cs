using UnityEngine;

namespace FUnit
{
    [System.Serializable]
    public class AngleLimits
    {
        public bool active = false;
        [Range(-180f, 0f)]
        public float min = 0f;
        [Range(0f, 180f)]
        public float max = 0f;

        public static Vector3 GetAngleVector(Vector3 sideVector, Vector3 forwardVector, float degrees)
        {
            var radians = Mathf.Deg2Rad * degrees;
            return Mathf.Sin(radians) * sideVector + Mathf.Cos(radians) * forwardVector;
        }

        public void CopyTo(AngleLimits target)
        {
            target.active = active;
            target.min = min;
            target.max = max;
        }

        // Returns true if exceeded bounds
        public bool ConstrainVector
        (
            Vector3 basisSide,
            Vector3 basisUp,
            Vector3 basisForward,
            float springStrength,
            float deltaTime,
            ref Vector3 vector
        )
        {
            var upProjection = Vector3.Project(vector, basisUp);
            var projection = vector - upProjection;
            var projectionMagnitude = projection.magnitude;
            var originalSine = Vector3.Dot(projection / projectionMagnitude, basisSide);
            // The above math might have a bit of floating point error 
            // so clamp the sine value into a valid range so we don't get NaN later
            originalSine = Mathf.Clamp(originalSine, -1f, 1f);

            // Use soft limits based on Hooke's Law to reduce jitter,
            // then apply hard limits
            var newAngle = Mathf.Rad2Deg * Mathf.Asin(originalSine);
            var acceleration = -newAngle * springStrength;
            newAngle += acceleration * deltaTime * deltaTime;

            var minAngle = min;
            var maxAngle = max;
            var preClampAngle = newAngle;
            newAngle = Mathf.Clamp(newAngle, minAngle, maxAngle);

            var radians = Mathf.Deg2Rad * newAngle;
            var newProjection = Mathf.Sin(radians) * basisSide + Mathf.Cos(radians) * basisForward;
            newProjection *= projectionMagnitude;
            vector = newProjection + upProjection;

            return newAngle != preClampAngle;
        }

#if UNITY_EDITOR
        // Rendering of Gizmos / Handles in the editor
        public void DrawLimits
        (
            Vector3 origin,
            Vector3 sideVector,
            Vector3 forwardVector,
            float drawScale,
            Color color
        )
        {
            DrawAngleLimit(origin, sideVector, forwardVector, min, drawScale, color);
            DrawAngleLimit(origin, sideVector, forwardVector, max, drawScale, color);
            UnityEditor.Handles.color = Color.gray;
            UnityEditor.Handles.DrawLine(origin, origin + drawScale * forwardVector);
        }

        public void DrawProjectedVector
        (
            Vector3 origin,
            Vector3 vector,
            Vector3 upVector,
            float drawScale,
            Color color
        )
        {
            var projectedVector = Vector3.ProjectOnPlane(vector, upVector);
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawLine(origin, origin + drawScale * projectedVector);
        }

        public static void DrawAngleLimit
        (
            Vector3 origin,
            Vector3 sideVector,
            Vector3 forwardVector,
            float angleLimit,
            float scale,
            Color color
        )
        {
            const int BaseIterationCount = 3;

            UnityEditor.Handles.color = color;
            var lastPoint = origin + scale * forwardVector;
            var iterationCount = (Mathf.RoundToInt(Mathf.Abs(angleLimit) / 45f) + 1) * BaseIterationCount;
            var deltaAngle = angleLimit / iterationCount;
            var angle = deltaAngle;
            for (var iteration = 0; iteration < iterationCount; ++iteration)
            {
                var newPoint = origin + scale * GetAngleVector(sideVector, forwardVector, angle);
                UnityEditor.Handles.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
                angle += deltaAngle;
            }
            UnityEditor.Handles.DrawLine(origin, lastPoint);
        }
#endif
    }
}