using FUnit.GameObjectExtensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FUnit
{
    public static class SpringBoneSetup
    {
        public static List<GameObject> GetProbableSpringBones(GameObject root)
        {
            const string DynamicsBonePrefix = "j_";
            string[] IgnoreSuffixes = { "pivot", "end" };
            string[] IgnoredBones = { "j_1_katana", "j_1_saya" };

            var allChildren = root.GetComponentsInChildren<Transform>(true);
            var probableBones = new List<GameObject>();
            foreach (var child in allChildren)
            {
                var lowercaseName = child.name.ToLowerInvariant();
                if (lowercaseName.StartsWith(DynamicsBonePrefix)
                    && !IgnoreSuffixes.Any(item => lowercaseName.EndsWith(item))
                    && !IgnoredBones.Any(item => lowercaseName == item))
                {
                    probableBones.Add(child.gameObject);
                }
            }
            return probableBones;
        }

        // Warning: maybe dangerous!
        public static void DestroyPivotObjects(GameObject rootObject)
        {
            if (rootObject == null) { return; }

            var springBones = rootObject.GetComponentsInChildren<SpringBone>(true);
            var pivots = from springBone in springBones
                         where springBone.pivotNode != null
                         select springBone.pivotNode;
            var skinBones = GameObjectUtil.GetAllBones(rootObject);

            var maybeSafeToDestroyPivots = from pivot in pivots
                                           where IsPivotProbablySafeToDestroy(pivot, skinBones)
                                           select pivot;

            var objectsToDestroy = maybeSafeToDestroyPivots.Select(pivot => pivot.gameObject).ToList();
            var objectCount = objectsToDestroy.Count;
            for (int objectIndex = 0; objectIndex < objectCount; objectIndex++)
            {
                Object.DestroyImmediate(objectsToDestroy[objectIndex]);
            }
        }

        // オブジェクトとその子供に当たっている全てのSpringBoneとSpringManagerを削除
        // この関数はDestroyImmediateを使うのでエディターの時以外は使わないでください！
        public static void DestroySpringManagersAndBones(GameObject rootObject)
        {
            DestroyPivotObjects(rootObject);

            var springManagers = rootObject.GetComponentsInChildren<SpringManager>(true);
            foreach (var manager in springManagers)
            {
                UnityEngine.Object.DestroyImmediate(manager);
            }

            var springBones = rootObject.GetComponentsInChildren<SpringBone>(true);
            foreach (var springBone in springBones)
            {
                UnityEngine.Object.DestroyImmediate(springBone);
            }
        }

        // 全子供にSpringBoneを見つけて、springManagerに割り当てる
        public static void FindAndAssignSpringBones(SpringManager springManager, bool includeInactive = false)
        {
            if (springManager != null)
            {
                var sortedBones = GetSpringBonesSortedByDepth(springManager.gameObject, includeInactive);
                springManager.springBones = sortedBones.ToArray();
            }
        }

        // オブジェクトとその子供にSpringBoneを当てる
        public static void AssignSpringBonesRecursively(Transform rootObject)
        {
            if (rootObject.childCount == 0) { return; }

            var springBone = rootObject.gameObject.GetComponent<SpringBone>();
            if (springBone == null)
            {
                springBone = rootObject.gameObject.AddComponent<SpringBone>();
            }

            for (var childIndex = 0; childIndex < rootObject.childCount; ++childIndex)
            {
                var child = rootObject.GetChild(childIndex);
                AssignSpringBonesRecursively(child);
            }
        }

        public static Dictionary<Transform, List<SpringBone>> GetPivotToSpringBoneMap(GameObject rootObject)
        {
            var skinBones = GameObjectUtil.GetAllBones(rootObject);
            var springBones = from springBone in rootObject.GetComponentsInChildren<SpringBone>(true)
                              where springBone.pivotNode != null
                              && !skinBones.Contains(springBone.pivotNode)
                              select springBone;

            // Collect pivots and their bones
            var pivotDictionary = new Dictionary<Transform, List<SpringBone>>();
            foreach (var springBone in springBones)
            {
                List<SpringBone> pivotBones = null;
                if (!pivotDictionary.TryGetValue(springBone.pivotNode, out pivotBones))
                {
                    pivotBones = new List<SpringBone>();
                }
                pivotBones.Add(springBone);
                pivotDictionary[springBone.pivotNode] = pivotBones;
            }
            return pivotDictionary;
        }

        // 基点オブジェクトの位置をすべて合わせる
        public static void FixAllPivotNodePositions(GameObject rootObject)
        {
            var pivotToSpringBoneMap = GetPivotToSpringBoneMap(rootObject);
            foreach (var item in pivotToSpringBoneMap)
            {
                var position = Vector3.zero;
                var springBones = item.Value;
                foreach (var springBone in springBones)
                {
                    position += springBone.transform.position;
                }
                position /= springBones.Count;
                item.Key.position = position;
            }
        }

        // 基点ノードを作成
        public static GameObject CreateSpringPivotNode(SpringBone springBone)
        {
            var pivotObject = new GameObject(springBone.name + "_Pivot");
            pivotObject.transform.parent = springBone.transform.parent;
            pivotObject.transform.rotation = GetPivotRotation(springBone);
            pivotObject.transform.position = springBone.transform.position;
            pivotObject.AddComponent<SpringBonePivot>();

            var oldPivotNode = springBone.pivotNode;
            if (oldPivotNode != null)
            {
                var skinBones = GameObjectUtil.GetAllBones(springBone.transform.root.gameObject);
                if (IsPivotProbablySafeToDestroy(oldPivotNode, skinBones))
                {
                    Object.DestroyImmediate(oldPivotNode.gameObject);
                }
            }

            springBone.pivotNode = pivotObject.transform;

            springBone.yAngleLimits.active = true;
            if (springBone.yAngleLimits.min > -0.5f && springBone.yAngleLimits.max < 0.5f)
            {
                springBone.yAngleLimits.min = -20f;
                springBone.yAngleLimits.max = 20f;
            }

            springBone.zAngleLimits.active = true;
            if (springBone.zAngleLimits.min > -0.5f && springBone.zAngleLimits.max < 0.5f)
            {
                springBone.zAngleLimits.min = 0f;
                springBone.zAngleLimits.max = 20f;
            }

            return pivotObject;
        }

#if UNITY_EDITOR
        public static void AutoAssignLengthLimitTargetsToSelectedBones()
        {
            var chainRoots = from item in UnityEditor.Selection.transforms
                             where item.gameObject.GetComponent<SpringBone>() != null
                             select item.gameObject.GetComponent<SpringBone>();
            AutoAssignLengthLimitTargets(chainRoots);
        }
#endif

        public static void AutoAssignLengthLimitTargets(IEnumerable<SpringBone> chainRoots)
        {
            var chainInfos = chainRoots.Select(root => new ChainInfo(root));
#if UNITY_EDITOR
            var allSpringBones = chainInfos.SelectMany(chainInfo => chainInfo.springBones);
            UnityEditor.Undo.RecordObjects(allSpringBones.ToArray(), "Auto assign length limit targets");
#endif
            foreach (var chainInfo in chainInfos)
            {
                var neighborChains = new List<ChainInfo>(2);
                var sideVectors = new Vector2[] { chainInfo.sideVector, -chainInfo.sideVector };
                foreach (var sideVector in sideVectors)
                {
                    var possibleNeighbors = from possibleNeighbor in chainInfos
                                            where possibleNeighbor != chainInfo
                                            && Vector2.Dot(sideVector, possibleNeighbor.baseVector) > 0f
                                            select possibleNeighbor;
                    var closestNeighbor = FindClosestPolarNeighbor(chainInfo, possibleNeighbors);
                    if (closestNeighbor != null)
                    {
                        neighborChains.Add(closestNeighbor);
                    }
                }

                foreach (var bone in chainInfo.springBones)
                {
                    var lengthLimitTargets = new List<Transform>(2);
                    var endPosition = bone.ComputeChildPosition();
                    foreach (var neighborChain in neighborChains)
                    {
                        var lengthLimitTarget = FindClosestTransform(endPosition, neighborChain.transforms);
                        if (lengthLimitTarget != null)
                        {
                            lengthLimitTargets.Add(lengthLimitTarget);
                        }
                    }
                    bone.lengthLimitTargets = lengthLimitTargets.ToArray();
                }
            }
        }

        public static bool IsPivotProbablySafeToDestroy(Transform pivot, IEnumerable<Transform> skinBones)
        {
            // Definitely not safe to destroy
            if (skinBones.Contains(pivot)
                || pivot.childCount > 0
                || pivot.GetComponent<Renderer>() != null)
            {
                return false;
            }

            // Definitely safe to destroy
            if (pivot.GetComponent<SpringBonePivot>() != null)
            {
                return true;
            }

            // Probably safe to destroy
            // Todo: Think of other exclusion rules for pivots
            const string PivotSuffix = "_pivot";
            return pivot.name.ToLowerInvariant().EndsWith(PivotSuffix);
        }

        // private

        private class ChainInfo
        {
            public SpringBone Root { get { return springBones[0]; } }
            public SpringBone[] springBones;
            public Transform[] transforms;
            public Vector2 baseVector;
            public Vector2 sideVector;

            public ChainInfo(SpringBone rootBone)
            {
                springBones = rootBone.GetComponentsInChildren<SpringBone>(true);
                transforms = rootBone.GetComponentsInChildren<Transform>(true);
                var bonePosition = rootBone.transform.position;
                baseVector = new Vector2(bonePosition.x, bonePosition.z).normalized;
                sideVector = new Vector2(-baseVector.y, baseVector.x);
            }
        }

        private static Transform FindClosestTransform(Vector3 basePosition, IEnumerable<Transform> transforms)
        {
            var closestDistanceSquared = 0f;
            Transform closestTransform = null;
            foreach (var transform in transforms)
            {
                var distanceSquared = (basePosition - transform.position).sqrMagnitude;
                if (closestTransform == null
                    || distanceSquared < closestDistanceSquared)
                {
                    closestTransform = transform;
                    closestDistanceSquared = distanceSquared;
                }
            }
            return closestTransform;
        }

        private static ChainInfo FindClosestPolarNeighbor(ChainInfo baseInfo, IEnumerable<ChainInfo> possibleNeighbors)
        {
            var closestDot = -2f;
            ChainInfo closestNeighbor = null;
            foreach (var possibleNeighbor in possibleNeighbors)
            {
                var currentDot = Vector2.Dot(baseInfo.baseVector, possibleNeighbor.baseVector);
                if (currentDot >= closestDot)
                {
                    closestDot = currentDot;
                    closestNeighbor = possibleNeighbor;
                }
            }
            return closestNeighbor;
        }

        private static bool IsPivotSideDirectionValid(Vector3 lookDirection, Vector3 sideDirection)
        {
            return sideDirection.sqrMagnitude >= 0.1f
                && Mathf.Abs(Vector3.Dot(lookDirection, sideDirection)) < 0.99f;
        }

        private static Vector3 FindClosestMeshNormalToPoint(Transform rootObject, Vector3 sourcePoint)
        {
            var meshes = from renderer in rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                         where renderer.sharedMesh != null
                         && renderer.sharedMesh.vertexCount > 0
                         select renderer.sharedMesh;

            var closestDistanceSquared = 1000000f;
            var closestNormal = Vector3.up;
            foreach (var mesh in meshes)
            {
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                if (vertices != null
                    && normals != null
                    && vertices.Length == normals.Length)
                {
                    var closestVertexIndex = -1;
                    var vertexCount = vertices.Length;
                    for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                    {
                        var distanceSquared = (vertices[vertexIndex] - sourcePoint).sqrMagnitude;
                        if (distanceSquared < closestDistanceSquared)
                        {
                            closestVertexIndex = vertexIndex;
                            closestDistanceSquared = distanceSquared;
                        }
                    }

                    if (closestVertexIndex != -1)
                    {
                        closestNormal = mesh.normals[closestVertexIndex];
                    }
                }
            }

            return closestNormal;
        }

        private static bool TryToGetPivotSideDirection
        (
            SpringBone springBone,
            Vector3 lookDirection,
            out Vector3 sideDirection
        )
        {
            sideDirection = Vector3.up;
            var meshNormal = FindClosestMeshNormalToPoint(springBone.transform.root, springBone.transform.position);
            var upDirection = Vector3.Cross(meshNormal, lookDirection).normalized;
            var possibleSideDirection = Vector3.Cross(lookDirection, upDirection).normalized;
            var isSideDirectionValid = IsPivotSideDirectionValid(lookDirection, possibleSideDirection);
            if (isSideDirectionValid)
            {
                sideDirection = possibleSideDirection;
            }
            return isSideDirectionValid;
        }

        private static Quaternion GetPivotRotation(SpringBone springBone)
        {
            var lookDirection = springBone.ComputeChildPosition() - springBone.transform.position;
            lookDirection.Normalize();

            Vector3 sideDirection;
            if (!TryToGetPivotSideDirection(springBone, lookDirection, out sideDirection))
            {
                sideDirection = springBone.transform.position;
                sideDirection.y = 0f;
                sideDirection.Normalize();
                if (!IsPivotSideDirectionValid(lookDirection, sideDirection))
                {
                    sideDirection = Vector3.up;
                    if (!IsPivotSideDirectionValid(lookDirection, sideDirection))
                    {
                        sideDirection = Vector3.forward;
                    }
                }
            }

            var flattenedPosition = springBone.transform.position;
            flattenedPosition.y = 0f;
            if (Vector3.Dot(sideDirection, flattenedPosition) < 0f)
            {
                sideDirection = -sideDirection;
            }

            var upDirection = Vector3.Cross(lookDirection, sideDirection).normalized;
            var lookRotation = Quaternion.LookRotation(lookDirection, upDirection);
            var axisShift = Quaternion.Euler(180f, 90f, 0f);
            return lookRotation * axisShift;
        }

        private struct BoneDepthPair
        {
            public SpringBone bone;
            public int depth;
        }

        private static bool TryToAddItem<T>(Dictionary<string, T> map, string name, List<T> outputList)
        {
            T item;
            var succeeded = map.TryGetValue(name, out item);
            if (succeeded)
            {
                outputList.Add(item);
            }
            return succeeded;
        }

        private static List<BoneDepthPair> GetSortedSpringBonesWithDepth(GameObject rootObject, bool includeInactive)
        {
            var boneDepthList = new List<BoneDepthPair>();
            var unsortedSpringBones = rootObject.GetComponentsInChildren<SpringBone>(includeInactive);
            foreach (var springBone in unsortedSpringBones)
            {
                var boneDepth = GameObjectUtil.GetTransformDepth(springBone.transform);
                boneDepthList.Add(new BoneDepthPair { bone = springBone, depth = boneDepth });
            }
            boneDepthList.Sort((a, b) => a.depth.CompareTo(b.depth));
            return boneDepthList;
        }

        private static List<SpringBone> GetSpringBonesSortedByDepth(GameObject rootObject, bool includeInactive)
        {
            var boneDepthList = GetSortedSpringBonesWithDepth(rootObject, includeInactive);
            return boneDepthList.Select(item => item.bone).ToList();
        }
    }
}