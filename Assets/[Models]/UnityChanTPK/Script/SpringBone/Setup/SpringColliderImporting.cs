using FUnit.GameObjectExtensions;
using FUnit.StringQueueExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FUnit
{
    public static partial class SpringColliderSerialization
    {
        public class ParsedColliderSetup
        {
            public bool HasErrors { get; private set; }

            public static ParsedColliderSetup ReadColliderSetupFromText(GameObject colliderRoot, string recordText)
            {
                List<TextRecordParsing.Record> rawColliderRecords = null;
                List<TextRecordParsing.Record> rawDynamicsNullRecords = null;
                try
                {
                    var sourceRecords = TextRecordParsing.ParseRecordsFromText(recordText);
                    rawColliderRecords = TextRecordParsing.GetSectionRecords(sourceRecords, "Colliders");
                    if (rawColliderRecords == null || rawColliderRecords.Count == 0)
                    {
                        rawColliderRecords = TextRecordParsing.GetSectionRecords(sourceRecords, null);
                    }
                    rawDynamicsNullRecords = TextRecordParsing.GetSectionRecords(sourceRecords, "DynamicsNulls");
                }
                catch (System.Exception exception)
                {
                    Debug.LogError("SpringColliderSetup: 元のテキストデータを読み込めませんでした！\n\n" + exception.ToString());
                    return null;
                }

                var hasErrors = false;

                var errors = new List<TextRecordParsing.Record>();
                var colliderRecords = SerializeColliderRecords(rawColliderRecords, errors);
                var dynamicsNullRecords = SerializeTransformRecords(rawDynamicsNullRecords, errors);
                if (errors.Count > 0) { hasErrors = true; }

                var validParentNames = colliderRoot.GetComponentsInChildren<Transform>(true).Select(item => item.name).ToList();
                var validDynamicsNullRecords = new List<TransformSerializer>();
                if (!VerifyTransformRecords(dynamicsNullRecords, validParentNames, validDynamicsNullRecords)) { hasErrors = true; }

                // Colliders get added after DynamicsNulls, so they can be added as children to them
                validParentNames.AddRange(validDynamicsNullRecords.Select(item => item.name));
                var validColliderRecords = new List<IColliderSerializer>();
                if (!VerifyColliderRecords(colliderRecords, colliderRoot, validParentNames, validColliderRecords)) { hasErrors = true; }

                // Todo: Verify Component records

                return new ParsedColliderSetup
                {
                    HasErrors = hasErrors,
                    colliderRecords = validColliderRecords,
                    dynamicsNullRecords = validDynamicsNullRecords
                };
            }

            public void BuildObjects(GameObject colliderRoot)
            {
                SpringColliderSetup.DestroySpringColliders(colliderRoot);
                var allChildren = colliderRoot.BuildNameToComponentMap<Transform>(true);
                SetupDynamicsNulls(colliderRoot, allChildren, dynamicsNullRecords);

                // New objects may have been created by SetupDynamicNulls, so retrieve all the children again
                allChildren = colliderRoot.BuildNameToComponentMap<Transform>(true);
                foreach (var record in colliderRecords)
                {
                    SetupColliderFromRecord(colliderRoot, allChildren, record);
                }
            }

            public IEnumerable<string> GetColliderNames()
            {
                return colliderRecords.Select(item => item.GetBaseInfo().transform.name);
            }

            // private

            private IEnumerable<IColliderSerializer> colliderRecords;
            private IEnumerable<TransformSerializer> dynamicsNullRecords;
        }

        // private

        private const string SphereColliderToken = "sp";
        private const string CapsuleColliderToken = "cp";
        private const string PanelColliderToken = "pa";

        private static Transform CreateNewGameObject(Transform parent, string name)
        {
            var newObject = new GameObject(name);
            if (newObject == null)
            {
                Debug.LogError("新しいオブジェクトを作成できませんでした: " + name);
                return null;
            }
            newObject.transform.parent = parent;
            return newObject.transform;
        }

        private static Transform GetChildByName(Transform parent, string name)
        {
            return Enumerable.Range(0, parent.childCount)
                .Select(index => parent.GetChild(index))
                .Where(child => child.name == name)
                .FirstOrDefault();
        }

        private static T TryToFindComponent<T>(GameObject gameObject, string name) where T : Component
        {
            T component = default(T);
            if (name.Length > 0)
            {
                component = GameObjectUtil.FindChildComponentByName<T>(
                    gameObject.transform.root.gameObject, name);
            }
            return component;
        }

        // Serialized classes
#pragma warning disable 0649

        private class TransformSerializer
        {
            public string name;
            public string parentName;
            public Vector3 position;
            public Vector3 eulerAngles;
            public Vector3 scale;
        }

        private class ColliderSerializerBaseInfo
        {
            public TransformSerializer transform;
            public string colliderType;
        }

        private interface IColliderSerializer
        {
            ColliderSerializerBaseInfo GetBaseInfo();
            Component BuildColliderComponent(GameObject gameObject);
            string GetLinkedRendererName();
        }

        private class SphereColliderSerializer : IColliderSerializer
        {
            public ColliderSerializerBaseInfo baseInfo;
            public float radius;
            public string linkedRenderer;

            public ColliderSerializerBaseInfo GetBaseInfo() { return baseInfo; }

            public Component BuildColliderComponent(GameObject gameObject)
            {
                var collider = gameObject.AddComponent<SpringSphereCollider>();
                collider.radius = radius;
                if (!string.IsNullOrEmpty(linkedRenderer))
                {
                    collider.linkedRenderer = TryToFindComponent<Renderer>(gameObject, linkedRenderer);
                }
                return collider;
            }

            public string GetLinkedRendererName()
            {
                return linkedRenderer;
            }
        }

        private class CapsuleColliderSerializer : IColliderSerializer
        {
            public ColliderSerializerBaseInfo baseInfo;
            public float radius;
            public float height;
            public string linkedRenderer;

            public ColliderSerializerBaseInfo GetBaseInfo() { return baseInfo; }

            public Component BuildColliderComponent(GameObject gameObject)
            {
                var collider = gameObject.AddComponent<SpringCapsuleCollider>();
                collider.radius = radius;
                collider.height = height;
                if (!string.IsNullOrEmpty(linkedRenderer))
                {
                    collider.linkedRenderer = TryToFindComponent<Renderer>(gameObject, linkedRenderer);
                }
                return collider;
            }

            public string GetLinkedRendererName()
            {
                return linkedRenderer;
            }
        }

        private class PanelColliderSerializer : IColliderSerializer
        {
            public ColliderSerializerBaseInfo baseInfo;
            public float width;
            public float height;
            public string linkedRenderer;

            public ColliderSerializerBaseInfo GetBaseInfo() { return baseInfo; }

            public Component BuildColliderComponent(GameObject gameObject)
            {
                var collider = gameObject.AddComponent<SpringPanelCollider>();
                collider.width = width;
                collider.height = height;
                if (!string.IsNullOrEmpty(linkedRenderer))
                {
                    collider.linkedRenderer = TryToFindComponent<Renderer>(gameObject, linkedRenderer);
                }
                return collider;
            }

            public string GetLinkedRendererName()
            {
                return linkedRenderer;
            }
        }

#pragma warning restore 0649

        private static GameObject SetupTransformFromRecord
        (
            GameObject rootObject,
            Dictionary<string, Transform> objectMap,
            TransformSerializer serializer
        )
        {
            Transform parent = null;
            if (!objectMap.TryGetValue(serializer.parentName, out parent))
            {
                Debug.LogError("親が見つかりませんでした: " + serializer.parentName);
                return null;
            }

            var objectTransform = GetChildByName(parent, serializer.name);
            if (objectTransform == null)
            {
                objectTransform = CreateNewGameObject(parent, serializer.name);
                if (objectTransform == null)
                {
                    return null;
                }
            }

            // Don't change the transform if it is a bone
            var skinBoneNames = GameObjectUtil.GetAllBones(rootObject)
                .Select(bone => bone.name);
            if (!skinBoneNames.Contains(serializer.name))
            {
                objectTransform.localScale = serializer.scale;
                objectTransform.localEulerAngles = serializer.eulerAngles;
                objectTransform.localPosition = serializer.position;
            }
            return objectTransform.gameObject;
        }

        private static bool SetupColliderFromRecord
        (
            GameObject rootObject,
            Dictionary<string, Transform> objectMap,
            IColliderSerializer colliderSerializer
        )
        {
            var gameObject = SetupTransformFromRecord(
                rootObject, objectMap, colliderSerializer.GetBaseInfo().transform);
            var succeeded = gameObject != null;
            if (succeeded)
            {
                colliderSerializer.BuildColliderComponent(gameObject);
            }
            return succeeded;
        }

        private static void LogRecordError(string prefix, IEnumerable<string> items, string suffix = "")
        {
            var errorMessage = prefix + "\n";
            if (items != null)
            {
                errorMessage += string.Join(", ", items.ToArray()) + "\n";
            }
            errorMessage += suffix;
            Debug.LogError(errorMessage);
        }

        private static System.Object SerializeObjectFromStrings
        (
            System.Type type,
            IEnumerable<string> sourceItems,
            string firstOptionalField = null
        )
        {
            try
            {
                var sourceQueue = new Queue<string>(sourceItems);
                return sourceQueue.DequeueObject(type, firstOptionalField);
            }
            catch (System.Exception exception)
            {
                LogRecordError("Error building " + type.ToString(), sourceItems, exception.ToString());
            }
            return null;
        }

        private static T SerializeObjectFromStrings<T>
        (
            IEnumerable<string> sourceItems,
            string firstOptionalField = null
        ) where T : class
        {
            return SerializeObjectFromStrings(typeof(T), sourceItems, firstOptionalField) as T;
        }

        private static IEnumerable<IColliderSerializer> SerializeColliderRecords
        (
            IEnumerable<TextRecordParsing.Record> sourceRecords,
            List<TextRecordParsing.Record> errorRecords
        )
        {
            var serializerClasses = new Dictionary<string, System.Type>
            {
                { SphereColliderToken, typeof(SphereColliderSerializer) },
                { CapsuleColliderToken, typeof(CapsuleColliderSerializer) },
                { PanelColliderToken, typeof(PanelColliderSerializer) },
            };

            var colliderSerializers = new List<IColliderSerializer>(sourceRecords.Count());
            foreach (var sourceRecord in sourceRecords)
            {
                IColliderSerializer newColliderInfo = null;
                var baseInfo = SerializeObjectFromStrings<ColliderSerializerBaseInfo>(sourceRecord.Items);
                if (baseInfo != null)
                {
                    System.Type serializerType;
                    if (serializerClasses.TryGetValue(baseInfo.colliderType, out serializerType))
                    {
                        newColliderInfo = SerializeObjectFromStrings(
                            serializerType, sourceRecord.Items, "linkedRenderer")
                            as IColliderSerializer;
                    }
                    else
                    {
                        LogRecordError("Invalid collider type: " + baseInfo.colliderType, sourceRecord.Items);
                    }
                }

                if (newColliderInfo != null)
                {
                    colliderSerializers.Add(newColliderInfo);
                }
                else
                {
                    errorRecords.Add(sourceRecord);
                }
            }
            return colliderSerializers;
        }

        private static IEnumerable<TransformSerializer> SerializeTransformRecords
        (
            IEnumerable<TextRecordParsing.Record> sourceRecords,
            List<TextRecordParsing.Record> errorRecords
        )
        {
            var transformRecords = new List<TransformSerializer>(sourceRecords.Count());
            foreach (var sourceRecord in sourceRecords)
            {
                var transformRecord = SerializeObjectFromStrings<TransformSerializer>(sourceRecord.Items);
                if (transformRecord != null)
                {
                    transformRecords.Add(transformRecord);
                }
                else
                {
                    errorRecords.Add(sourceRecord);
                }
            }
            return transformRecords;
        }

        private static bool VerifyTransformRecord
        (
            TransformSerializer transformSerializer,
            IEnumerable<string> validParentNames
        )
        {
            var isRecordValid = true;
            var objectName = transformSerializer.name;
            if (objectName.Length == 0)
            {
                // Todo: Need more details...
                Debug.LogError("コライダー名が指定されていないものがあります");
                isRecordValid = false;
            }

            var parentName = transformSerializer.parentName;
            if (parentName.Length == 0)
            {
                Debug.LogError(objectName + " : 親名が指定されていません");
                isRecordValid = false;
            }
            else if (!validParentNames.Contains(parentName))
            {
                Debug.LogError(objectName + " : 親が見つかりません: " + parentName);
                isRecordValid = false;
            }

            return isRecordValid;
        }

        private static bool VerifyTransformRecords
        (
            IEnumerable<TransformSerializer> sourceRecords,
            IEnumerable<string> validParentNames,
            List<TransformSerializer> validRecords
        )
        {
            var newValidRecords = new List<TransformSerializer>(sourceRecords.Count());
            foreach (var sourceRecord in sourceRecords
                .Where(sourceRecord => VerifyTransformRecord(sourceRecord, validParentNames)))
            {
                var isValidRecord = true;
                if (newValidRecords.Any(item => item.name == sourceRecord.name))
                {
                    Debug.LogError(sourceRecord.name + " : 名前が重複します");
                    isValidRecord = false;
                }
                if (isValidRecord) { newValidRecords.Add(sourceRecord); }
            }
            validRecords.AddRange(newValidRecords);
            return sourceRecords.Count() == newValidRecords.Count();
        }

        private static bool VerifyColliderRecords
        (
            IEnumerable<IColliderSerializer> colliderRecords,
            GameObject rootObject,
            IEnumerable<string> validParentNames,
            List<IColliderSerializer> validRecords
        )
        {
            var newValidRecords = new List<IColliderSerializer>(colliderRecords.Count());
            foreach (var sourceRecord in colliderRecords)
            {
                var objectName = sourceRecord.GetBaseInfo().transform.name;
                var isValidRecord = true;
                if (!VerifyTransformRecord(sourceRecord.GetBaseInfo().transform, validParentNames))
                {
                    isValidRecord = false;
                }

                var linkedRendererName = sourceRecord.GetLinkedRendererName();
                if (!string.IsNullOrEmpty(linkedRendererName)
                    && rootObject.FindChildComponentByName<Renderer>(linkedRendererName) == null)
                {
                    Debug.LogError(objectName + " : linkedRendererが見つかりません: " + linkedRendererName);
                    isValidRecord = false;
                }

                if (newValidRecords.Any(item => item.GetBaseInfo().transform.name == objectName))
                {
                    Debug.LogError(objectName + " : 名前が重複します");
                    isValidRecord = false;
                }

                if (isValidRecord) { newValidRecords.Add(sourceRecord); }
            }
            validRecords.AddRange(newValidRecords);
            return colliderRecords.Count() == newValidRecords.Count;
        }

        // DynamicsNulls

        private static void SetupDynamicsNulls
        (
            GameObject rootObject,
            Dictionary<string, Transform> objectMap,
            IEnumerable<TransformSerializer> records
        )
        {
            // Remove excess DynamicsNulls
            foreach (var transform in objectMap.Values)
            {
                var dynamicsNulls = transform.gameObject.GetComponents<DynamicsNull>();
                for (int index = 1; index < dynamicsNulls.Length; ++index)
                {
                    Object.DestroyImmediate(dynamicsNulls[index]);
                }
            }

            foreach (var entry in records)
            {
                var newObject = SetupTransformFromRecord(rootObject, objectMap, entry);
                if (newObject != null && newObject.GetComponent<DynamicsNull>() == null)
                {
                    newObject.AddComponent<DynamicsNull>();
                }
            }
        }
    }
}