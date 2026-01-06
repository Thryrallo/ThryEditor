using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
	/// <summary>
	/// Hides the property in ThryEditor's inspector without using Unity's [HideInInspector].
	/// This allows external tools like VRCFury to still discover and animate the property.
	/// </summary>
	public class ThryHideInInspectorDrawer : MaterialPropertyDrawer
	{
		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			return 0f;
		}
	}
}

