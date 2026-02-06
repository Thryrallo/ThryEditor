using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
	// [Helpbox(messageType)] - Standard helpbox
	// [Helpbox(messageType, minLines)] - Minimum height in lines
	public class HelpboxDrawer : MaterialPropertyDrawer
	{
		readonly MessageType type;
		readonly int minLines;
		static Texture2D _customIcon;

		public HelpboxDrawer()
		{
			type = MessageType.Info;
			minLines = 0;
		}

		public HelpboxDrawer(float f)
		{
			type = (MessageType)(int)f;
			minLines = 0;
		}

		public HelpboxDrawer(float messageType, float minLines)
		{
			type = (MessageType)(int)messageType;
			this.minLines = (int)minLines;
		}

	public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
	{
		if (_customIcon == null)
		{
			_customIcon = Resources.Load<Texture2D>("help");
		}

		PropertyOptions options = ShaderEditor.Active?.CurrentProperty?.Options;

		int xOffset = ShaderEditor.Active?.CurrentProperty?.XOffset ?? 0;
		float leftX = GUILib.GetPropertyX(xOffset);
		float availableWidth = EditorGUIUtility.currentViewWidth - leftX - GUILib.SectionContentRightPadding - 1;
		GUIContent content = new GUIContent(label.text);
		
		float calcHeight = EditorStyles.helpBox.CalcHeight(content, availableWidth);
		float height = calcHeight;
		
		if (minLines > 0)
		{
			float lineHeight = EditorStyles.helpBox.CalcHeight(new GUIContent("A"), availableWidth);
			float minHeight = lineHeight * minLines;
			height = Mathf.Max(calcHeight, minHeight);
		}
		
		Rect r = EditorGUILayout.GetControlRect(false, height);
		float rightEdge = r.x + r.width - GUILib.SectionContentRightPadding - 1;
		r.x = leftX;
		r.width = rightEdge - leftX;
		
		GUI.Box(r, GUIContent.none, GUI.skin.button);
		
		if (_customIcon != null)
		{
			Rect iconRect = new Rect(r.x + 5, r.y + (r.height - 32) * 0.5f, 32, 32);
			GUI.DrawTexture(iconRect, _customIcon);
		}
		
		GUIStyle textStyle = new GUIStyle(GUI.skin.label);
		textStyle.wordWrap = true;
		textStyle.alignment = TextAnchor.MiddleLeft;
		Rect textRect = new Rect(r.x + 42, r.y + 4, r.width - 56, r.height - 8);
		GUI.Label(textRect, label.text, textStyle);
		
		if (options?.onClick != null && Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
		{
			options.onClick.Perform(ShaderEditor.Active.Materials);
			Event.current.Use();
		}
		
		GUILayout.Space(4);
	}

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            ShaderProperty.RegisterDrawer(this);
            return 0;
        }
    }

}