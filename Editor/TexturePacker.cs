using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class TexturePacker : EditorWindow
    {
        [MenuItem("Thry/Texture Packer")]
        public static TexturePacker ShowWindow()
        {
            TexturePacker packer = (TexturePacker)GetWindow(typeof(TexturePacker));
            packer.titleContent = new GUIContent("Thry Texture Packer");
            packer.OnSave = null; // clear save callback
            packer.OnChange = null; // clear save callback
            return packer;
        }

        [MenuItem("Assets/Thry/Open in Texture Packer")]
        public static void OpenInTexturePacker()
        {
            TexturePacker packer = ShowWindow();
            packer.InitilizeWithOneTexture(Selection.activeObject as Texture2D);
        }

        [MenuItem("Assets/Thry/Open in Texture Packer", true)]
        public static bool OpenInTexturePackerValidate()
        {
            return Selection.activeObject is Texture2D;
        }

#region DataStructures
        public enum TextureChannelIn { R, G, B, A, Max, None }
        public enum TextureChannelOut { R, G, B, A, None }
        public enum BlendMode { Add, Multiply, Max, Min }
        public enum InvertMode { None, Invert}
        public enum SaveType { PNG, JPG }
        public enum InputType { Texture, Color, Gradient }
        public enum GradientDirection { Horizontal, Vertical }
        public class ImageAdjust
        {
            public float Brightness = 1;
            public float Hue = 0;
            public float Saturation = 1;
            public float Rotation = 0;
            public Vector2 Scale = Vector2.one;
            public bool ChangeCheck = false;
        }
        static string GetTypeEnding(SaveType t)
        {
            switch (t)
            {
                case SaveType.PNG: return ".png";
                case SaveType.JPG: return ".jpg";
                default: return ".png";
            }
        }
        public class  OutputConfig
        {
            public BlendMode BlendMode;
            public InvertMode Invert;
            public float Fallback;

            public OutputConfig(float fallback = 0)
            {
                Fallback = fallback;
            }
        }

        public class TextureSource
        {
            public Texture2D Texture;
            public long LastHandledTextureEditTime;
            public FilterMode FilterMode;
            public Color Color;
            public Gradient Gradient;
            public GradientDirection GradientDirection;
            public Texture2D GradientTexture;
            public Texture2D TextureTexture;

            InputType _inputType = InputType.Texture;
            public InputType InputType
            {
                get
                {
                    return _inputType;
                }
                set
                {
                    if(_inputType != value)
                    {
                        _inputType = value;
                        if(_inputType == InputType.Texture) Texture = TextureTexture;
                        if(_inputType == InputType.Gradient) Texture = GradientTexture;
                     }
                }
            }

            public TextureSource()
            {
            }

            public TextureSource(Texture2D tex)
            {
                SetTexture(tex);
            }

            public void SetTexture(Texture2D tex)
            {
                Texture = tex;
                FilterMode = tex != null ? tex.filterMode : FilterMode.Bilinear;
            }

            public static void SetUncompressedTextureDirty(Texture2D tex)
            {
                if (_cachedUncompressedTextures.ContainsKey(tex))
                {
                    _cachedUncompressedTexturesNeedsReload[tex] = true;
                }
            }

            static Dictionary<Texture2D, Texture2D> _cachedUncompressedTextures = new Dictionary<Texture2D, Texture2D>();
            static Dictionary<Texture2D, bool> _cachedUncompressedTexturesNeedsReload = new Dictionary<Texture2D, bool>();
            public Texture2D UncompressedTexture
            {
                get
                {
                    if(_cachedUncompressedTextures.ContainsKey(Texture) == false || _cachedUncompressedTexturesNeedsReload[Texture])
                    {
                        string path = AssetDatabase.GetAssetPath(Texture);
                        if(path.EndsWith(".png") || path.EndsWith(".jpg"))
                        {
                            EditorUtility.DisplayProgressBar("Loading Raw PNG", "Loading " + path, 0.5f);
                            Texture2D tex = new Texture2D(2,2, TextureFormat.RGBA32, false, true);
                            tex.LoadImage(System.IO.File.ReadAllBytes(path));
                            tex.filterMode = Texture.filterMode;
                            _cachedUncompressedTextures[Texture] = tex;
                            EditorUtility.ClearProgressBar();
                        }else if (path.EndsWith(".tga"))
                        {
                            Texture2D tex = TextureHelper.LoadTGA(path, true);
                            tex.filterMode = Texture.filterMode;
                            _cachedUncompressedTextures[Texture] = tex;
                        }
                        else
                        {
                            _cachedUncompressedTextures[Texture] = Texture;
                        }
                        _cachedUncompressedTexturesNeedsReload[Texture] = false;
                    }
                    return _cachedUncompressedTextures[Texture];
                }
            }

            public void FindMaxSize(ref int width, ref int height)
            {
                if (Texture == null) return;
                width = Mathf.Max(width, UncompressedTexture.width);
                height = Mathf.Max(height, UncompressedTexture.height);
            }
        }

        public class Connection
        {
            public int FromTextureIndex = -1;
            public TextureChannelIn FromChannel = TextureChannelIn.None;
            public TextureChannelOut ToChannel = TextureChannelOut.None;

            public static Connection CreateFull(int index, TextureChannelIn channel, TextureChannelOut toChannel)
            {
                Connection connection = new Connection();
                connection.FromTextureIndex = index;
                connection.FromChannel = channel;
                connection.ToChannel = toChannel;
                return connection;
            }

            public static Connection Create(int index, TextureChannelIn channel)
            {
                Connection connection = new Connection();
                connection.FromTextureIndex = index;
                connection.FromChannel = channel;
                return connection;
            }

            public static Connection Create(TextureChannelOut channel)
            {
                Connection connection = new Connection();
                connection.ToChannel = channel;
                return connection;
            }

            public void SetFrom(int index, TextureChannelIn channel, TexturePacker packer)
            {
                // cancle if selecting same channel
                if(FromTextureIndex == index && FromChannel == channel)
                {
                    packer._creatingConnection = null;
                    return;
                }
                // set
                FromTextureIndex = index;
                FromChannel = channel;
                // check if done
                if(ToChannel == TextureChannelOut.None) return;
                // check if already exists
                if(packer._connections.Exists(c => c.ToChannel == ToChannel && c.FromTextureIndex == FromTextureIndex && c.FromChannel == FromChannel))
                {
                    packer._creatingConnection = null;
                    return;
                }
                packer._connections.Add(this);
                packer._creatingConnection = null;
                packer._changeCheckForPacking = true;
            }

            public void SetTo(TextureChannelOut channel, TexturePacker packer)
            {
                // cancle if selecting same channel
                if (ToChannel == channel)
                {
                    return;
                }
                // set
                ToChannel = channel;
                // check if done
                if(FromTextureIndex == -1 || FromChannel == TextureChannelIn.None) return;
                // check if already exists
                if(packer._connections.Exists(c => c.ToChannel == ToChannel && c.FromTextureIndex == FromTextureIndex && c.FromChannel == FromChannel))
                {
                    packer._creatingConnection = null;
                    return;
                }
                packer._connections.Add(this);
                packer._creatingConnection = null;
                packer._changeCheckForPacking = true;
            }

            Vector3 _bezierStart, _bezierEnd, _bezierStartTangent, _bezierEndTangent;

            public void CalculateBezierPoints(Vector2[] positionsIn, Vector2[] positionsOut)
            {
                _bezierStart = positionsIn[FromTextureIndex * 5 + (int)FromChannel];
                _bezierEnd = positionsOut[(int)ToChannel];
                _bezierStartTangent = _bezierStart + Vector3.right * 50;
                _bezierEndTangent = _bezierEnd + Vector3.left * 50;
            }

            public Vector3 BezierStart { get { return _bezierStart; } }
            public Vector3 BezierEnd { get { return _bezierEnd; } }
            public Vector3 BezierStartTangent { get { return _bezierStartTangent; } }
            public Vector3 BezierEndTangent { get { return _bezierEndTangent; } }
        }
#endregion

        const string CHANNEL_PREVIEW_SHADER = "Hidden/Thry/ChannelPreview";

        TextureSource[] _textureSources = new TextureSource[]
        {
            new TextureSource(),
            new TextureSource(),
            new TextureSource(),
            new TextureSource(),
        };
        OutputConfig[] _outputConfigs = new OutputConfig[]
        {
            new OutputConfig(0),
            new OutputConfig(0),
            new OutputConfig(0),
            new OutputConfig(1),
        };
        

        List<Connection> _connections = new List<Connection>();
        Connection _creatingConnection;
        Texture2D _outputTexture;
        ColorSpace _colorSpace = ColorSpace.Uninitialized;
        FilterMode _filterMode = FilterMode.Bilinear;

        string _saveFolder;
        string _saveName;
        SaveType _saveType = SaveType.PNG;
        float _saveQuality = 1;

        ImageAdjust _colorAdjust = new ImageAdjust();

        public Action<Texture2D> OnSave;
        public Action<Texture2D, TextureSource[], OutputConfig[], Connection[]> OnChange;
        
        static Material s_channelPreviewMaterial;
        static Material ChannelPreviewMaterial
        {
            get
            {
                if (s_channelPreviewMaterial == null)
                {
                    s_channelPreviewMaterial = new Material(Shader.Find(CHANNEL_PREVIEW_SHADER));
                }
                return s_channelPreviewMaterial;
            }
        }

        static ComputeShader s_computeShader;
        static ComputeShader ComputeShader
        {
            get
            {
                if (s_computeShader == null)
                {
                    s_computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(AssetDatabase.GUIDToAssetPath("56f54c5664777a747b2552701571174d"));
                }
                return s_computeShader;
            }
        }

        Vector2[] _positionsChannelIn = new Vector2[20];
        Vector2[] _positionsChannelOut = new Vector2[4];

        public void InitilizeWithData(TextureSource[] sources, OutputConfig[] configs, IEnumerable<Connection> connections, FilterMode filterMode, ColorSpace colorSpace)
        {
            _textureSources = new TextureSource[]
            {
                new TextureSource(),
                new TextureSource(),
                new TextureSource(),
                new TextureSource(),
            };
            Array.Copy(sources, _textureSources, sources.Length);
            _outputConfigs = configs;
            _connections = connections.ToList();
            _filterMode = filterMode;
            _colorSpace = colorSpace;
            // Reset Color Adjust
            _colorAdjust = new ImageAdjust();
            DeterminePath();
            DetermineImportSettings();
            Pack();
        }

        void InitilizeWithOneTexture(Texture2D texture)
        {
            _connections.Clear();
            _textureSources[0].SetTexture(texture);
            _textureSources[1].SetTexture(texture);
            _textureSources[2].SetTexture(texture);
            _textureSources[3].SetTexture(texture);
            // Add connections
            _connections.Add(Connection.CreateFull(0, TextureChannelIn.R, TextureChannelOut.R));
            _connections.Add(Connection.CreateFull(1, TextureChannelIn.G, TextureChannelOut.G));
            _connections.Add(Connection.CreateFull(2, TextureChannelIn.B, TextureChannelOut.B));
            _connections.Add(Connection.CreateFull(3, TextureChannelIn.A, TextureChannelOut.A));
            // Reset Color Adjust
            _colorAdjust = new ImageAdjust();
            DeterminePath();
            DetermineImportSettings();
            Pack();
        }

        const int TOP_OFFSET = 50;
        const int INPUT_PADDING = 20;
        const int OUTPUT_HEIGHT = 300;

        bool _changeCheckForPacking;
        private void OnGUI()
        {
            // Draw three texture slots on the left, a space in the middle, and one texutre slot on the right
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); 

            _changeCheckForPacking = false;

            GUILayout.BeginVertical();
            GUILayout.Space(TOP_OFFSET);
            bool didInputTexturesChange = false;
            didInputTexturesChange |= DrawInput( _textureSources[0], 0);
            GUILayout.Space(INPUT_PADDING);
            didInputTexturesChange |= DrawInput( _textureSources[1], 1);
            GUILayout.Space(INPUT_PADDING);
            didInputTexturesChange |= DrawInput( _textureSources[2], 2);
            GUILayout.Space(INPUT_PADDING);
            didInputTexturesChange |= DrawInput( _textureSources[3], 3);
            GUILayout.EndVertical();
            float inputHeight = 120 * 4 + INPUT_PADDING * 3 + TOP_OFFSET;

            GUILayout.Space(400);
            GUILayout.BeginVertical();
            GUILayout.Space(TOP_OFFSET);
            GUILayout.Space((inputHeight - TOP_OFFSET - OUTPUT_HEIGHT) / 2);
            DrawOutput(_outputTexture, OUTPUT_HEIGHT);

            EditorGUILayout.Space(15);
            Rect backgroundImageSettings = EditorGUILayout.BeginVertical();
            backgroundImageSettings = new RectOffset(5, 5, 5, 5).Add(backgroundImageSettings);
            GUI.DrawTexture(backgroundImageSettings, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1, Styles.COLOR_BACKGROUND_1, 0, 10);

            EditorGUI.BeginChangeCheck();
            _colorSpace = (ColorSpace)EditorGUILayout.EnumPopup(_colorSpace);
            _filterMode = (FilterMode)EditorGUILayout.EnumPopup(_filterMode);
            _changeCheckForPacking |= EditorGUI.EndChangeCheck();

            // Make the sliders delayed, else the UX feels terrible
            EditorGUI.BeginChangeCheck();
            EventType eventTypeBeforerSliders  = Event.current.type;
            _colorAdjust.Scale = EditorGUILayout.Vector2Field("Scale", _colorAdjust.Scale);
            _colorAdjust.Rotation = EditorGUILayout.Slider("Rotation", _colorAdjust.Rotation, -180, 180);
            _colorAdjust.Hue = EditorGUILayout.Slider("Hue", _colorAdjust.Hue, 0, 1);
            _colorAdjust.Saturation = EditorGUILayout.Slider("Saturation", _colorAdjust.Saturation, 0, 3);
            _colorAdjust.Brightness = EditorGUILayout.Slider("Brightness", _colorAdjust.Brightness, 0, 3);
            _colorAdjust.ChangeCheck |= EditorGUI.EndChangeCheck();
            if(_colorAdjust.ChangeCheck && (eventTypeBeforerSliders == EventType.MouseUp || (eventTypeBeforerSliders == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)))
            {
                _changeCheckForPacking = true;
                _colorAdjust.ChangeCheck = false;
            }

            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace(); 
            GUILayout.EndHorizontal();

            DrawConnections();

            if(didInputTexturesChange)
            {
                DetermineImportSettings();
            }
            if(_changeCheckForPacking)
            {
                Pack();
                Repaint();
            }

            GUILayout.Space(20);
            DrawSaveGUI();
            GUILayout.EndVertical();

            HandleConnectionDeletion();
            HandleConnectionCreationHandle();
        }

        void HandleConnectionDeletion()
        {
            if(Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Delete)
            {
                Connection toDelete = CheckIfConnectionClicked();
                if(toDelete != null)
                {
                    _connections.Remove(toDelete);
                    Pack();
                    Repaint();
                }
            }
        }

        void HandleConnectionCreationHandle()
        {
            if(_creatingConnection != null)
            {
                // if user clicked anywhere on the screen, stop creating the connection
                if(Event.current.type == EventType.MouseDown)
                {
                    _creatingConnection = null;
                    return;
                }

                Vector2 bezierStart, bezierEnd, bezierStartTangent, bezierEndTangent;
                Color color = Color.white;

                bezierEnd = Event.current.mousePosition;

                if(_creatingConnection.FromChannel != TextureChannelIn.None)
                {
                    bezierStart = _positionsChannelIn[_creatingConnection.FromTextureIndex * 5 + (int)_creatingConnection.FromChannel];
                    bezierStartTangent = bezierStart + Vector2.right * 50;
                    bezierEndTangent = bezierEnd + Vector2.left * 50;
                    color = GetColor(_creatingConnection.FromChannel);
                }
                else
                {
                    bezierStart = _positionsChannelOut[(int)_creatingConnection.ToChannel];
                    bezierStartTangent = bezierStart + Vector2.left * 50;
                    bezierEndTangent = bezierEnd + Vector2.right * 50;
                    color = GetColor(_creatingConnection.ToChannel);
                }

                Handles.DrawBezier(bezierStart, bezierEnd, bezierStartTangent, bezierEndTangent, color, null, 2);
                Repaint();
            }
        }

        Connection CheckIfConnectionClicked()
        {
            Vector2 mousePos = Event.current.mousePosition;
            float minDistance = 50;
            Connection closestConnection = null;
            foreach(Connection c in _connections)
            {
                Vector3 from = c.BezierStart;
                Vector3 to = c.BezierEnd;
                float topY = Mathf.Max(from.y, to.y);
                float bottomY = Mathf.Min(from.y, to.y);
                float leftX = Mathf.Min(from.x, to.x);
                float rightX = Mathf.Max(from.x, to.x);
                // check if mouse is in the area of the bezier curve
                if(mousePos.x > leftX && mousePos.x < rightX)
                {
                    if(mousePos.y > bottomY && mousePos.y < topY)
                    {
                        // check if mouse is close to the bezier curve
                        float distance = HandleUtility.DistancePointBezier(mousePos, c.BezierStart, c.BezierEnd, c.BezierStartTangent, c.BezierEndTangent);
                        if(distance < 50)
                        {
                            if(distance < minDistance)
                            {
                                minDistance = distance;
                                closestConnection = c;
                            }
                        }
                    }
                }
            }
            return closestConnection;
        }

        void DrawSaveGUI()
        {
            // Saving information
            // folder selection
            // determine folder & filename from asset name if not set
            if(string.IsNullOrEmpty(_saveFolder) || string.IsNullOrEmpty(_saveName))
            {
                DeterminePath();
            }

            Rect r = EditorGUILayout.BeginHorizontal();

            Rect background = new Rect(r.x + r.width / 2 - 400, r.y - 5, 800, 50);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Styles.COLOR_BACKGROUND_1, 0, 10);

            GUILayout.FlexibleSpace();
            // show current path
            GUILayout.Label("Save to: ");
            GUILayout.Label(_saveFolder + "\\");
            _saveName = GUILayout.TextField(_saveName, GUILayout.MinWidth(50));
            _saveType = (SaveType)EditorGUILayout.EnumPopup(_saveType, GUILayout.Width(70));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Change Folder", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("Select folder", _saveFolder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _saveFolder = path;
                }
            }
            if(GUILayout.Button("Save", GUILayout.Width(100)))
            {
                Save();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DeterminePath()
        {
            foreach(TextureSource s in _textureSources)
            {
                if(s.Texture != null)
                {
                    string path = AssetDatabase.GetAssetPath(s.Texture);
                    if(string.IsNullOrWhiteSpace(path))
                        continue;
                    _saveFolder = Path.GetDirectoryName(path);
                    _saveName = Path.GetFileNameWithoutExtension(path) + "_packed";
                    break;
                }
            }
        }

        void DetermineImportSettings()
        {
            _colorSpace = ColorSpace.Gamma;
            _filterMode = FilterMode.Bilinear;
            foreach(TextureSource s in _textureSources)
            {
                if(DetermineImportSettings(s))
                    break;
            }
        }

        bool DetermineImportSettings(TextureSource s)
        {
            if(s.Texture != null)
            {
                string path = AssetDatabase.GetAssetPath(s.Texture);
                if(path == null)
                    return false;
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer != null)
                {
                    _colorSpace = importer.sRGBTexture ? ColorSpace.Gamma : ColorSpace.Linear;
                    _filterMode = importer.filterMode;
                    return true;
                }
            }
            return false;
        }

        void DrawConnections()
        {
            // Draw connections as lines
            foreach (Connection c in _connections)
            {
                c.CalculateBezierPoints(_positionsChannelIn, _positionsChannelOut);
                Handles.DrawBezier(c.BezierStart, c.BezierEnd, c.BezierStartTangent, c.BezierEndTangent, GetColor(c.FromChannel), null, 2);
            }
        }

        void DrawOutput(Texture2D texture, int height = 200)
        {
            int channelWidth = height / 4;

            Rect rect = GUILayoutUtility.GetRect(height, height);

            // draw 4 channl boxes on the left side
            Rect rectR = new Rect(rect.x - channelWidth, rect.y, channelWidth, channelWidth);
            Rect rectG = new Rect(rect.x - channelWidth, rect.y + channelWidth, channelWidth, channelWidth);
            Rect rectB = new Rect(rect.x - channelWidth, rect.y + channelWidth * 2, channelWidth, channelWidth);
            Rect rectA = new Rect(rect.x - channelWidth, rect.y + channelWidth * 3, channelWidth, channelWidth);

            // Draw circle button bext to each channel box
            int buttonWidth = 80;
            int buttonHeight = 40;
            Rect buttonR = new Rect(rectR.x - buttonWidth - 5, rectR.y + rectR.height / 2 - buttonHeight / 2, buttonWidth, buttonHeight);
            Rect buttonG = new Rect(rectG.x - buttonWidth - 5, rectG.y + rectG.height / 2 - buttonHeight / 2, buttonWidth, buttonHeight);
            Rect buttonB = new Rect(rectB.x - buttonWidth - 5, rectB.y + rectB.height / 2 - buttonHeight / 2, buttonWidth, buttonHeight);
            Rect buttonA = new Rect(rectA.x - buttonWidth - 5, rectA.y + rectA.height / 2 - buttonHeight / 2, buttonWidth, buttonHeight);

            // Draw background
            Rect background = new Rect(buttonR.x + 10, rect.y - 5, (rect.x + rect.width + 5) - (buttonR.x + 10), rect.height + 10);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1, Styles.COLOR_BACKGROUND_1, 0, 10);

            EditorGUI.DrawTextureTransparent(rect, texture != null ? texture : Texture2D.blackTexture, ScaleMode.ScaleToFit, 1);
           // draw 4 channl boxes on the left side
            if (texture != null)
            {
                ChannelPreviewMaterial.SetTexture("_MainTex", texture);
                ChannelPreviewMaterial.SetFloat("_Channel", 0);
                EditorGUI.DrawPreviewTexture(rectR, texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
                ChannelPreviewMaterial.SetFloat("_Channel", 1);
                EditorGUI.DrawPreviewTexture(rectG, texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
                ChannelPreviewMaterial.SetFloat("_Channel", 2);
                EditorGUI.DrawPreviewTexture(rectB, texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
                ChannelPreviewMaterial.SetFloat("_Channel", 3);
                EditorGUI.DrawPreviewTexture(rectA, texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(rectR, Color.black);
                EditorGUI.DrawRect(rectG, Color.black);
                EditorGUI.DrawRect(rectB, Color.black);
                EditorGUI.DrawRect(rectA, Color.black);
            }
            // Draw circle button bext to each channel box
            _positionsChannelOut[0] = new Vector2(buttonR.x, buttonR.y + buttonR.height / 2);
            _positionsChannelOut[1] = new Vector2(buttonG.x, buttonG.y + buttonG.height / 2);
            _positionsChannelOut[2] = new Vector2(buttonB.x, buttonB.y + buttonB.height / 2);
            _positionsChannelOut[3] = new Vector2(buttonA.x, buttonA.y + buttonA.height / 2);
            DrawOutputChannel(buttonR, TextureChannelOut.R, _outputConfigs[0]);
            DrawOutputChannel(buttonG, TextureChannelOut.G, _outputConfigs[1]);
            DrawOutputChannel(buttonB, TextureChannelOut.B, _outputConfigs[2]);
            DrawOutputChannel(buttonA, TextureChannelOut.A, _outputConfigs[3]);
        }

        void DrawOutputChannel(Rect position, TextureChannelOut channel, OutputConfig config)
        {
            // RGBA on the left side
            // fallback or (blendmode & invert) on the right side
            Rect channelRect = new Rect(position.x, position.y, 20, position.height);
            Rect fallbackRect = new Rect(position.x + 20, position.y, position.width - 20, position.height);
            Rect blendmodeRect = new Rect(fallbackRect.x, fallbackRect.y, fallbackRect.width, fallbackRect.height / 2);
            Rect invertRect = new Rect(fallbackRect.x, fallbackRect.y + fallbackRect.height / 2, fallbackRect.width, fallbackRect.height / 2);
            
            if (GUI.Button(channelRect, channel.ToString()))
            {
                if (_creatingConnection != null) _creatingConnection.SetTo(channel, this);
                else _creatingConnection = Connection.Create(channel);
            }
            EditorGUI.BeginChangeCheck();
            if(DoFallback(channel))
            {
                config.Fallback = EditorGUI.FloatField(fallbackRect, config.Fallback);
            }else
            {
                config.BlendMode = (BlendMode)EditorGUI.EnumPopup(blendmodeRect, config.BlendMode);
                config.Invert = (InvertMode)EditorGUI.EnumPopup(invertRect, config.Invert);
            }
            _changeCheckForPacking |= EditorGUI.EndChangeCheck();
        }

        bool DrawInput(TextureSource texture, int index, int textureHeight = 100)
        {
            int channelWidth = textureHeight / 5;
            Rect rect = GUILayoutUtility.GetRect(textureHeight, textureHeight + 40);
            Rect typeRect = new Rect(rect.x, rect.y, textureHeight, 20);
            Rect textureRect = new Rect(rect.x, rect.y + 20, textureHeight, textureHeight);
            Rect filterRect = new Rect(textureRect.x, textureRect.y + textureHeight, textureRect.width, 20);

            Rect background = new Rect(rect.x - 5, rect.y - 5, rect.width + channelWidth + 40, rect.height + 10);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1, Styles.COLOR_BACKGROUND_1, 0, 10);

            // Draw textrue & filtermode. Change filtermode if texture is changed
            EditorGUI.BeginChangeCheck();
            texture.InputType = (InputType)EditorGUI.EnumPopup(typeRect, texture.InputType);
            bool didTextureChange = false;
            switch(texture.InputType)
            {
                case InputType.Texture:
                    EditorGUI.BeginChangeCheck();
                    texture.Texture = (Texture2D)EditorGUI.ObjectField(textureRect, texture.Texture, typeof(Texture2D), false);
                    didTextureChange = EditorGUI.EndChangeCheck();
                    if(didTextureChange && texture.Texture != null) texture.FilterMode = texture.Texture.filterMode;
                    texture.FilterMode = (FilterMode)EditorGUI.EnumPopup(filterRect, texture.FilterMode);
                    break;
                case InputType.Gradient:
                    if(texture.GradientTexture == null) EditorGUI.DrawRect(textureRect, Color.black);
                    else EditorGUI.DrawPreviewTexture(textureRect, texture.GradientTexture);
                    if(Event.current.type == EventType.MouseDown && textureRect.Contains(Event.current.mousePosition))
                    {
                        if(texture.Gradient == null) texture.Gradient = new Gradient();
                        GradientEditor2.Open(texture.Gradient, GetMaxTextureSize(_textureSources), texture.GradientDirection == GradientDirection.Vertical, (Gradient gradient, Texture2D tex) => {
                            texture.Gradient = gradient;
                            texture.GradientTexture = tex;
                            texture.Texture = tex;
                            // Needs to call these itself because it's in a callback not the OnGUI method
                            Pack();
                            Repaint();
                        });

                    }
                    EditorGUI.BeginChangeCheck();
                    texture.GradientDirection = (GradientDirection)EditorGUI.EnumPopup(filterRect, texture.GradientDirection);
                    if(EditorGUI.EndChangeCheck() && texture.Gradient != null)
                    {
                        Vector2Int size = GetMaxTextureSize(_textureSources);
                        texture.GradientTexture = Converter.GradientToTexture(texture.Gradient, size.x, size.y, texture.GradientDirection == GradientDirection.Vertical);
                        texture.Texture = texture.GradientTexture;
                    }
                    break;
                case InputType.Color:
                    EditorGUI.BeginChangeCheck();
                    texture.Color = EditorGUI.ColorField(textureRect, texture.Color);
                    if(EditorGUI.EndChangeCheck())
                    {
                        texture.Texture = Converter.ColorToTexture(texture.Color, 16, 16);
                    }
                    break;
            }
            
            _changeCheckForPacking |= EditorGUI.EndChangeCheck();

            // draw 4 channl boxes on the right side
            Rect rectR = new Rect(textureRect.x + textureRect.width, textureRect.y, channelWidth, channelWidth);
            Rect rectG = new Rect(textureRect.x + textureRect.width, textureRect.y + channelWidth, channelWidth, channelWidth);
            Rect rectB = new Rect(textureRect.x + textureRect.width, textureRect.y + channelWidth * 2, channelWidth, channelWidth);
            Rect rectA = new Rect(textureRect.x + textureRect.width, textureRect.y + channelWidth * 3, channelWidth, channelWidth);
            Rect rectMax = new Rect(textureRect.x + textureRect.width, textureRect.y + channelWidth * 4, channelWidth, channelWidth);
            if (texture.Texture != null)
            {
                ChannelPreviewMaterial.SetTexture("_MainTex", texture.Texture);
                ChannelPreviewMaterial.SetFloat("_Channel", 0);
                EditorGUI.DrawPreviewTexture(rectR, texture.Texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
                ChannelPreviewMaterial.SetFloat("_Channel", 1);
                EditorGUI.DrawPreviewTexture(rectG, texture.Texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
                ChannelPreviewMaterial.SetFloat("_Channel", 2);
                EditorGUI.DrawPreviewTexture(rectB, texture.Texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
                ChannelPreviewMaterial.SetFloat("_Channel", 3);
                EditorGUI.DrawPreviewTexture(rectA, texture.Texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
                ChannelPreviewMaterial.SetFloat("_Channel", 4);
                EditorGUI.DrawPreviewTexture(rectMax, texture.Texture, ChannelPreviewMaterial, ScaleMode.ScaleToFit);
            }else
            {
                EditorGUI.DrawRect(rectR, Color.black);
                EditorGUI.DrawRect(rectG, Color.black);
                EditorGUI.DrawRect(rectB, Color.black);
                EditorGUI.DrawRect(rectA, Color.black);
                EditorGUI.DrawRect(rectMax, Color.black);
            }
            // Draw circle button bext to each channel box
            Rect circleR = new Rect(rectR.x + rectR.width + 5, rectR.y + rectR.height / 2 - 10 + 1, 40, 18);
            Rect circleG = new Rect(rectG.x + rectG.width + 5, rectG.y + rectG.height / 2 - 10 + 1, 40, 18);
            Rect circleB = new Rect(rectB.x + rectB.width + 5, rectB.y + rectB.height / 2 - 10 + 1, 40, 18);
            Rect circleA = new Rect(rectA.x + rectA.width + 5, rectA.y + rectA.height / 2 - 10 + 1, 40, 18);
            Rect circleMax = new Rect(rectMax.x + rectMax.width + 5, rectMax.y + rectMax.height / 2 - 10, 40, 18);
            _positionsChannelIn[index * 5 + 0] = new Vector2(circleR.x + circleR.width, circleR.y + circleR.height / 2);
            _positionsChannelIn[index * 5 + 1] = new Vector2(circleG.x + circleG.width, circleG.y + circleG.height / 2);
            _positionsChannelIn[index * 5 + 2] = new Vector2(circleB.x + circleB.width, circleB.y + circleB.height / 2);
            _positionsChannelIn[index * 5 + 3] = new Vector2(circleA.x + circleA.width, circleA.y + circleA.height / 2);
            _positionsChannelIn[index * 5 + 4] = new Vector2(circleMax.x + circleMax.width, circleMax.y + circleMax.height / 2);
            DrawInputChannel(circleR, index, TextureChannelIn.R);
            DrawInputChannel(circleG, index, TextureChannelIn.G);
            DrawInputChannel(circleB, index, TextureChannelIn.B);
            DrawInputChannel(circleA, index, TextureChannelIn.A);
            DrawInputChannel(circleMax, index, TextureChannelIn.Max);

            return didTextureChange;
        }

        void DrawInputChannel(Rect position, int index, TextureChannelIn channel)
        {
            if(GUI.Button(position, channel.ToString()))
            {
                if (_creatingConnection == null) _creatingConnection = Connection.Create(index, channel);
                else _creatingConnection.SetFrom(index, channel, this);
            }
        }

        

        bool DoFallback(TextureChannelOut channel)
        {
            return _connections.Any(c => c.ToChannel == channel && c.FromTextureIndex != -1
                && _textureSources[c.FromTextureIndex].Texture != null) == false;
        }

        Color GetColor(TextureChannelIn c)
        {
            switch (c)
            {
                case TextureChannelIn.R: return Color.red;
                case TextureChannelIn.G: return Color.green;
                case TextureChannelIn.B: return Color.blue;
                case TextureChannelIn.A: return Color.white;
                case TextureChannelIn.Max: return Color.yellow;
                default: return Color.black;
            }
        }

        Color GetColor(TextureChannelOut c)
        {
            switch (c)
            {
                case TextureChannelOut.R: return Color.red;
                case TextureChannelOut.G: return Color.green;
                case TextureChannelOut.B: return Color.blue;
                case TextureChannelOut.A: return Color.white;
                default: return Color.black;
            }
        }

        // Packing Logic

        void Pack()
        {
            _outputTexture = Pack(_textureSources, _outputConfigs, _connections, _filterMode, _colorSpace, _colorAdjust);
            if(OnChange != null) OnChange(_outputTexture, _textureSources, _outputConfigs, _connections.ToArray());
        }

        static Vector2Int GetMaxTextureSize(TextureSource[] sources)
        {
            int width = 16;
            int height = 16;
            foreach (TextureSource source in sources)
            {
                source.FindMaxSize(ref width, ref height);
            }
            return new Vector2Int(width, height);
        }

        public static Texture2D Pack(TextureSource[] sources, OutputConfig[] outputConfigs, IEnumerable<Connection> connections, FilterMode targetFilterMode, ColorSpace targetColorSpace, ImageAdjust colorAdjust = null)
        {
            Vector2Int maxTextureSize = GetMaxTextureSize(sources);
            int width = maxTextureSize.x;
            int height = maxTextureSize.y;

            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.filterMode = targetFilterMode;
            target.Create();

            ComputeShader.SetTexture(0, "Result", target);
            ComputeShader.SetFloat("Width", width);
            ComputeShader.SetFloat("Height", height);

            if(colorAdjust == null) colorAdjust = new ImageAdjust();
            ComputeShader.SetFloat("Rotation", colorAdjust.Rotation / 360f * 2f * Mathf.PI);
            ComputeShader.SetVector("Scale", colorAdjust.Scale);
            ComputeShader.SetFloat("Hue", colorAdjust.Hue);
            ComputeShader.SetFloat("Saturation", colorAdjust.Saturation);
            ComputeShader.SetFloat("Brightness", colorAdjust.Brightness);

            bool repeatTextures = Math.Abs(colorAdjust.Scale.x) > 1 || Math.Abs(colorAdjust.Scale.y) > 1;

            // Set Compute Shader Properties
            int rCons = SetComputeValues(sources, connections, outputConfigs[0], TextureChannelOut.R, repeatTextures);
            int gCons = SetComputeValues(sources, connections, outputConfigs[1], TextureChannelOut.G, repeatTextures);
            int bCons = SetComputeValues(sources, connections, outputConfigs[2], TextureChannelOut.B, repeatTextures);
            int aCons = SetComputeValues(sources, connections, outputConfigs[3], TextureChannelOut.A, repeatTextures);

            bool hasTransparency = aCons > 0 || outputConfigs[3].Fallback < 1; 

            ComputeShader.Dispatch(0, width / 8 + 1, height / 8 + 1, 1);

            Texture2D atlas = new Texture2D(width, height, TextureFormat.RGBA32, true, targetColorSpace == ColorSpace.Linear);
            RenderTexture.active = target;
            atlas.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            atlas.filterMode = targetFilterMode;
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.alphaIsTransparency = hasTransparency;
            atlas.Apply();

            return atlas;
        }

        static int SetComputeValues(TextureSource[] sources, IEnumerable<Connection> allConnections, OutputConfig config, TextureChannelOut outChannel, bool repeatMode)
        {
            // Find all incoming connections
            Connection[] chnlConnections = allConnections.Where(c => c.ToChannel == outChannel && sources[c.FromTextureIndex].Texture != null).ToArray();
            
            // Set textures
            for(int i = 0; i < chnlConnections.Length; i++)
            {
                TextureSource s = sources[chnlConnections[i].FromTextureIndex];
                // set the sampler states correctly
                s.UncompressedTexture.wrapMode = repeatMode ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                s.UncompressedTexture.filterMode = s.FilterMode;
                ComputeShader.SetTexture(0, outChannel.ToString() + "_Input_" + i, s.UncompressedTexture);
                ComputeShader.SetInt(outChannel.ToString() + "_Channel_" + i, (int)chnlConnections[i].FromChannel);
            }
            for(int i = chnlConnections.Length; i < 4; i++)
            {
                ComputeShader.SetTexture(0, outChannel.ToString() + "_Input_" + i, Texture2D.whiteTexture);
            }

            // Set other data
            ComputeShader.SetInt(outChannel.ToString() + "_Count", chnlConnections.Length);
            ComputeShader.SetInt(outChannel.ToString() + "_BlendMode", (int)config.BlendMode);
            ComputeShader.SetBool(outChannel.ToString() + "_Invert", config.Invert == InvertMode.Invert);
            ComputeShader.SetFloat(outChannel.ToString() + "_Fallback", config.Fallback);

            return chnlConnections.Length;
        }

        void Save()
        {
            if (_outputTexture == null) return;
            string path = _saveFolder + "/" + _saveName + GetTypeEnding(_saveType);
            byte[] bytes = null;
            if(File.Exists(path))
            {
                // open dialog
                if (!EditorUtility.DisplayDialog("File already exists", "Do you want to overwrite the file?", "Yes", "No"))
                {
                    return;
                }
            }
            switch (_saveType)
            {
                case SaveType.PNG: bytes = _outputTexture.EncodeToPNG(); break;
                case SaveType.JPG: bytes = _outputTexture.EncodeToJPG((int)_saveQuality); break;
            }
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.streamingMipmaps = true;
            importer.crunchedCompression = true;
            importer.sRGBTexture = _colorSpace == ColorSpace.Gamma;
            importer.filterMode = _filterMode;
            importer.alphaIsTransparency = _outputTexture.alphaIsTransparency;
            importer.SaveAndReimport();

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(OnSave != null) OnSave(tex);
        }
    }
}