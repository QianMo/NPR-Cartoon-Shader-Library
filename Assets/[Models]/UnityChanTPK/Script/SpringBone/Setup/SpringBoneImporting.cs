using FUnit.GameObjectExtensions;
using FUnit.StringQueueExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FUnit
{
    public static partial class SpringBoneSerialization
    {
        public class ImportSettings
        {
            public ImportSettings()
            {
                ImportSpringBones = true;
                ImportCollision = true;
            }

            public ImportSettings(ImportSettings sourceSettings)
            {
                ImportSpringBones = sourceSettings.ImportSpringBones;
                ImportCollision = sourceSettings.ImportCollision;
            }

            public bool ImportSpringBones { get; set; }
            public bool ImportCollision { get; set; }
        }

        // Reads a SpringBone configuration from CSV source text.
        // If requiredBoneList is not null, bones not in the list will not be created,
        // and bones in the list not listed in the CSV will be created with default parameters.
        public static bool SetupFromRecordText
        (
            GameObject springBoneRoot,
            GameObject colliderRoot,
            string recordText,
            ImportSettings importSettings = null,
            IEnumerable<string> requiredBones = null
        )
        {
            if (springBoneRoot == null
                || colliderRoot == null
                || System.String.IsNullOrEmpty(recordText))
            {
                return false;
            }

            // Copy the source import settings in case we need to change them based on the source text
            var actualImportSettings = (importSettings != null) 
                ? new ImportSettings(importSettings) 
                : new ImportSettings();

            if (!VerifyVersionAndDetectContents(recordText, actualImportSettings))
            {
                return false;
            }

            ParsedSpringBoneSetup springBoneSetup = null;
            SpringColliderSerialization.ParsedColliderSetup colliderSetup = null;

            if (actualImportSettings.ImportCollision)
            {
                colliderSetup = SpringColliderSerialization.ParsedColliderSetup.ReadColliderSetupFromText(
                    colliderRoot, recordText);
                if (colliderSetup == null)
                {
                    Debug.LogError("ダイナミクスセットアップが失敗しました：元データにエラーがあります");
                    return false;
                }
            }

            if (actualImportSettings.ImportSpringBones)
            {
                var validColliderNames = (colliderSetup != null) ? colliderSetup.GetColliderNames() : null;
                springBoneSetup = ParsedSpringBoneSetup.ReadSpringBoneSetupFromText(
                    springBoneRoot, colliderRoot, recordText, validColliderNames);
                if (springBoneSetup == null)
                {
                    Debug.LogError("ダイナミクスセットアップが失敗しました：元データにエラーがあります");
                    return false;
                }
            }

            var hasErrors = (springBoneSetup != null && springBoneSetup.HasErrors)
                || (colliderSetup != null && colliderSetup.HasErrors);
            var okayToCreate = !hasErrors;
#if UNITY_EDITOR
            if (hasErrors)
            {
                var errorMessage = "ダイナミクスセットアップに一部エラーが出ているものがあります。\n"
                    + "正常なものだけ作成しますか？";
                okayToCreate = UnityEditor.EditorUtility.DisplayDialog(
                    "ダイナミクスセットアップ",
                    errorMessage,
                    "作成します",
                    "キャンセル");
            }
#endif

            if (!okayToCreate) { return false; }

            // Point of no return

            if (actualImportSettings.ImportCollision && colliderSetup != null)
            {
                colliderSetup.BuildObjects(colliderRoot);
            }

            if (actualImportSettings.ImportSpringBones && springBoneSetup != null)
            {
                springBoneSetup.BuildObjects(springBoneRoot, colliderRoot, requiredBones);
            }

            return true;
        }

        // private

        private const int UnknownVersion = -1;

        // todo: Do... better...
        private class PersistentSpringManagerProperties
        {
            public static PersistentSpringManagerProperties Create(SpringManager sourceManager)
            {
                if (sourceManager == null) { return null; }

                var properties = new PersistentSpringManagerProperties
                {
                    simulationFrameRate = sourceManager.simulationFrameRate,
                    dynamicRatio = sourceManager.dynamicRatio,
                    gravity = sourceManager.gravity,
                    collideWithGround = sourceManager.collideWithGround,
                    groundHeight = sourceManager.groundHeight
                };
                return properties;
            }

            public void ApplyTo(SpringManager targetManager)
            {
                targetManager.simulationFrameRate = simulationFrameRate;
                targetManager.dynamicRatio = dynamicRatio;
                targetManager.gravity = gravity;
                targetManager.collideWithGround = collideWithGround;
                targetManager.groundHeight = groundHeight;
            }

            private bool automaticUpdates;
            private int simulationFrameRate;
            private float dynamicRatio;
            private Vector3 gravity;
            private bool collideWithGround;
            private float groundHeight;
        }

        private class SpringBoneSetupMaps
        {
            public Dictionary<string, Transform> allChildren;
            public Dictionary<string, SpringSphereCollider> sphereColliders;
            public Dictionary<string, SpringCapsuleCollider> capsuleColliders;
            public Dictionary<string, SpringPanelCollider> panelColliders;

            public static SpringBoneSetupMaps Build(GameObject springBoneRoot, GameObject colliderRoot)
            {
                return new SpringBoneSetupMaps
                {
                    allChildren = GameObjectUtil.BuildNameToComponentMap<Transform>(springBoneRoot, true),
                    sphereColliders = GameObjectUtil.BuildNameToComponentMap<SpringSphereCollider>(colliderRoot, true),
                    capsuleColliders = GameObjectUtil.BuildNameToComponentMap<SpringCapsuleCollider>(colliderRoot, true),
                    panelColliders = GameObjectUtil.BuildNameToComponentMap<SpringPanelCollider>(colliderRoot, true),
                };
            }
        }

        // Version and CSV content detection
        // If version 3, import settings will be changed to reflect whether the file is bone-only or collider-only
        private static bool VerifyVersionAndDetectContents(string recordText, ImportSettings importSettings)
        {
            var version = UnknownVersion;
            try
            {
                var sourceRecords = TextRecordParsing.ParseRecordsFromText(recordText);
                version = GetVersionFromSetupRecords(sourceRecords);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("SpringBoneSetup: 元のテキストデータを読み込めませんでした！\n\n" + exception.ToString());
                return false;
            }

            const int VersionSpringBonesOnly = 3;
            const int MinSupportedVersion = VersionSpringBonesOnly;
            const int MaxSupportedVersion = 4;

            if (version == UnknownVersion)
            {
                // No version means it's probably colliders-only, but check if there are SpringBones just in case
                if (!recordText.ToLowerInvariant().Contains("[springbones]"))
                {
                    importSettings.ImportSpringBones = false;
                }
            }
            else
            {
                if (version < MinSupportedVersion
                    || version > MaxSupportedVersion)
                {
                    Debug.LogError("SpringBoneSetup: データのバージョンは対応していません！\nVersion: " + version.ToString());
                    return false;
                }

                if (version <= VersionSpringBonesOnly)
                {
                    importSettings.ImportCollision = false;
                }
            }

            return true;
        }

        // Serialized classes
#pragma warning disable 0649

        private class PivotSerializer
        {
            public string name;
            public string parentName;
            public Vector3 eulerAngles;
        }

        private class AngleLimitSerializer
        {
            public bool enabled;
            public float min;
            public float max;
        }

        private class LengthLimitSerializer
        {
            public string objectName;
            public float ratio;
        }

        private class SpringBoneBaseSerializer
        {
            public string boneName;
            public float radius;
            public float stiffness;
            public float drag;
            public Vector3 springForce;
            public float windInfluence;
            public string pivotName;
            public AngleLimitSerializer yAngleLimits;
            public AngleLimitSerializer zAngleLimits;
            public float angularStiffness;
            public LengthLimitSerializer[] lengthLimits;
        }

        private class SpringBoneSerializer
        {
            public SpringBoneBaseSerializer baseData;
            public string[] colliderNames;
        }

#pragma warning restore 0649

        private static int GetVersionFromSetupRecords(List<TextRecordParsing.Record> sourceRecords)
        {
            TextRecordParsing.Record unusedRecord;
            return GetVersionFromSetupRecords(sourceRecords, out unusedRecord);
        }

        private static int GetVersionFromSetupRecords
        (
            List<TextRecordParsing.Record> sourceRecords,
            out TextRecordParsing.Record versionRecord
        )
        {
            var version = UnknownVersion;
            const string VersionToken = "version";
            versionRecord = sourceRecords.FirstOrDefault(
                item => item.GetString(0).ToLowerInvariant() == VersionToken);
            if (versionRecord != null)
            {
                versionRecord.TryGetInt(1, ref version);
            }
            return version;
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

        // Object serialization

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

        private static IEnumerable<PivotSerializer> SerializePivotRecords
        (
            IEnumerable<TextRecordParsing.Record> sourceRecords,
            List<TextRecordParsing.Record> errorRecords
        )
        {
            var validRecords = new List<PivotSerializer>(sourceRecords.Count());
            foreach (var sourceRecord in sourceRecords)
            {
                var newRecord = SerializeObjectFromStrings<PivotSerializer>(sourceRecord.Items);
                if (newRecord != null)
                {
                    validRecords.Add(newRecord);
                }
                else
                {
                    errorRecords.Add(sourceRecord);
                }
            }
            return validRecords;
        }

        private static IEnumerable<SpringBoneSerializer> SerializeSpringBoneRecords
        (
            IEnumerable<TextRecordParsing.Record> sourceRecords,
            List<TextRecordParsing.Record> errorRecords
        )
        {
            var validRecords = new List<SpringBoneSerializer>(sourceRecords.Count());
            foreach (var sourceRecord in sourceRecords)
            {
                var itemQueue = sourceRecord.ToQueue();
                SpringBoneBaseSerializer newBaseRecord = null;
                try
                {
                    newBaseRecord = itemQueue.DequeueObject<SpringBoneBaseSerializer>();
                }
                catch (System.Exception exception)
                {
                    LogRecordError("Error building SpringBoneBaseSerializer", sourceRecord.Items, exception.ToString());
                }

                if (newBaseRecord != null)
                {
                    // The rest of the queue should be collider names
                    var colliderNames = new List<string>(itemQueue)
                        .Where(item => item.Length > 0);
                    var newRecord = new SpringBoneSerializer
                    {
                        baseData = newBaseRecord,
                        colliderNames = colliderNames.ToArray()
                    };
                    validRecords.Add(newRecord);
                }
                else
                {
                    errorRecords.Add(sourceRecord);
                }
            }
            return validRecords;
        }

        // Verification

        private static bool VerifyPivotRecords
        (
            IEnumerable<PivotSerializer> sourceRecords,
            IEnumerable<string> validParentNames,
            List<PivotSerializer> validRecords
        )
        {
            var newValidRecords = new List<PivotSerializer>(sourceRecords.Count());
            foreach (var sourceRecord in sourceRecords)
            {
                var isRecordValid = true;
                if (sourceRecord.name.Length == 0)
                {
                    // Todo: Need more details...
                    Debug.LogError("名前が指定されていない基点オブジェクトがあります");
                    isRecordValid = false;
                }

                var parentName = sourceRecord.parentName;
                if (parentName.Length == 0)
                {
                    Debug.LogError(sourceRecord.name + " : 親名が指定されていません");
                    isRecordValid = false;
                }
                else if (!validParentNames.Contains(parentName))
                {
                    Debug.LogError(sourceRecord.name + " : 親が見つかりません: " + parentName);
                    isRecordValid = false;
                }

                if (isRecordValid) { newValidRecords.Add(sourceRecord); }
            }
            validRecords.AddRange(newValidRecords);
            return sourceRecords.Count() == newValidRecords.Count();
        }

        private static bool VerifySpringBoneRecords
        (
            IEnumerable<SpringBoneSerializer> sourceRecords,
            IEnumerable<string> validBoneNames,
            IEnumerable<string> validPivotNames,
            IEnumerable<string> validColliderNames,
            List<SpringBoneSerializer> validRecords,
            out bool hasMissingColliders
        )
        {
            hasMissingColliders = false;
            var newValidRecords = new List<SpringBoneSerializer>(sourceRecords.Count());
            foreach (var sourceRecord in sourceRecords)
            {
                var isRecordValid = true;
                var baseData = sourceRecord.baseData;
                if (baseData.boneName.Length == 0)
                {
                    // Todo: Need more details...
                    Debug.LogError("名前が指定されていない基点オブジェクトがあります");
                    isRecordValid = false;
                }
                else if (!validBoneNames.Contains(baseData.boneName))
                {
                    Debug.LogError(baseData.boneName + " : オブジェくトが見つかりません");
                    isRecordValid = false;
                }

                var pivotName = baseData.pivotName;
                if (pivotName.Length == 0)
                {
                    Debug.LogError(baseData.boneName + " : 基点名が指定されていません");
                    isRecordValid = false;
                }
                else if (!validPivotNames.Contains(pivotName))
                {
                    Debug.LogError(baseData.boneName + " : 基点オブジェクトが見つかりません: " + pivotName);
                    isRecordValid = false;
                }

                var missingColliders = sourceRecord.colliderNames
                    .Where(name => !validColliderNames.Contains(name));
                if (missingColliders.Any())
                {
                    // Missing colliders are just a warning
                    hasMissingColliders = true;
                    Debug.LogWarning(
                        baseData.boneName + " : コライダーが見つかりません:\n"
                        + string.Join(" ", missingColliders.ToArray()));
                }

                if (isRecordValid) { newValidRecords.Add(sourceRecord); }
            }
            validRecords.AddRange(newValidRecords);
            return sourceRecords.Count() == newValidRecords.Count();
        }

        // Object construction

        private static AngleLimits BuildAngleLimitsFromSerializer(AngleLimitSerializer serializer)
        {
            return new AngleLimits
            {
                active = serializer.enabled,
                min = serializer.min,
                max = serializer.max
            };
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            for (var childIndex = 0; childIndex < parent.childCount; ++childIndex)
            {
                var child = parent.GetChild(childIndex);
                if (child.name.ToLowerInvariant() == name.ToLowerInvariant())
                {
                    return child;
                }
            }
            return null;
        }

        private static bool SetupPivotFromSerializer
        (
            Dictionary<string, Transform> transforms,
            PivotSerializer serializer
        )
        {
            Transform parent;
            var parentExists = transforms.TryGetValue(serializer.parentName, out parent);
            if (parentExists)
            {
                var pivot = FindChildByName(parent, serializer.name);
                if (pivot == null)
                {
                    var pivotGameObject = new GameObject(serializer.name, typeof(SpringBonePivot));
                    pivot = pivotGameObject.transform;
                    pivot.parent = parent;
                }
                pivot.localScale = Vector3.one;
                pivot.localEulerAngles = serializer.eulerAngles;
                pivot.localPosition = Vector3.zero;
            }
            return parentExists;
        }

        private static bool SetupSpringBoneFromSerializer
        (
            SpringBoneSetupMaps setupMaps,
            SpringBoneSerializer serializer
        )
        {
            var baseData = serializer.baseData;
            Transform childBone = null;
            if (!setupMaps.allChildren.TryGetValue(baseData.boneName, out childBone))
            {
                Debug.LogError("ボーンが見つかりません: " + baseData.boneName);
                return false;
            }

            var springBone = childBone.gameObject.AddComponent<SpringBone>();
            springBone.stiffnessForce = baseData.stiffness;
            springBone.dragForce = baseData.drag;
            springBone.springForce = baseData.springForce;
            springBone.windInfluence = baseData.windInfluence;
            springBone.angularStiffness = baseData.angularStiffness;
            springBone.yAngleLimits = BuildAngleLimitsFromSerializer(baseData.yAngleLimits);
            springBone.zAngleLimits = BuildAngleLimitsFromSerializer(baseData.zAngleLimits);

            // Pivot node
            var pivotNodeName = baseData.pivotName;
            Transform pivotNode = null;
            if (pivotNodeName.Length > 0)
            {
                if (!setupMaps.allChildren.TryGetValue(pivotNodeName, out pivotNode))
                {
                    Debug.LogError("Pivotオブジェクトが見つかりません: " + pivotNodeName);
                    pivotNode = null;
                }
            }
            if (pivotNode == null)
            {
                pivotNode = springBone.transform.parent ?? springBone.transform;
            }
            else
            {
                var skinBones = GameObjectUtil.GetAllBones(springBone.transform.root.gameObject);
                if (pivotNode.GetComponent<SpringBonePivot>()
                    && SpringBoneSetup.IsPivotProbablySafeToDestroy(pivotNode, skinBones))
                {
                    pivotNode.position = springBone.transform.position;
                }
            }
            springBone.pivotNode = pivotNode;

            springBone.lengthLimitTargets = baseData.lengthLimits
                .Where(lengthLimit => setupMaps.allChildren.ContainsKey(lengthLimit.objectName))
                .Select(lengthLimit => setupMaps.allChildren[lengthLimit.objectName])
                .ToArray();

            springBone.sphereColliders = serializer.colliderNames
                .Where(name => setupMaps.sphereColliders.ContainsKey(name))
                .Select(name => setupMaps.sphereColliders[name])
                .ToArray();

            springBone.capsuleColliders = serializer.colliderNames
                .Where(name => setupMaps.capsuleColliders.ContainsKey(name))
                .Select(name => setupMaps.capsuleColliders[name])
                .ToArray();

            springBone.panelColliders = serializer.colliderNames
                .Where(name => setupMaps.panelColliders.ContainsKey(name))
                .Select(name => setupMaps.panelColliders[name])
                .ToArray();

            return true;
        }

        // Put it all together

        private class ParsedSpringBoneSetup
        {
            public bool HasErrors { get; private set; }

            public static ParsedSpringBoneSetup ReadSpringBoneSetupFromText
            (
                GameObject springBoneRoot,
                GameObject colliderRoot,
                string recordText,
                IEnumerable<string> inputValidColliderNames
            )
            {
                List<TextRecordParsing.Record> rawSpringBoneRecords = null;
                List<TextRecordParsing.Record> rawPivotRecords = null;
                try
                {
                    var sourceRecords = TextRecordParsing.ParseRecordsFromText(recordText);
                    TextRecordParsing.Record versionRecord = null;
                    GetVersionFromSetupRecords(sourceRecords, out versionRecord);
                    rawSpringBoneRecords = TextRecordParsing.GetSectionRecords(sourceRecords, "SpringBones");
                    if (rawSpringBoneRecords == null || rawSpringBoneRecords.Count == 0)
                    {
                        rawSpringBoneRecords = TextRecordParsing.GetSectionRecords(sourceRecords, null)
                            .Where(item => item != versionRecord)
                            .ToList();
                    }
                    rawPivotRecords = TextRecordParsing.GetSectionRecords(sourceRecords, "Pivots");
                }
                catch (System.Exception exception)
                {
                    Debug.LogError("SpringBoneSetup: 元のテキストデータを読み込めませんでした！\n\n" + exception.ToString());
                    return null;
                }

                var hasErrors = false;

                var errors = new List<TextRecordParsing.Record>();
                var pivotRecords = SerializePivotRecords(rawPivotRecords, errors);
                var springBoneRecords = SerializeSpringBoneRecords(rawSpringBoneRecords, errors);
                if (errors.Count > 0) { hasErrors = true; }

                var validObjectNames = springBoneRoot.GetComponentsInChildren<Transform>(true)
                    .Select(item => item.name)
                    .Distinct()
                    .ToList();
                var validPivotRecords = new List<PivotSerializer>();
                if (!VerifyPivotRecords(pivotRecords, validObjectNames, validPivotRecords)) { hasErrors = true; }

                var validPivotNames = new List<string>(validObjectNames);
                validPivotNames.AddRange(validPivotRecords.Select(record => record.name));

                var validColliderNames = new List<string>();
                var colliderTypes = SpringColliderSetup.GetColliderTypes();
                validColliderNames.AddRange(colliderTypes
                    .SelectMany(type => colliderRoot.GetComponentsInChildren(type, true))
                    .Select(item => item.name));
                if (inputValidColliderNames != null) { validColliderNames.AddRange(inputValidColliderNames); }

                var validSpringBoneRecords = new List<SpringBoneSerializer>();
                bool hasMissingColliders;
                if (!VerifySpringBoneRecords(
                    springBoneRecords,
                    validObjectNames,
                    validPivotNames,
                    validColliderNames,
                    validSpringBoneRecords,
                    out hasMissingColliders))
                {
                    hasErrors = true;
                }

                if (hasMissingColliders)
                {
                    Debug.LogWarning("スプリングボーンセットアップ：一部のコライダーが見つかりません");
                }

                return new ParsedSpringBoneSetup
                {
                    HasErrors = hasErrors,
                    pivotRecords = validPivotRecords,
                    springBoneRecords = validSpringBoneRecords
                };
            }

            public void BuildObjects(GameObject springBoneRoot, GameObject colliderRoot, IEnumerable<string> requiredBones)
            {
                DestroyOldSpringBones(springBoneRoot);
                if (requiredBones != null)
                {
                    FilterBoneRecordsByRequiredBonesAndCreateUnrecordedBones(springBoneRoot, requiredBones);
                }

                var initialTransforms = GameObjectUtil.BuildNameToComponentMap<Transform>(springBoneRoot, true);
                foreach (var record in pivotRecords)
                {
                    SetupPivotFromSerializer(initialTransforms, record);
                }

                var setupMaps = SpringBoneSetupMaps.Build(springBoneRoot, colliderRoot);
                foreach (var record in springBoneRecords)
                {
                    SetupSpringBoneFromSerializer(setupMaps, record);
                }

                var springManager = GameObjectUtil.AcquireComponent<SpringManager>(springBoneRoot, true);
                SpringBoneSetup.FindAndAssignSpringBones(springManager);
            }

            // private

            private IEnumerable<PivotSerializer> pivotRecords;
            private IEnumerable<SpringBoneSerializer> springBoneRecords;

            private static void DestroyOldSpringBones(GameObject springBoneRoot)
            {
                var springManager = springBoneRoot.GetComponent<SpringManager>();
                if (springManager != null)
                {
                    springManager.springBones = new SpringBone[0];
                }

                SpringBoneSetup.DestroyPivotObjects(springBoneRoot);
                var springBones = springBoneRoot.GetComponentsInChildren<SpringBone>(true);
                foreach (var springBone in springBones)
                {
                    UnityEngine.Object.DestroyImmediate(springBone);
                }
            }

            private void FilterBoneRecordsByRequiredBonesAndCreateUnrecordedBones
            (
                GameObject springBoneRoot,
                IEnumerable<string> requiredBones
            )
            {
                var boneRecordsToUse = springBoneRecords
                    .Where(record => requiredBones.Contains(record.baseData.boneName));
                var recordedBoneNames = boneRecordsToUse.Select(record => record.baseData.boneName);

                var bonesWithoutRecords = requiredBones
                    .Except(recordedBoneNames)
                    .Select(boneName => GameObjectUtil.FindChildByName(springBoneRoot, boneName))
                    .Where(item => item != null)
                    .Select(item => item.gameObject);
                foreach (var bone in bonesWithoutRecords)
                {
                    var springBone = bone.AddComponent<SpringBone>();
                    SpringBoneSetup.CreateSpringPivotNode(springBone);
                }

                // Report the skipped bone records so the user knows
                var skippedBoneRecords = springBoneRecords.Except(boneRecordsToUse);
                foreach (var skippedRecord in skippedBoneRecords)
                {
                    Debug.LogWarning(skippedRecord.baseData.boneName
                        + "\nボーンリストにないので作成しません");
                }

                springBoneRecords = boneRecordsToUse;
            }
        }
    }
}