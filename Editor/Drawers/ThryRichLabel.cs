using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
	public class ThryRichLabelDrawer : MaterialPropertyDrawer
	{
		readonly int _size;
		GUIStyle _style;

		public ThryRichLabelDrawer(float size)
		{
			this._size = (int)size;
		}

		public ThryRichLabelDrawer() : this(EditorStyles.standardFont.fontSize) { }

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			ShaderProperty.RegisterDrawer(this);
			return _size + 4;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
		{
			if (_style == null)
			{
				_style = new GUIStyle(EditorStyles.boldLabel);
				_style.richText = true;
				_style.fontSize = this._size;
			}

			int xOffset = ShaderEditor.Active?.CurrentProperty?.XOffset ?? 0;
			float leftX = GUILib.GetPropertyX(xOffset);
			Rect r = new Rect(position);
			r.x = leftX;
			r.width = position.x + position.width - leftX - GUILib.SectionContentRightPadding - 1;
			GUI.Label(r, label, _style);
		}
	}
}
