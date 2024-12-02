using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Thry.MaterialCompareTool
{
    public class MaterialCompareToolWindow : EditorWindow
    {
        ObjectField leftObjectField, rightObjectField;
        IMGUIContainer leftMaterialUi, rightMaterialUi;

        MaterialRepresentation leftMaterial, rightMaterial;

        static GUIContent windowLabelContent = new GUIContent("Compare Materials");

        [MenuItem("Tools/Compare Material/Left")]
        public static void CompareLeft()
        {
            var window = GetWindow<MaterialCompareToolWindow>();
            window.titleContent = new GUIContent(windowLabelContent);
            window.leftObjectField.value = Selection.activeObject as Material;
        }

        [MenuItem("Tools/Compare Material/Right")]
        public static void CompareRight()
        {
            var window = GetWindow<MaterialCompareToolWindow>();
            window.titleContent = new GUIContent(windowLabelContent);
            window.rightObjectField.value = Selection.activeObject as Material;
        }

        [MenuItem("Tools/Compare Material/Right", true)]
        [MenuItem("Tools/Compare Material/Left", true)]
        static bool CompareValidate()
        {
            return Selection.activeObject is Material;
        }

        void CreateGUI()
        {
            VisualElement mainElement = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1
                }
            };

            var leftContainer = CreateContainer(out leftObjectField, out leftMaterialUi,
                evt => HandleMaterialChange(out leftMaterial, leftMaterialUi, evt.newValue as Material));
            var rightContainer = CreateContainer(out rightObjectField, out rightMaterialUi,
                evt => HandleMaterialChange(out rightMaterial, rightMaterialUi, evt.newValue as Material));

            mainElement.Add(leftContainer);
            mainElement.Add(rightContainer);

            ScrollView mainScrollView = new ScrollView();
            mainScrollView.Add(mainElement);

            rootVisualElement.Add(mainScrollView);
        }

        void HandleMaterialChange(out MaterialRepresentation materialRepresentation, IMGUIContainer materialUiContainer, Material newMaterial)
        {
            if(newMaterial == null)
            {
                materialRepresentation = null;
                materialUiContainer.onGUIHandler = null;
            }
            else
            {
                materialRepresentation = new MaterialRepresentation(newMaterial);
                materialUiContainer.onGUIHandler = materialRepresentation.DrawGUI;
            }
        }

        VisualElement CreateContainer(out ObjectField objectField, out IMGUIContainer materialUi,
            EventCallback<ChangeEvent<UnityEngine.Object>> changeEvent)
        {
            float borderWidth = 2f;
            StyleColor borderColor = new StyleColor(new Color(0, 0, 0, 0.3f));
            VisualElement container = new VisualElement()
            {
                style =
                {
                    flexGrow = 1,
                    maxWidth = new StyleLength(new Length(50, LengthUnit.Percent)),
                    borderTopWidth = borderWidth,
                    borderRightWidth = borderWidth / 2f,
                    borderBottomWidth = borderWidth,
                    borderLeftWidth = borderWidth,
                    borderTopColor = borderColor,
                    borderRightColor = borderColor,
                    borderBottomColor = borderColor,
                    borderLeftColor = borderColor,
                }
            };
            materialUi = new IMGUIContainer();
            objectField = new ObjectField()
            {
                objectType = typeof(Material),
            };
            objectField.RegisterCallback(changeEvent);
            container.Add(objectField);
            container.Add(materialUi);
            return container;
        }
    }
}