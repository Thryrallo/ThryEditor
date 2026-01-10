using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
	public class Vector3SliderDrawer : MaterialPropertyDrawer
	{
		private readonly float _min;
		private readonly float _max;
		private readonly bool _allowUnbounded;

		public Vector3SliderDrawer() : this(0, 1, 0) { }
		public Vector3SliderDrawer(float min, float max) : this(min, max, 0) { }
		public Vector3SliderDrawer(float min, float max, float allowUnbounded)
		{
			_min = min;
			_max = max;
			_allowUnbounded = allowUnbounded == 1;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			string[] labels = label.text.Split('|');
			string vectorLabel = labels.Length > 0 ? labels[0] : "Vector";
			string sliderLabel = labels.Length > 1 ? labels[1] : "Length";

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = prop.hasMixedValue;

			Vector4 vec = EditorGUI.Vector3Field(position, vectorLabel, prop.vectorValue);

			float sliderValue;
			if (_allowUnbounded)
			{
				Rect controlRect = EditorGUILayout.GetControlRect();
				Rect labelRect = new Rect(controlRect.x, controlRect.y, EditorGUIUtility.labelWidth, controlRect.height);
				Rect sliderRect = new Rect(labelRect.xMax, controlRect.y, controlRect.width - EditorGUIUtility.labelWidth - 55f, controlRect.height);
				Rect fieldRect = new Rect(sliderRect.xMax + 5f, controlRect.y, 50f, controlRect.height);

				EditorGUI.PrefixLabel(labelRect, new GUIContent(sliderLabel));
				sliderValue = GUI.HorizontalSlider(sliderRect, prop.vectorValue.w, _min, _max);
				sliderValue = EditorGUI.FloatField(fieldRect, sliderValue);
			}
			else
			{
				sliderValue = EditorGUILayout.Slider(sliderLabel, prop.vectorValue.w, _min, _max);
			}

			if (EditorGUI.EndChangeCheck())
			{
				prop.vectorValue = new Vector4(vec.x, vec.y, vec.z, sliderValue);
			}
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			ShaderProperty.RegisterDrawer(this);
			return base.GetPropertyHeight(prop, label, editor);
		}
	}
}

