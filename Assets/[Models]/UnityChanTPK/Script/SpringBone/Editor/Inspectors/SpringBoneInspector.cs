using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FUnit
{
    using Inspector;
    using SpringBoneButton = SpringManagerInspector.InspectorButton<SpringBone>;

    // https://docs.unity3d.com/ScriptReference/Editor.html

    [CustomEditor(typeof(SpringBone))]
    [CanEditMultipleObjects]
    public class SpringBoneInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var bone = (SpringBone)target;

            ShowActionButtons(bone);

            GUILayout.Space(16f);
            var newEnabled = EditorGUILayout.Toggle("有効", bone.enabled);
            if (newEnabled != bone.enabled)
            {
                var targetBones = from targetObject in serializedObject.targetObjects
                                  where targetObject is SpringBone
                                  select (SpringBone)targetObject;

                if (targetBones.Any())
                {
                    Undo.RecordObjects(targetBones.ToArray(), "SpringBoneの有効状態を変更");
                    foreach (var targetBone in targetBones)
                    {
                        targetBone.enabled = newEnabled;
                    }
                }
            }

            var setCount = propertySets.Length;
            for (int setIndex = 0; setIndex < setCount; setIndex++)
            {
                propertySets[setIndex].Show();
            }
            GUILayout.Space(16f);

            serializedObject.ApplyModifiedProperties();

            showOriginalInspector = EditorGUILayout.Toggle("標準インスペクター表示", showOriginalInspector);
            if (showOriginalInspector)
            {
                base.OnInspectorGUI();
            }
        }

        // private

        private const int ButtonHeight = 30;

        private SpringBoneButton[] actionButtons;
        private PropertySet[] propertySets;
        private bool showOriginalInspector = false;

        private class PropertySet
        {
            public PropertySet(string newTitle, PropertyInfo[] newProperties)
            {
                title = newTitle;
                properties = newProperties;
            }

            public void Initialize(SerializedObject serializedObject)
            {
                var propertyCount = properties.Length;
                for (var propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
                {
                    properties[propertyIndex].Initialize(serializedObject);
                }
            }

            public void Show()
            {
                const float Spacing = 16f;

                GUILayout.Space(Spacing);
                GUILayout.Label(title, GUILayout.Height(ButtonHeight));
                var propertyCount = properties.Length;
                for (var propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
                {
                    properties[propertyIndex].Show();
                }
            }

            private string title;
            private PropertyInfo[] properties;
        }

        private void InitializeActionButtons()
        {
            if (actionButtons == null)
            {
                actionButtons = new SpringBoneButton[] {
                    new SpringBoneButton("マネージャーを選択", SelectSpringManager),
                    new SpringBoneButton("基点オブジェクトを選択", SelectPivotNode)
                };
            }
        }

        private void ShowActionButtons(SpringBone bone)
        {
            InitializeActionButtons();
            var buttonCount = actionButtons.Length;
            var buttonHeight = GUILayout.Height(ButtonHeight);
            for (int buttonIndex = 0; buttonIndex < buttonCount; buttonIndex++)
            {
                actionButtons[buttonIndex].Show(bone, buttonHeight);
            }
        }

        private void OnEnable()
        {
            InitializeActionButtons();

            var forceProperties = new PropertyInfo[] {
                new PropertyInfo("stiffnessForce", "硬さ"),
                new PropertyInfo("dragForce", "空気抵抗"),
                new PropertyInfo("springForce", "重力"),
                new PropertyInfo("windInfluence", "風の影響値")
            };

            var angleLimitProperties = new PropertyInfo[] {
                new PropertyInfo("pivotNode", "基点"),
                new PropertyInfo("angularStiffness", "回転の硬さ"),
                new AngleLimitPropertyInfo("yAngleLimits", "Y 軸角度制限"),
                new AngleLimitPropertyInfo("zAngleLimits", "Z 軸角度制限")
            };

            var lengthLimitProperties = new PropertyInfo[] {
                new PropertyInfo("lengthLimitTargets", "ターゲット")
            };

            var collisionProperties = new PropertyInfo[] {
                new PropertyInfo("radius", "半径"),
                new PropertyInfo("sphereColliders", "球体"),
                new PropertyInfo("capsuleColliders", "カプセル"),
                new PropertyInfo("panelColliders", "板")
            };

            propertySets = new PropertySet[] {
                new PropertySet("力", forceProperties), 
                new PropertySet("角度制限", angleLimitProperties),
                new PropertySet("距離制限", lengthLimitProperties),
                new PropertySet("当たり判定", collisionProperties),
            };

            foreach (var set in propertySets)
            {
                set.Initialize(serializedObject);
            }
        }

        private static void SelectSpringManager(SpringBone bone)
        {
            var manager = bone.gameObject.GetComponentInParent<SpringManager>();
            if (manager != null)
            {
                Selection.objects = new Object[] { manager.gameObject };
            }
        }

        private static void SelectPivotNode(SpringBone bone)
        {
            var pivotObjects = new List<GameObject>();
            foreach (var gameObject in Selection.gameObjects)
            {
                var springBone = gameObject.GetComponent<SpringBone>();
                if (springBone != null
                    && springBone.pivotNode != null)
                {
                    pivotObjects.Add(springBone.pivotNode.gameObject);
                }
            }
            Selection.objects = pivotObjects.ToArray();
        }
    }
}