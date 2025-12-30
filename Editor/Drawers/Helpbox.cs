using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
    public class HelpboxDrawer : MaterialPropertyDrawer
    {
        readonly MessageType type;

        public HelpboxDrawer()
        {
            type = MessageType.Info;
        }

        public HelpboxDrawer(float f)
        {
            type = (MessageType)(int)f;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            int xOffset = ShaderEditor.Active?.CurrentProperty?.XOffset ?? 0;
            float leftX = GUILib.GetPropertyX(xOffset);
            float availableWidth = EditorGUIUtility.currentViewWidth - leftX - GUILib.SectionContentRightPadding - 1;
            GUIContent content = new GUIContent(label.text);
            float height = EditorStyles.helpBox.CalcHeight(content, availableWidth);
            Rect r = EditorGUILayout.GetControlRect(false, height);
            float rightEdge = r.x + r.width - GUILib.SectionContentRightPadding - 1;
            r.x = leftX;
            r.width = rightEdge - leftX;
            EditorGUI.HelpBox(r, label.text, type);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            ShaderProperty.RegisterDrawer(this);
            return 0;
        }
    }

}