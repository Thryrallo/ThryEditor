using Thry.ThryEditor.Helpers;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.Drawers
{
    public class TextureArrayDrawer : MaterialPropertyDrawer
    {
        private string framesProperty;
        private string fpsProperty;

        public TextureArrayDrawer() { }

        public TextureArrayDrawer(string framesProperty)
        {
            this.framesProperty = framesProperty;
        }

        public TextureArrayDrawer(string framesProperty, string fpsProperty)
        {
            this.framesProperty = framesProperty;
            this.fpsProperty = fpsProperty;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            ShaderProperty shaderProperty = (ShaderProperty)ShaderEditor.Active.CurrentProperty;
            GUILib.ConfigTextureProperty(position, prop, label, editor, true, true);

            if ((ShaderEditor.Input.is_drag_drop_event) && position.Contains(ShaderEditor.Input.mouse_position))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (ShaderEditor.Input.is_drop_event)
                {
                    DragAndDrop.AcceptDrag();
                    HanldeDropEvent(prop, shaderProperty);
                }
            }
            if (ShaderEditor.Active.IsFirstCall)
                ShaderEditor.Active.TextureArrayProperties.Add(shaderProperty);
        }

        public void HanldeDropEvent(MaterialProperty prop, ShaderProperty shaderProperty)
        {
            string[] paths = DragAndDrop.paths;
            Texture2DArray tex;
            float fps = 0;
            if (AssetDatabase.GetMainAssetTypeAtPath(paths[0]) != typeof(Texture2DArray))
                tex = Converter.PathsToTexture2DArray(paths, out fps);
            else
                tex = AssetDatabase.LoadAssetAtPath<Texture2DArray>(paths[0]);
            prop.textureValue = tex;
            UpdateFramesProperty(shaderProperty, tex);
            if (fps > 0)
                UpdateFpsProperty(shaderProperty, fps);
            EditorGUIUtility.ExitGUI();
        }

        private void UpdateFramesProperty(ShaderProperty shaderProperty, Texture2DArray tex)
        {
            if (framesProperty == null)
                framesProperty = shaderProperty.Options.reference_property;

            if (framesProperty != null && ShaderEditor.Active.PropertyDictionary.ContainsKey(framesProperty))
                ShaderEditor.Active.PropertyDictionary[framesProperty].MaterialProperty.SetNumber(tex.depth);
        }

        private void UpdateFpsProperty(ShaderProperty shaderProperty, float fps)
        {
            if (fpsProperty == null)
                fpsProperty = shaderProperty.Options.fps_property;

            if (fpsProperty != null && ShaderEditor.Active.PropertyDictionary.ContainsKey(fpsProperty))
                ShaderEditor.Active.PropertyDictionary[fpsProperty].MaterialProperty.SetNumber(fps);
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            ShaderProperty.RegisterDrawer(this);
            return base.GetPropertyHeight(prop, label, editor);
        }
    }

}