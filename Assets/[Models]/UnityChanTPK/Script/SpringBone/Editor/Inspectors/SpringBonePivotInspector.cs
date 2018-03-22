using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FUnit
{
    [CustomEditor(typeof(SpringBonePivot))]
    public class SpringBonePivotInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            InitializeButtons();
            var buttonCount = buttons.Length;
            for (int buttonIndex = 0; buttonIndex < buttonCount; buttonIndex++)
            {
                if (GUILayout.Button(buttons[buttonIndex].Label, heightOption))
                {
                    var pivot = (SpringBonePivot)target;
                    buttons[buttonIndex].OnPress(pivot);
                }
            }
            // base.OnInspectorGUI();
        }

        // private

        private InspectorButton<SpringBonePivot>[] buttons;
        private GUILayoutOption heightOption;

        private class InspectorButton<T>
        {
            public string Label { get; set; }
            public ActionFunction OnPress { get; set; }

            public delegate void ActionFunction(T target);
            
            public InspectorButton(string label, ActionFunction onPress)
            {
                Label = label;
                OnPress = onPress;
            }
        }

        private void InitializeButtons()
        {
            if (heightOption != null
                && buttons != null)
            {
                return;
            }

            const int UIRowHeight = 30;
            heightOption = GUILayout.Height(UIRowHeight);
            buttons = new InspectorButton<SpringBonePivot>[] {
                new InspectorButton<SpringBonePivot>("マネージャーを選択", SelectSpringManager),
                new InspectorButton<SpringBonePivot>("骨を選択", SelectBoneFromPivot)
            };
        }

        private static void SelectSpringManager(SpringBonePivot pivot)
        {
            var manager = pivot.gameObject.GetComponentInParent<SpringManager>();
            if (manager != null)
            {
                Selection.objects = new Object[] { manager.gameObject };
            }
        }

        private static void SelectBoneFromPivot(SpringBonePivot pivot)
        {
            var pivotTransform = pivot.transform;
            var root = pivotTransform.root;
            var bonesWithPivot = root.GetComponentsInChildren<SpringBone>(true)
                .Where(bone => bone.pivotNode == pivotTransform)
                .Select(bone => bone.gameObject);
            if (bonesWithPivot.Any())
            {
                Selection.objects = bonesWithPivot.ToArray();
            }
        }
    }
}