using UnityEditor;

namespace Thry.ThryEditor.Helpers
{
    internal class GradientPreviewSafeguard : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            GradientPreviewManager.BeforeSaveRestoreOriginals();
            EditorApplication.delayCall += GradientPreviewManager.AfterSaveReapplyPreviews;
            return paths;
        }
    }
}