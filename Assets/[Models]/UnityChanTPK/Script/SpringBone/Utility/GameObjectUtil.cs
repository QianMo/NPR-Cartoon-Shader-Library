using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FUnit
{
    namespace GameObjectExtensions
    {
        public static class GameObjectUtil
        {
            public enum SearchOptions
            {
                None,
                IgnoreNamespace // Maya風「namespace:objectname」の「namespace:」の部分を無視
            }

            public static T AcquireComponent<T>
            (
                this GameObject inGameObject,
                bool inCreateIfNotFound = false
            ) where T : Component
            {
                var component = inGameObject.GetComponent<T>();
                if (null == component && inCreateIfNotFound)
                {
                    component = inGameObject.AddComponent<T>();
                }
                return component;
            }

            // ioComponentがnullの場合、inGameObjectとから最初に見つけたTを取得。
            // 存在しなかった場合はfalseを返す。
            public static bool AcquireComponent<T>(this GameObject inGameObject, ref T ioComponent)
            {
                if (ioComponent == null)
                {
                    ioComponent = inGameObject.GetComponent<T>();
                }
                return ioComponent != null;
            }

            public static IEnumerable<Transform> GetAllSceneTransforms()
            {
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var rootObjects = activeScene.GetRootGameObjects();
                var allTransforms = new List<Transform>();
                foreach (var rootObject in rootObjects)
                {
                    allTransforms.AddRange(rootObject.GetComponentsInChildren<Transform>(true));
                }
                return allTransforms;
            }

            // ioComponentがnullの場合、inGameObjectとその子供たちから最初に見つけたTを取得。
            // 存在しなかった場合はfalseを返す。
            public static bool AcquireComponentInChildren<T>
            (
                this GameObject inGameObject,
                ref T ioComponent,
                bool inCreateIfNotFound = false
            ) where T : Component
            {
                if (ioComponent == null)
                {
                    ioComponent = inGameObject.GetComponentInChildren<T>();
                }

                if (ioComponent == null && inCreateIfNotFound)
                {
                    ioComponent = inGameObject.AddComponent<T>();
                }

                return ioComponent != null;
            }

            // 指定したクラスの名前→コンポーネントのマップを作成
            public static Dictionary<string, T> BuildNameToComponentMap<T>
            (
                this GameObject rootObject,
                bool includeInactive
            ) where T : Component
            {
                var components = rootObject.GetComponentsInChildren<T>(includeInactive);
                return components.ToDictionary(item => item.name, item => item);
            }

            public static List<Transform> FindAllBonesByAnySubstring(this GameObject inRootObject, string[] inSubstrings)
            {
                var bones = new List<Transform>();
                var substringCount = inSubstrings.Length;
                for (int substringIndex = 0; substringIndex < substringCount; substringIndex++)
                {
                    FindAllBonesBySubstring(inRootObject, inSubstrings[substringIndex], bones);
                }
                return bones;
            }

            public static List<Transform> FindAllBonesBySubstring
            (
                this GameObject inRootObject,
                string inSubstring
            )
            {
                var list = new List<Transform>();
                FindAllBonesBySubstring(inRootObject, inSubstring, list);
                return list;
            }

            public static void FindAllBonesBySubstring
            (
                this GameObject inRootObject,
                string inSubstring,
                List<Transform> outBones
            )
            {
                var bones = GetAllBones(inRootObject);
                var upperSubstring = inSubstring.ToUpperInvariant();
                var matchingBones = from bone in bones
                                    where bone.name.ToUpperInvariant().Contains(upperSubstring)
                                    select bone;
                outBones.AddRange(matchingBones);
            }

            public static IEnumerable<Transform> GetAllBones(this GameObject rootObject)
            {
                var skinnedMeshRenderers = rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var bones = new HashSet<Transform>();
                foreach (var renderer in skinnedMeshRenderers)
                {
                    var rendererBones = renderer.bones;
                    foreach (var bone in rendererBones)
                    {
                        bones.Add(bone);
                    }
                }
                return bones;
            }

            public static List<Transform> FindAllChildrenByAnySubstring(this GameObject inRootObject, string[] inSubstrings)
            {
                var children = new List<Transform>();
                var substringCount = inSubstrings.Length;
                for (int substringIndex = 0; substringIndex < substringCount; substringIndex++)
                {
                    FindAllChildrenBySubstring(inRootObject, inSubstrings[substringIndex], children);
                }
                return children;
            }

            public static List<Transform> FindAllChildrenBySubstring
            (
                this GameObject inRootObject,
                string inSubstring
            )
            {
                var children = new List<Transform>();
                FindAllChildrenBySubstring(inRootObject, inSubstring, children);
                return children;
            }

            public static void FindAllChildrenBySubstring
            (
                this GameObject inRootObject,
                string inSubstring,
                List<Transform> outChildren
            )
            {
                var upperSubstring = inSubstring.ToUpperInvariant();
                var children = inRootObject.GetComponentsInChildren<Transform>();
                var childCount = children.Length;
                for (var childIndex = 0; childIndex < childCount; childIndex++)
                {
                    var child = children[childIndex];
                    if (child.name.ToUpperInvariant().Contains(upperSubstring))
                    {
                        outChildren.Add(child);
                    }
                }
            }

            public static IEnumerable<SkinnedMeshRenderer> FindBlendShapeRenderers(this GameObject rootObject)
            {
                return rootObject.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(renderer => renderer.sharedMesh != null
                        && renderer.sharedMesh.blendShapeCount > 0);
            }

            public static int FindBlendShapeTargetIndex(SkinnedMeshRenderer renderer, string targetName)
            {
                var sharedMesh = renderer.sharedMesh;
                var targetCount = (sharedMesh != null) ? sharedMesh.blendShapeCount : 0;
                var periodPrefixedName = "." + targetName;
                for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
                {
                    var currentTargetName = sharedMesh.GetBlendShapeName(targetIndex);
                    if (currentTargetName == targetName || currentTargetName.EndsWith(periodPrefixedName))
                    {
                        return targetIndex;
                    }
                }
                return -1;
            }

            public static Transform FindBoneByAnySubstring(this GameObject inRootObject, string[] inSubstrings)
            {
                var substringCount = inSubstrings.Length;
                for (var substringIndex = 0; substringIndex < substringCount; substringIndex++)
                {
                    var bone = FindBoneBySubstring(inRootObject, inSubstrings[substringIndex]);
                    if (bone != null)
                    {
                        return bone;
                    }
                }
                return null;
            }

            // 名前にinSubstringが入っているボーンを検索します。
            public static Transform FindBoneBySubstring(this GameObject inRootObject, string inSubstring)
            {
                var bones = GetAllBones(inRootObject);
                var upperSubstring = inSubstring.ToUpperInvariant();
                return bones.FirstOrDefault(item => item.name.ToUpperInvariant().Contains(upperSubstring));
            }

            public static Transform FindChildByName
            (
                this GameObject inRoot,
                string inName,
                SearchOptions searchOptions = SearchOptions.IgnoreNamespace
            )
            {
                return FindChildComponentByName<Transform>(inRoot, inName, searchOptions);
            }

            // 子供の指定した名前のオブジェクトを検出
            public static T FindChildComponentByName<T>
            (
                this GameObject inRoot,
                string inName,
                SearchOptions searchOptions = SearchOptions.IgnoreNamespace
            ) where T : Component
            {
                var lowerName = inName.ToLowerInvariant();
                if (searchOptions == SearchOptions.IgnoreNamespace)
                {
                    lowerName = RemoveNamespaceFromName(lowerName);
                }

                var children = inRoot.GetComponentsInChildren<T>();
                var childCount = children.Length;
                for (int childIndex = 0; childIndex < childCount; childIndex++)
                {
                    var child = children[childIndex];
                    var childName = child.gameObject.name.ToLowerInvariant();
                    if (searchOptions == SearchOptions.IgnoreNamespace)
                    {
                        childName = RemoveNamespaceFromName(childName);
                    }

                    if (childName == lowerName)
                    {
                        return child;
                    }
                }
                return null;
            }

            // 子供の指定した名前のオブジェクトを検出
            public static T[] FindChildComponentsByName<T>
            (
                this GameObject inRoot,
                string[] inNames,
                SearchOptions searchOptions = SearchOptions.IgnoreNamespace
            ) where T : Component
            {
                var children = inRoot.GetComponentsInChildren<T>();
                var outputList = new List<T>();
                var childCount = children.Length;
                for (var childIndex = 0; childIndex < childCount; ++childIndex)
                {
                    var child = children[childIndex];
                    var childName = child.gameObject.name.ToLowerInvariant();
                    if (searchOptions == SearchOptions.IgnoreNamespace)
                    {
                        childName = RemoveNamespaceFromName(childName);
                        if (System.Array.Exists(inNames,
                            searchName => RemoveNamespaceFromName(searchName.ToLowerInvariant()) == childName))
                        {
                            outputList.Add(child);
                        }
                    }
                    else
                    {
                        if (System.Array.Exists(inNames,
                            searchName => searchName.ToLowerInvariant() == childName))
                        {
                            outputList.Add(child);
                        }
                    }
                }
                return outputList.ToArray();
            }

            // 正規表現で子供を検出
            public static List<T> FindChildComponentsByRegularExpression<T>
            (
                this GameObject rootObject,
                string pattern,
                RegexOptions regexOptions = RegexOptions.IgnoreCase,
                bool includeInactive = true
            ) where T : Component
            {
                var matchingComponents = new List<T>();
                var regularExpression = new Regex(pattern, regexOptions);
                var allComponents = rootObject.GetComponentsInChildren<T>(includeInactive);
                var componentCount = allComponents.Length;
                for (int componentIndex = 0; componentIndex < componentCount; componentIndex++)
                {
                    var component = allComponents[componentIndex];
                    if (regularExpression.IsMatch(component.name))
                    {
                        matchingComponents.Add(component);
                    }
                }
                return matchingComponents;
            }

            public static Transform[] FindChildrenByName
            (
                this GameObject inRoot,
                string[] inNames,
                SearchOptions searchOptions = SearchOptions.IgnoreNamespace
            )
            {
                return FindChildComponentsByName<Transform>(inRoot, inNames, searchOptions);
            }

            // この処理は重いので毎フレーム呼び出したりしないように
            public static GameObject FindGameObjectByInstanceID(int instanceIDToFind)
            {
                var objectList = Object.FindObjectsOfType<GameObject>();
                var objectCount = objectList.Length;
                for (int objectIndex = 0; objectIndex < objectCount; objectIndex++)
                {
                    var gameObject = objectList[objectIndex];
                    if (gameObject.GetInstanceID() == instanceIDToFind)
                    {
                        return gameObject;
                    }
                }
                return null;
            }

            public static T RemoveAllOldAndAddNewComponent<T>(this GameObject gameObject) where T : Component
            {
                const int AttemptCount = 100;
                for (var attempt = 0; attempt < AttemptCount; ++attempt)
                {
                    var oldComponent = gameObject.GetComponent<T>();
                    if (null != oldComponent)
                    {
                        Object.DestroyImmediate(oldComponent);
                    }
                    else
                    {
                        break;
                    }
                }
                return gameObject.AddComponent<T>();
            }

            public static string RemoveNamespaceFromName(string inName)
            {
                var splitName = inName.Split(new char[] { ':' }, System.StringSplitOptions.None);
                return (splitName.Length > 0) ? splitName[splitName.Length - 1] : "";
            }

            public static IEnumerable<T> RemoveNulls<T>(IEnumerable<T> originalCollection) where T : Object
            {
                return originalCollection.Where(item => item != null);
            }

            public static int GetTransformDepth(Transform inObject)
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

            public static List<T> SortComponentsByDepth<T>(IEnumerable<T> sourceItems) where T : Component
            {
                var itemDepthList = sourceItems
                    .Select(item => new ItemWithDepth<T>(item))
                    .ToList();
                itemDepthList.Sort((a, b) => a.depth.CompareTo(b.depth));
                var itemCount = itemDepthList.Count;
                var sortedItems = new List<T>(itemCount);
                for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    sortedItems.Add(itemDepthList[itemIndex].item);
                }
                return sortedItems;
            }

#if UNITY_EDITOR
            // This exists here so we can pick objects from their OnGUI functions
            // (See e.g. SpringCapsuleCollider.OnGUI())
            public static void SelectObjectInteractively(Object target)
            {
#if UNITY_EDITOR_OSX
                if (Event.current.command)
#else
                if (Event.current.control)
#endif
                {
                    // Toggle
                    if (UnityEditor.Selection.Contains(target))
                    {
                        RemoveFromSelection(target);
                    }
                    else
                    {
                        AddToSelection(target);
                    }
                }
                else if (Event.current.shift)
                {
                    AddToSelection(target);
                }
                else if (Event.current.alt)
                {
                    RemoveFromSelection(target);
                }
                else
                {
                    UnityEditor.Selection.objects = new Object[] { target };
                }
            }

            private static void AddToSelection(Object target)
            {
                if (!UnityEditor.Selection.Contains(target))
                {
                    var newSelection = UnityEditor.Selection.objects.ToList();
                    newSelection.Add(target);
                    UnityEditor.Selection.objects = newSelection.ToArray();
                }
            }

            private static void RemoveFromSelection(Object target)
            {
                if (UnityEditor.Selection.Contains(target))
                {
                    UnityEditor.Selection.objects = UnityEditor.Selection.objects
                        .Where(item => item != target)
                        .ToArray();
                }
            }
#endif

            private class ItemWithDepth<T> where T : Component
            {
                public T item;
                public int depth;
                public ItemWithDepth(T inItem)
                {
                    item = inItem;
                    depth = GetTransformDepth(item.transform);
                }
            }
        }
    }
}