using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FUnit
{
    using SpringManagerButton = SpringManagerInspector.InspectorButton<SpringManager>;

    [CustomEditor(typeof(SpringManager))]
    public class SpringManagerInspector : Editor
    {
        public class InspectorButton<T>
        {
            public InspectorButton(string label, OnPressFunction onPress)
            {
                Label = label;
                OnPress = onPress;
                isSeparator = false;
            }

            public delegate void OnPressFunction(T target);

            public string Label { get; set; }
            public OnPressFunction OnPress { get; set; }

            public static InspectorButton<T> CreateSeparator()
            {
                var newButton = new InspectorButton<T>("", null);
                newButton.isSeparator = true;
                return newButton;
            }

            public void Show(T target, params GUILayoutOption[] options)
            {
                if (isSeparator)
                {
                    EditorGUILayout.Space();
                }
                else
                {
                    if (GUILayout.Button(Label, options))
                    {
                        OnPress(target);
                    }
                }
            }

            private bool isSeparator = false;
        }

        public override void OnInspectorGUI()
        {
            var springManagerButtons = new[] {
                new SpringManagerButton("SpringBoneリストを更新", UpdateBoneList),
                new SpringManagerButton("ダイナミクス情報をCSVから読み込む", LoadDynamicsFromCSV),
                new SpringManagerButton("ダイナミクス情報をCSVに保存", SaveDynamicsToCSV)
            };

            EditorGUILayout.Space();
            var manager = (SpringManager)target;
            foreach (var button in springManagerButtons)
            {
                button.Show(manager);
            }
            EditorGUILayout.Space();

            var registeredBoneCount = (manager.springBones != null) ? manager.springBones.Length : 0;
            EditorGUILayout.LabelField("登録ボーン数: " + registeredBoneCount.ToString());
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        public static void BrowseAndLoadDynamicsSetup(GameObject rootObject)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("再生モードを止めてください。");
                return;
            }

            var fileFilters = new string[] { "CSVファイル", "csv", "テキストファイル", "txt" };
            var path = EditorUtility.OpenFilePanelWithFilters(
                "ダイナミクス情報を読み込む", "", fileFilters);
            if (path.Length > 0)
            {
                var sourceText = FileUtil.ReadAllText(path);
                if (sourceText.Length > 0)
                {
                    // Record undo objects so that the changes will be saved
                    var undoObjects = new List<Object>{ rootObject };
                    var springManager = rootObject.GetComponent<SpringManager>();
                    if (springManager != null)
                    {
                        undoObjects.Add(springManager);
                    }
                    Undo.RecordObjects(undoObjects.ToArray(), "Load Dynamics");

                    SpringBoneSerialization.SetupFromRecordText(rootObject, rootObject, sourceText);
                    AssetDatabase.Refresh();
                    Debug.Log("読み込みました: " + path);
                }
            }
        }

        public static void BrowseAndSaveDynamicsSetup(SpringManager springManager)
        {
            var path = EditorUtility.SaveFilePanel(
                "ダイナミクス情報を保存", "", "", "csv");
            if (path.Length > 0)
            {
                var sourceText = SpringBoneSerialization.BuildDynamicsSetupString(springManager.gameObject);
                if (FileUtil.WriteAllText(path, sourceText))
                {
                    AssetDatabase.Refresh();
                    Debug.Log("保存しました: " + path);
                }
            }
        }

        // private

        private static void UpdateBoneList(SpringManager manager)
        {
            Undo.RecordObject(manager, "Update Bone List");
            SpringBoneSetup.FindAndAssignSpringBones(manager, true);
        }

        private static void LoadDynamicsFromCSV(SpringManager manager)
        {
            BrowseAndLoadDynamicsSetup(manager.gameObject);
        }

        private static void SaveDynamicsToCSV(SpringManager manager)
        {
            BrowseAndSaveDynamicsSetup(manager);
        }
    }
}