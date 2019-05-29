#if VRC_SDK_EXISTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRC.Core;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    [ExecuteInEditMode]
    public class VRCContentManager : EditorWindow
    {

        [MenuItem("Thry/VRC/Manage Uploaded Content")]
        static void Init()
        {
            window = (VRCContentManager)EditorWindow.GetWindow(typeof(VRCContentManager));
            window.titleContent.text = "Thry's VRChat Content Manager";
            window.Show();
        }

        const int PageLimit = 20;

        static List<ApiAvatar> uploadedAvatars = null;
        static List<ApiWorld> uploadedWorlds = null;

        public static Dictionary<string, Texture2D> ImageCache = new Dictionary<string, Texture2D>();

        static List<string> justDeletedContents;
        static List<ApiAvatar> justUpdatedAvatars;

        static EditorCoroutine fetchingAvatars = null, fetchingWorlds = null;

        static VRCContentManager window;

        const int SCROLLBAR_RESERVED_REGION_WIDTH = 50;

        const int WORLD_DESCRIPTION_FIELD_WIDTH = 200;
        const int WORLD_IMAGE_BUTTON_WIDTH = 100;
        const int WORLD_RELEASE_STATUS_FIELD_WIDTH = 100;
        const int COPY_WORLD_ID_BUTTON_WIDTH = 75;
        const int DELETE_WORLD_BUTTON_WIDTH = 75;
        const int WORLD_ALL_INFORMATION_MAX_WIDTH = WORLD_DESCRIPTION_FIELD_WIDTH + WORLD_IMAGE_BUTTON_WIDTH + WORLD_RELEASE_STATUS_FIELD_WIDTH + COPY_WORLD_ID_BUTTON_WIDTH + DELETE_WORLD_BUTTON_WIDTH + SCROLLBAR_RESERVED_REGION_WIDTH;
        const int WORLD_REDUCED_INFORMATION_MAX_WIDTH = WORLD_DESCRIPTION_FIELD_WIDTH + WORLD_IMAGE_BUTTON_WIDTH + WORLD_RELEASE_STATUS_FIELD_WIDTH + SCROLLBAR_RESERVED_REGION_WIDTH;

        const int AVATAR_DESCRIPTION_FIELD_WIDTH = 200;
        const int AVATAR_IMAGE_BUTTON_WIDTH = WORLD_IMAGE_BUTTON_WIDTH;
        const int AVATAR_RELEASE_STATUS_FIELD_WIDTH = 100;
        const int SET_AVATAR_STATUS_BUTTON_WIDTH = 100;
        const int COPY_AVATAR_ID_BUTTON_WIDTH = COPY_WORLD_ID_BUTTON_WIDTH;
        const int DELETE_AVATAR_BUTTON_WIDTH = DELETE_WORLD_BUTTON_WIDTH;
        const int AVATAR_ALL_INFORMATION_MAX_WIDTH = AVATAR_DESCRIPTION_FIELD_WIDTH + AVATAR_IMAGE_BUTTON_WIDTH + AVATAR_RELEASE_STATUS_FIELD_WIDTH + SET_AVATAR_STATUS_BUTTON_WIDTH + COPY_AVATAR_ID_BUTTON_WIDTH + DELETE_AVATAR_BUTTON_WIDTH + SCROLLBAR_RESERVED_REGION_WIDTH;
        const int AVATAR_REDUCED_INFORMATION_MAX_WIDTH = AVATAR_DESCRIPTION_FIELD_WIDTH + AVATAR_IMAGE_BUTTON_WIDTH + AVATAR_RELEASE_STATUS_FIELD_WIDTH + SCROLLBAR_RESERVED_REGION_WIDTH;

        const int MAX_ALL_INFORMATION_WIDTH = WORLD_ALL_INFORMATION_MAX_WIDTH > AVATAR_ALL_INFORMATION_MAX_WIDTH ? WORLD_ALL_INFORMATION_MAX_WIDTH : AVATAR_ALL_INFORMATION_MAX_WIDTH;
        const int MAX_REDUCED_INFORMATION_WIDTH = WORLD_REDUCED_INFORMATION_MAX_WIDTH > AVATAR_REDUCED_INFORMATION_MAX_WIDTH ? WORLD_REDUCED_INFORMATION_MAX_WIDTH : AVATAR_REDUCED_INFORMATION_MAX_WIDTH;


        void Update()
        {
            if (APIUser.IsLoggedInWithCredentials && (uploadedWorlds == null || uploadedAvatars == null))
            {
                if (uploadedWorlds == null)
                    uploadedWorlds = new List<ApiWorld>();
                if (uploadedAvatars == null)
                    uploadedAvatars = new List<ApiAvatar>();

                EditorCoroutine.Start(window.FetchUploadedData());
            }

            Repaint();
        }

        [UnityEditor.Callbacks.PostProcessScene]
        static void OnPostProcessScene()
        {
            if (window != null && APIUser.IsLoggedInWithCredentials)
                EditorCoroutine.Start(window.FetchUploadedData());
        }

        void OnFocus()
        {
            if (!APIUser.IsLoggedInWithCredentials)
                return;

            EditorCoroutine.Start(FetchUploadedData());
        }

        public static void ClearContent()
        {
            if (uploadedWorlds != null)
                uploadedWorlds = null;
            if (uploadedAvatars != null)
                uploadedAvatars = null;
            ImageCache.Clear();
        }

        IEnumerator FetchUploadedData()
        {
            if (!RemoteConfig.IsInitialized())
                RemoteConfig.Init();

            if (!APIUser.IsLoggedInWithCredentials)
                yield break;

            ApiCache.ClearResponseCache();
            VRCCachedWWW.ClearOld();

            if (fetchingAvatars == null)
                fetchingAvatars = EditorCoroutine.Start(() => FetchAvatars());
            if (fetchingWorlds == null)
                fetchingWorlds = EditorCoroutine.Start(() => FetchWorlds());
        }

        static void FetchAvatars(int offset = 0)
        {
            ApiAvatar.FetchList(
                delegate (List<ApiAvatar> obj)
                {
                    Debug.LogFormat("<color=yellow>Fetching Avatar Bucket {0}</color>", offset);
                    if (obj.Count > 0)
                        fetchingAvatars = EditorCoroutine.Start(() =>
                        {
                            int count = obj.Count;
                            SetupAvatarData(obj);
                            FetchAvatars(offset + count);
                        });
                    else
                    {
                        fetchingAvatars = null;
                        foreach (ApiAvatar a in uploadedAvatars)
                            DownloadImage(a.id, a.thumbnailImageUrl);
                    }
                },
                delegate (string obj)
                {
                    Debug.LogError("Error fetching your uploaded avatars:\n" + obj);
                    fetchingAvatars = null;
                },
                ApiAvatar.Owner.Mine,
                ApiAvatar.ReleaseStatus.All,
                null,
                PageLimit,
                offset,
                ApiAvatar.SortHeading.None,
                ApiAvatar.SortOrder.Descending,
                false,
                true);
        }

        static void FetchWorlds(int offset = 0)
        {
            ApiWorld.FetchList(
                delegate (List<ApiWorld> obj)
                {
                    Debug.LogFormat("<color=yellow>Fetching World Bucket {0}</color>", offset);
                    if (obj.Count > 0)
                        fetchingWorlds = EditorCoroutine.Start(() =>
                        {
                            int count = obj.Count;
                            SetupWorldData(obj);
                            FetchWorlds(offset + count);
                        });
                    else
                    {
                        fetchingWorlds = null;

                        foreach (ApiWorld w in uploadedWorlds)
                            DownloadImage(w.id, w.thumbnailImageUrl);
                    }
                },
                delegate (string obj)
                {
                    Debug.LogError("Error fetching your uploaded worlds:\n" + obj);
                    fetchingWorlds = null;
                },
                ApiWorld.SortHeading.Updated,
                ApiWorld.SortOwnership.Mine,
                ApiWorld.SortOrder.Descending,
                offset,
                PageLimit,
                "",
                null,
                null,
                null,
                "",
                ApiWorld.ReleaseStatus.All,
                false,
                true);
        }

        static void SetupWorldData(List<ApiWorld> worlds)
        {
            worlds.RemoveAll(w => w == null || w.name == null || uploadedWorlds.Any(w2 => w2.id == w.id));

            if (worlds.Count > 0)
            {
                uploadedWorlds.AddRange(worlds);
                uploadedWorlds.Sort((w1, w2) => w1.name.CompareTo(w2.name));
            }
        }

        static void SetupAvatarData(List<ApiAvatar> avatars)
        {
            avatars.RemoveAll(a => a == null || a.name == null || uploadedAvatars.Any(a2 => a2.id == a.id));

            if (avatars.Count > 0)
            {
                uploadedAvatars.AddRange(avatars);
                uploadedAvatars.Sort((w1, w2) => w1.name.CompareTo(w2.name));
            }
        }

        static void DownloadImage(string id, string url)
        {
            if (ImageCache.ContainsKey(id) && ImageCache[id] != null)
                return;

            System.Action<WWW> onDone = (www) =>
            {
                if (string.IsNullOrEmpty(www.error))
                {
                    try
                    {
                        ImageCache[id] = www.texture;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                else if (ImageCache.ContainsKey(id))
                    ImageCache.Remove(id);
                window.Repaint();
            };

            EditorCoroutine.Start(VRCCachedWWW.Get(url, onDone));
        }

        Vector2 scrollPos;

        bool OnGUIUserInfo()
        {
            if (!RemoteConfig.IsInitialized())
                RemoteConfig.Init();

            if (APIUser.IsLoggedInWithCredentials && uploadedWorlds != null && uploadedAvatars != null)
            {
                EditorGUILayout.LabelField(string.Format(fetchingWorlds != null ? "Fetching Worlds... {0}" : "{0} Worlds", uploadedWorlds.Count.ToString()), EditorStyles.helpBox);
                EditorGUILayout.LabelField(string.Format(fetchingAvatars != null ? "Fetching Avatars... {0}" : "{0} Avatars", uploadedAvatars.Count.ToString()), EditorStyles.helpBox);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                GUIStyle descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                descriptionStyle.wordWrap = true;

                int divideDescriptionWidth = (position.width > MAX_REDUCED_INFORMATION_WIDTH) ? 1 : 2;

                if (uploadedWorlds.Count > 0)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("WORLDS", EditorStyles.boldLabel);
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(WORLD_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                    EditorGUILayout.LabelField("Image", EditorStyles.boldLabel, GUILayout.Width(WORLD_IMAGE_BUTTON_WIDTH));
                    EditorGUILayout.LabelField("Release Status", EditorStyles.boldLabel, GUILayout.Width(WORLD_RELEASE_STATUS_FIELD_WIDTH));
                    EditorGUILayout.EndHorizontal();

                    List<ApiWorld> tmpWorlds = new List<ApiWorld>();

                    if (uploadedWorlds.Count > 0)
                        tmpWorlds = new List<ApiWorld>(uploadedWorlds);

                    foreach (ApiWorld w in tmpWorlds)
                    {
                        if (justDeletedContents != null && justDeletedContents.Contains(w.id))
                        {
                            uploadedWorlds.Remove(w);
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(WORLD_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));

                        EditorGUILayout.LabelField(w.name, descriptionStyle, GUILayout.Width(WORLD_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                        if (ImageCache.ContainsKey(w.id))
                        {
                            if (GUILayout.Button(ImageCache[w.id], GUILayout.Height(100), GUILayout.Width(WORLD_IMAGE_BUTTON_WIDTH)))
                            {
                                Application.OpenURL(w.imageUrl);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("", GUILayout.Height(100), GUILayout.Width(WORLD_IMAGE_BUTTON_WIDTH)))
                            {
                                Application.OpenURL(w.imageUrl);
                            }
                        }

                        if (position.width > MAX_ALL_INFORMATION_WIDTH)
                            EditorGUILayout.BeginHorizontal();
                        else
                            EditorGUILayout.BeginVertical();

                        EditorGUILayout.LabelField(w.releaseStatus, GUILayout.Width(WORLD_RELEASE_STATUS_FIELD_WIDTH));
                        if (GUILayout.Button("Copy ID", GUILayout.Width(COPY_WORLD_ID_BUTTON_WIDTH)))
                        {
                            TextEditor te = new TextEditor();
                            te.text = w.id;
                            te.SelectAll();
                            te.Copy();
                        }
                        if (GUILayout.Button("Delete", GUILayout.Width(DELETE_WORLD_BUTTON_WIDTH)))
                        {
                            if (EditorUtility.DisplayDialog("Delete " + w.name + "?", "Are you sure you want to delete " + w.name + "? This cannot be undone.", "Delete", "Cancel"))
                            {
                                foreach (VRC.Core.PipelineManager pm in FindObjectsOfType<VRC.Core.PipelineManager>().Where(pm => pm.blueprintId == w.id))
                                {
                                    pm.blueprintId = "";
                                    pm.completedSDKPipeline = false;

                                    UnityEditor.EditorUtility.SetDirty(pm);
                                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pm.gameObject.scene);
                                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(pm.gameObject.scene);
                                }

                                API.Delete<ApiWorld>(w.id);
                                uploadedWorlds.RemoveAll(world => world.id == w.id);
                                if (ImageCache.ContainsKey(w.id))
                                    ImageCache.Remove(w.id);

                                if (justDeletedContents == null) justDeletedContents = new List<string>();
                                justDeletedContents.Add(w.id);
                            }
                        }
                        if (position.width > MAX_ALL_INFORMATION_WIDTH)
                            EditorGUILayout.EndHorizontal();
                        else
                            EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                    }

                }

                if (uploadedAvatars.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("AVATARS", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(AVATAR_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                    EditorGUILayout.LabelField("Image", EditorStyles.boldLabel, GUILayout.Width(AVATAR_IMAGE_BUTTON_WIDTH));
                    EditorGUILayout.LabelField("Release Status", EditorStyles.boldLabel, GUILayout.Width(AVATAR_RELEASE_STATUS_FIELD_WIDTH));
                    EditorGUILayout.EndHorizontal();

                    List<ApiAvatar> tmpAvatars = new List<ApiAvatar>();

                    if (uploadedAvatars.Count > 0)
                        tmpAvatars = new List<ApiAvatar>(uploadedAvatars);

                    if (justUpdatedAvatars != null)
                    {
                        foreach (ApiAvatar a in justUpdatedAvatars)
                        {
                            int index = tmpAvatars.FindIndex((av) => av.id == a.id);
                            if (index != -1)
                                tmpAvatars[index] = a;
                        }
                    }

                    foreach (ApiAvatar a in tmpAvatars)
                    {
                        if (justDeletedContents != null && justDeletedContents.Contains(a.id))
                        {
                            uploadedAvatars.Remove(a);
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(AVATAR_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                        EditorGUILayout.LabelField(a.name, descriptionStyle, GUILayout.Width(AVATAR_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                        if (ImageCache.ContainsKey(a.id))
                        {
                            if (GUILayout.Button(ImageCache[a.id], GUILayout.Height(100), GUILayout.Width(AVATAR_IMAGE_BUTTON_WIDTH)))
                            {
                                Application.OpenURL(a.imageUrl);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("", GUILayout.Height(100), GUILayout.Width(AVATAR_IMAGE_BUTTON_WIDTH)))
                            {
                                Application.OpenURL(a.imageUrl);
                            }
                        }
                        if (position.width > MAX_ALL_INFORMATION_WIDTH)
                            EditorGUILayout.BeginHorizontal();
                        else
                            EditorGUILayout.BeginVertical();

                        EditorGUILayout.LabelField(a.releaseStatus, GUILayout.Width(AVATAR_RELEASE_STATUS_FIELD_WIDTH));

                        string oppositeReleaseStatus = a.releaseStatus == "public" ? "private" : "public";
                        if (GUILayout.Button("Make " + oppositeReleaseStatus, GUILayout.Width(SET_AVATAR_STATUS_BUTTON_WIDTH)))
                        {
                            a.releaseStatus = oppositeReleaseStatus;

                            a.SaveReleaseStatus((c) =>
                            {
                                ApiAvatar savedBP = (ApiAvatar)c.Model;

                                if (justUpdatedAvatars == null) justUpdatedAvatars = new List<ApiAvatar>();
                                justUpdatedAvatars.Add(savedBP);

                            },
                            (c) =>
                            {
                                Debug.LogError(c.Error);
                                EditorUtility.DisplayDialog("Avatar Updated", "Failed to change avatar release status", "OK");
                            });
                        }

                        if (GUILayout.Button("Copy ID", GUILayout.Width(COPY_AVATAR_ID_BUTTON_WIDTH)))
                        {
                            TextEditor te = new TextEditor();
                            te.text = a.id;
                            te.SelectAll();
                            te.Copy();
                        }

                        if (GUILayout.Button("Delete", GUILayout.Width(DELETE_AVATAR_BUTTON_WIDTH)))
                        {
                            if (EditorUtility.DisplayDialog("Delete " + a.name + "?", "Are you sure you want to delete " + a.name + "? This cannot be undone.", "Delete", "Cancel"))
                            {
                                foreach (VRC.Core.PipelineManager pm in FindObjectsOfType<VRC.Core.PipelineManager>().Where(pm => pm.blueprintId == a.id))
                                {
                                    pm.blueprintId = "";
                                    pm.completedSDKPipeline = false;

                                    UnityEditor.EditorUtility.SetDirty(pm);
                                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pm.gameObject.scene);
                                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(pm.gameObject.scene);
                                }

                                API.Delete<ApiAvatar>(a.id);
                                uploadedAvatars.RemoveAll(avatar => avatar.id == a.id);
                                if (ImageCache.ContainsKey(a.id))
                                    ImageCache.Remove(a.id);

                                if (justDeletedContents == null) justDeletedContents = new List<string>();
                                justDeletedContents.Add(a.id);
                            }
                        }

                        if (position.width > MAX_ALL_INFORMATION_WIDTH)
                            EditorGUILayout.EndHorizontal();
                        else
                            EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space();
                    }
                }

                EditorGUILayout.EndScrollView();

                return true;
            }
            else
            {
                return false;
            }
        }

        void OnGUI()
        {
            if (window == null)
                window = (VRCContentManager)EditorWindow.GetWindow(typeof(VRCContentManager));

            if (VRC.AccountEditorWindow.OnShowStatus())
                OnGUIUserInfo();

            window.Repaint();
        }
    }
}
#endif