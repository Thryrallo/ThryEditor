#if VRC_SDK_EXISTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VRC.Core;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

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

        const int PageLimit = 100;

        private const string AVATAR_TAGS_FILE_PATH = "./Assets/.ThryAvatarTags";

        static List<Avatar> uploadedAvatars = null;
        static List<ApiWorld> uploadedWorlds = null;

        public static Dictionary<string, Texture2D> ImageCache = new Dictionary<string, Texture2D>();

        static List<string> justDeletedContents;
        static List<ApiAvatar> justUpdatedAvatars;

        static EditorCoroutine fetchingAvatars = null, fetchingWorlds = null;

        static VRCContentManager window;

        private enum ContentSortingMethod { alphabetical=0,upload_date=1,public_private=2 };
        private static readonly string[] ContentSortingMethodNames = new string[] { "Alphabetical", "Upload Date" , "Public,Private" };
        private ContentSortingMethod sortingMethod = ContentSortingMethod.alphabetical;
        private string seach_term = "";

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

        private class Avatar
        {
            public ApiAvatar apiAvatar;
            public string[] tags;
            public string newtag;
            private string lowerCaseName;
            public Avatar(ApiAvatar apiAvatar)
            {
                this.apiAvatar = apiAvatar;
                if (!getAllTags().TryGetValue(apiAvatar.id, out tags)) tags = new string[0];
                newtag = "";
                lowerCaseName = apiAvatar.name.ToLower();
            }
            public void addNewTag()
            {
                string[] newTags = new string[tags.Length + 1];
                for (int i = 0; i < tags.Length; i++) newTags[i] = tags[i];
                newTags[newTags.Length-1] = newtag;
                tags = newTags;
                getAllTags().Remove(apiAvatar.id);
                getAllTags().Add(apiAvatar.id, newTags);
                saveAllTags();
                newtag = "";
            }
            public void deleteTag(string s)
            {
                if (!tags.Contains(s)) return;
                string[] newTags = new string[tags.Length - 1];
                for (int i = 0; i < tags.Length; i++) if(tags[i]!=s) newTags[i] = tags[i];
                tags = newTags;
                getAllTags().Remove(apiAvatar.id);
                getAllTags().Add(apiAvatar.id, tags);
                saveAllTags();
            }
            public bool search(string s)
            {
                if (lowerCaseName.StartsWith(s)) return true;
                foreach (string t in tags) if (t.StartsWith(s)) return true;
                return false;
            }
        }

        private static Dictionary<string, string[]> allTags;
        private static Dictionary<string, string[]> getAllTags()
        {
            if (allTags == null)
            {
                allTags = new Dictionary<string, string[]>();
                string[] tagsStrings = Regex.Split(ThryHelper.readFileIntoString(AVATAR_TAGS_FILE_PATH), @"\r?\n");
                foreach (string s in tagsStrings)
                {
                    string[] data = Regex.Split(s, @"==");
                    if (data[0].Length < 1 || data.Length<2) continue;
                    allTags.Add(data[0], Regex.Split(data[1], @"\|"));
                }
            }
            return allTags;
        }
        private static void saveAllTags()
        {
            if (allTags != null)
            {
                string s = "";
                foreach (KeyValuePair<string, string[]> data in allTags)
                {
                    if (data.Key.Replace(" ", "") == "" || data.Value.Length==0) continue;
                    s += data.Key + "==";
                    foreach (string tag in data.Value) s += tag + "|";
                    if(data.Value.Length>0) s = s.Substring(0, s.Length - 1);
                    s += "\n";
                }
                ThryHelper.writeStringToFile(s, AVATAR_TAGS_FILE_PATH);
            }
        }

        void Update()
        {
            if (APIUser.IsLoggedInWithCredentials && (uploadedWorlds == null || uploadedAvatars == null))
            {
                if (uploadedWorlds == null)
                    uploadedWorlds = new List<ApiWorld>();
                if (uploadedAvatars == null)
                    uploadedAvatars = new List<Avatar>();

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
                        foreach (Avatar a in uploadedAvatars)
                            DownloadImage(a.apiAvatar.id, a.apiAvatar.thumbnailImageUrl);
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
            avatars.RemoveAll(a => a == null || a.name == null || uploadedAvatars.Any(a2 => a2.apiAvatar.id == a.id));

            if (avatars.Count > 0)
            {
                List<Avatar> newAvatars = new List<Avatar>();
                foreach (ApiAvatar a in avatars) newAvatars.Add(new Avatar(a));
                uploadedAvatars.AddRange(newAvatars);
                uploadedAvatars.Sort((a1, a2) => a1.apiAvatar.name.CompareTo(a2.apiAvatar.name));
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

        private class sortByUploadDateHelper : IComparer<Avatar>
        {
            public int Compare(Avatar a1, Avatar a2)
            {
                if (a1.apiAvatar.updated_at < a2.apiAvatar.updated_at) return 1;
                else if (a1.apiAvatar.updated_at > a2.apiAvatar.updated_at) return -1;
                return 0;
            }
        }
        private static IComparer<Avatar> sortByUploadDate()
        {
            return new sortByUploadDateHelper();
        }
        private class sortByPublicPrivateHelper : IComparer<Avatar>
        {
            public int Compare(Avatar a1, Avatar a2)
            {
                bool a1Public = a1.apiAvatar.releaseStatus == "public";
                bool a2Public = a2.apiAvatar.releaseStatus == "public";
                if (a1Public == a2Public) return a1.apiAvatar.name.CompareTo(a2.apiAvatar.name);
                else if (a1Public) return -1;
                else if (a2Public) return 1;
                return 0;
            }
        }
        private static IComparer<Avatar> sortByPublicPrivate()
        {
            return new sortByPublicPrivateHelper();
        }

        Vector2 scrollPos;

        bool OnGUIUserInfo()
        {
            if (!RemoteConfig.IsInitialized())
                RemoteConfig.Init();

            if (APIUser.IsLoggedInWithCredentials && uploadedWorlds != null && uploadedAvatars != null)
            {
                EditorGUILayout.LabelField(string.Format(fetchingAvatars != null ? "Fetching Avatars... {0}" : "{0} Avatars", uploadedAvatars.Count.ToString()), EditorStyles.helpBox);
                EditorGUILayout.LabelField(string.Format(fetchingWorlds != null ? "Fetching Worlds... {0}" : "{0} Worlds", uploadedWorlds.Count.ToString()), EditorStyles.helpBox);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                GUIStyle descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                descriptionStyle.wordWrap = true;

                GUIStyle search_box_style = new GUIStyle(EditorStyles.textField);
                search_box_style.padding = new RectOffset(0, 0, 3, 3);

                GUIStyle deleteButtonStyle = new GUIStyle(EditorStyles.miniButton);
                deleteButtonStyle.fixedWidth = 12;
                deleteButtonStyle.fixedHeight = 12;
                deleteButtonStyle.margin = new RectOffset(0, 0, 5, 0);
                deleteButtonStyle.padding = new RectOffset(0, 0, 0, 0);

                GUIStyle tagLabelStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
                tagLabelStyle.margin = new RectOffset(0, 1, 5, 0);
                tagLabelStyle.padding = new RectOffset(10, 0, 0, 0);

                GUIStyle tagPrefixHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                tagPrefixHeaderStyle.margin = new RectOffset(0, 0, 0, 0);
                tagPrefixHeaderStyle.padding = new RectOffset(0, 0, 4, 0);

                int divideDescriptionWidth = (position.width > MAX_REDUCED_INFORMATION_WIDTH) ? 1 : 2;

                //--paint avatar list--
                if (uploadedAvatars.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("AVATARS", EditorStyles.boldLabel);
                    EditorGUILayout.Space();

                    //search adn sort tools
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Search: ", GUILayout.ExpandWidth(false));
                    seach_term = GUILayout.TextField(seach_term, search_box_style, GUILayout.MaxWidth(200));
                    if (Screen.width < 400)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                    GUILayout.Label("Sort by: ", GUILayout.ExpandWidth(false));
                    sortingMethod = (ContentSortingMethod)EditorGUILayout.Popup((int)sortingMethod, ContentSortingMethodNames, GUILayout.ExpandWidth(false));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(AVATAR_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                    EditorGUILayout.LabelField("Image", EditorStyles.boldLabel, GUILayout.Width(AVATAR_IMAGE_BUTTON_WIDTH));
                    EditorGUILayout.LabelField("Release Status", EditorStyles.boldLabel, GUILayout.Width(AVATAR_RELEASE_STATUS_FIELD_WIDTH));
                    EditorGUILayout.EndHorizontal();

                    List<Avatar> tmpAvatars = new List<Avatar>();

                    if (uploadedAvatars.Count > 0)
                        tmpAvatars = new List<Avatar>(uploadedAvatars);

                    if (justUpdatedAvatars != null)
                    {
                        foreach (ApiAvatar a in justUpdatedAvatars)
                        {
                            int index = tmpAvatars.FindIndex((av) => av.apiAvatar.id == a.id);
                            if (index != -1)
                                tmpAvatars[index] = new Avatar(a);
                        }
                    }

                    if (sortingMethod == ContentSortingMethod.upload_date) tmpAvatars.Sort(sortByUploadDate());
                    else if (sortingMethod == ContentSortingMethod.public_private) tmpAvatars.Sort(sortByPublicPrivate());

                    foreach (Avatar a in tmpAvatars)
                    {
                        if (justDeletedContents != null && justDeletedContents.Contains(a.apiAvatar.id))
                        {
                            uploadedAvatars.Remove(a);
                            continue;
                        }
                        if (!a.search(seach_term)) continue;

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(AVATAR_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                        EditorGUILayout.LabelField(a.apiAvatar.name, descriptionStyle, GUILayout.Width(AVATAR_DESCRIPTION_FIELD_WIDTH / divideDescriptionWidth));
                        if (ImageCache.ContainsKey(a.apiAvatar.id))
                        {
                            if (GUILayout.Button(ImageCache[a.apiAvatar.id], GUILayout.Height(100), GUILayout.Width(AVATAR_IMAGE_BUTTON_WIDTH)))
                            {
                                Application.OpenURL(a.apiAvatar.imageUrl);
                            }
                        }
                        else
                        {
                            if (GUILayout.Button("", GUILayout.Height(100), GUILayout.Width(AVATAR_IMAGE_BUTTON_WIDTH)))
                            {
                                Application.OpenURL(a.apiAvatar.imageUrl);
                            }
                        }

                        //Buttons alignment horizontal or vertical
                        if (position.width > MAX_ALL_INFORMATION_WIDTH)
                            EditorGUILayout.BeginHorizontal();
                        else
                            EditorGUILayout.BeginVertical();

                        EditorGUILayout.LabelField(a.apiAvatar.releaseStatus, GUILayout.Width(AVATAR_RELEASE_STATUS_FIELD_WIDTH));

                        string oppositeReleaseStatus = a.apiAvatar.releaseStatus == "public" ? "private" : "public";
                        if (GUILayout.Button("Make " + oppositeReleaseStatus, GUILayout.Width(SET_AVATAR_STATUS_BUTTON_WIDTH)))
                        {
                            a.apiAvatar.releaseStatus = oppositeReleaseStatus;

                            a.apiAvatar.SaveReleaseStatus((c) =>
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
                            te.text = a.apiAvatar.id;
                            te.SelectAll();
                            te.Copy();
                        }

                        if (GUILayout.Button("Delete", GUILayout.Width(DELETE_AVATAR_BUTTON_WIDTH)))
                        {
                            if (EditorUtility.DisplayDialog("Delete " + a.apiAvatar.name + "?", "Are you sure you want to delete " + a.apiAvatar.name + "? This cannot be undone.", "Delete", "Cancel"))
                            {
                                foreach (VRC.Core.PipelineManager pm in FindObjectsOfType<VRC.Core.PipelineManager>().Where(pm => pm.blueprintId == a.apiAvatar.id))
                                {
                                    pm.blueprintId = "";
                                    pm.completedSDKPipeline = false;

                                    UnityEditor.EditorUtility.SetDirty(pm);
                                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pm.gameObject.scene);
                                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(pm.gameObject.scene);
                                }

                                API.Delete<ApiAvatar>(a.apiAvatar.id);
                                uploadedAvatars.RemoveAll(avatar => avatar.apiAvatar.id == a.apiAvatar.id);
                                if (ImageCache.ContainsKey(a.apiAvatar.id))
                                    ImageCache.Remove(a.apiAvatar.id);

                                if (justDeletedContents == null) justDeletedContents = new List<string>();
                                justDeletedContents.Add(a.apiAvatar.id);
                            }
                        }

                        if (position.width > MAX_ALL_INFORMATION_WIDTH)
                            EditorGUILayout.EndHorizontal();
                        else
                            EditorGUILayout.EndVertical();
                        //buttons done

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Tags: ", tagPrefixHeaderStyle, GUILayout.ExpandWidth(false));
                        foreach (string t in a.tags)
                        {
                            GUILayout.Label(t, tagLabelStyle, GUILayout.ExpandWidth(false));
                            if (GUILayout.Button("X", deleteButtonStyle)) a.deleteTag(t);
                        }
                        GUI.SetNextControlName("new tag input"+a.apiAvatar.id);
                        if (GUI.GetNameOfFocusedControl() != "new tag input"+ a.apiAvatar.id)
                        {
                            GUIStyle greyedStyle = new GUIStyle(EditorStyles.textField);
                            greyedStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
                            EditorGUILayout.TextField("add tag", greyedStyle, GUILayout.ExpandWidth(false));
                        }
                        else
                        {
                            a.newtag = EditorGUILayout.TextField(a.newtag, GUILayout.ExpandWidth(false));
                            Event e = Event.current;
                            if (e.isKey && (e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Return)) a.addNewTag();
                        }
                        
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();

                        EditorGUILayout.Space();
                    }
                }

                //--paint worlds list--
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

        private void drawLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}
#endif