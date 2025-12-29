using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
	public class LocalMessageDrawer : MaterialPropertyDrawer
	{
		protected ButtonData _buttonData;
		protected bool _isInit;

		protected virtual void Init(string s)
		{
			if (_isInit) return;
			_buttonData = Parser.Deserialize<ButtonData>(s);
			_isInit = true;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			Init(prop.displayName);
			if (_buttonData == null) return;

			int xOffset = ShaderEditor.Active?.CurrentProperty?.XOffset ?? 0;
			float leftX = GUILib.GetPropertyX(xOffset);
			Rect baseRect = EditorGUILayout.GetControlRect(false, 0);
			float rightEdge = baseRect.x + baseRect.width - GUILib.SectionContentRightPadding - 1;
			float availableWidth = rightEdge - leftX;

			if (_buttonData.text.Length > 0)
			{
				GUIStyle style = _buttonData.center_position ? Styles.middleCenter_richText_wordWrap : Styles.upperLeft_richText_wordWrap;
				GUIContent content = new GUIContent(_buttonData.text, _buttonData.hover);
				float height = style.CalcHeight(content, availableWidth);
				Rect r = EditorGUILayout.GetControlRect(false, height);
				r.x = leftX;
				r.width = rightEdge - leftX;
				GUI.Label(r, content, style);
				if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
					_buttonData.action.Perform(ShaderEditor.Active?.Materials);
			}

			if (_buttonData.texture != null)
			{
				float height = Mathf.Min(_buttonData.texture.height, availableWidth);
				Rect r = EditorGUILayout.GetControlRect(false, height);
				r.x = leftX;
				r.width = rightEdge - leftX;
				GUIContent content = new GUIContent(_buttonData.texture.loaded_texture, _buttonData.hover);
				if (_buttonData.center_position)
				{
					float texWidth = Mathf.Min(_buttonData.texture.loaded_texture.width, r.width);
					Rect centered = new Rect(r.x + (r.width - texWidth) / 2, r.y, texWidth, r.height);
					GUI.Label(centered, content);
				}
				else
				{
					GUI.Label(r, content);
				}
				if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
					_buttonData.action.Perform(ShaderEditor.Active?.Materials);
			}
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			ShaderProperty.RegisterDrawer(this);
			return 0;
		}
	}
}
