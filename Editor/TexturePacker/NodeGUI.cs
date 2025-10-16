using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Thry.ThryEditor.Helpers;
using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor.TexturePacker
{
    public class NodeGUI : EditorWindow
    {
        const int MIN_WIDTH = 850;
        const int MIN_HEIGHT = 790;
        Vector2 _scrollPosition = Vector2.zero;

        const string CHANNEL_PREVIEW_SHADER = "Hidden/Thry/ChannelPreview";

        TexturePackerConfig _config;
        Dictionary<Connection, ConnectionBezierPoints> _connectionPoints = new Dictionary<Connection, ConnectionBezierPoints>();
        bool _kernelEditHorizontal = true;
        Connection? _creatingConnection;
        Texture2D _outputTexture;
        bool _showTransparency = true;

        bool[] _channel_export = new bool[4] { true, true, true, true };

        public Action<Texture2D> OnSave;
        public Action<Texture2D, TexturePackerConfig> OnChange;

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

        Vector2[] _positionsChannelIn = new Vector2[20];
        Vector2[] _positionsChannelOut = new Vector2[4];
        Rect[] _rectsChannelIn = new Rect[20];
        Rect[] _rectsChannelOut = new Rect[4];

        [MenuItem("Assets/Thry/Textures/Open in Texture Packer")]
        public static void OpenTexturePackerWithOneTexture()
        {
            Open(Selection.activeObject as Texture2D);
        }

        [MenuItem("Assets/Thry/Textures/Open in Texture Packer", true)]
        public static bool OpenTexturePackerWithOneTextureValidate()
        {
            return Selection.activeObject is Texture2D;
        }

        public static NodeGUI Open()
        {
            return ShowWindow().InitilizeWithData(TexturePackerConfig.GetNewConfig());
        }

        public static NodeGUI Open(Texture2D tex)
        {
            if (TexturePackerConfig.TryGetFromTexture(tex, out TexturePackerConfig config))
            {
                return ShowWindow().InitilizeWithData(config);
            }
            return ShowWindow().InitilizeWithOneTexture(tex);
        }

        public static NodeGUI Open(TexturePackerConfig config)
        {
            return ShowWindow().InitilizeWithData(config);
        }

        NodeGUI InitilizeWithData(TexturePackerConfig config)
        {
            _config = config;
            Packer.DeterminePathAndFileNameIfEmpty(_config, true);
            Packer.DetermineImportSettings(_config);
            Packer.DetermineOutputResolution(_config);
            Pack();
            return this;
        }

        NodeGUI InitilizeWithOneTexture(Texture2D texture)
        {
            _config = TexturePackerConfig.GetNewConfig();
            _config.Sources[0].SetInputTexture(texture);
            _config.Sources[1].SetInputTexture(texture);
            _config.Sources[2].SetInputTexture(texture);
            _config.Sources[3].SetInputTexture(texture);
            // Add connections
            _config.Connections.Add(new Connection(0, TextureChannelIn.R, TextureChannelOut.R));
            _config.Connections.Add(new Connection(1, TextureChannelIn.G, TextureChannelOut.G));
            _config.Connections.Add(new Connection(2, TextureChannelIn.B, TextureChannelOut.B));
            _config.Connections.Add(new Connection(3, TextureChannelIn.A, TextureChannelOut.A));
            // Reset Color Adjust
            _config.ImageAdjust = new ImageAdjust();
            Packer.DeterminePathAndFileNameIfEmpty(_config, true);
            Packer.DetermineImportSettings(_config);
            Packer.DetermineOutputResolution(_config);
            Pack();
            return this;
        }

        static NodeGUI ShowWindow()
        {
            NodeGUI packer = (NodeGUI)GetWindow(typeof(NodeGUI));
            packer.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            packer.titleContent = new GUIContent("Thry Texture Packer");
            packer.OnSave = null; // clear save callback
            packer.OnChange = null; // clear save callback
            return packer;
        }

        const int TOP_OFFSET = 50;
        const int INPUT_PADDING = 20;
        const int OUTPUT_HEIGHT = 300;

        bool _changeCheckForPacking;
        private void OnGUI()
        {
            if (_config == null)
            {
                _config = TexturePackerConfig.GetNewConfig();
            }

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            DrawConfigGUI();
            // Draw three texture slots on the left, a space in the middle, and one texutre slot on the right
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            _changeCheckForPacking = false;

            GUILayout.BeginVertical();
            GUILayout.Space(TOP_OFFSET);
            bool didInputTexturesChange = false;
            didInputTexturesChange |= DrawInput(_config.Sources[0], 0);
            GUILayout.Space(INPUT_PADDING);
            didInputTexturesChange |= DrawInput(_config.Sources[1], 1);
            GUILayout.Space(INPUT_PADDING);
            didInputTexturesChange |= DrawInput(_config.Sources[2], 2);
            GUILayout.Space(INPUT_PADDING);
            didInputTexturesChange |= DrawInput(_config.Sources[3], 3);
            GUILayout.EndVertical();
            float inputHeight = 120 * 4 + INPUT_PADDING * 3 + TOP_OFFSET;

            GUILayout.Space(400);
            Rect rect_outputAndSettings = EditorGUILayout.BeginVertical();
            float output_y_offset = TOP_OFFSET + (inputHeight - TOP_OFFSET - OUTPUT_HEIGHT) / 2;
            GUILayout.Space(output_y_offset);
            DrawOutput(_outputTexture, OUTPUT_HEIGHT);

            EditorGUILayout.Space(15);
            Rect backgroundImageSettings = EditorGUILayout.BeginVertical();
            backgroundImageSettings = new RectOffset(5, 5, 5, 5).Add(backgroundImageSettings);
            GUI.DrawTexture(backgroundImageSettings, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1, Colors.backgroundDark, 0, 10);

            EditorGUI.BeginChangeCheck();
            _config.FileOutput.ColorSpace = (ColorSpace)EditorGUILayout.EnumPopup(_config.FileOutput.ColorSpace);
            _config.FileOutput.FilterMode = (FilterMode)EditorGUILayout.EnumPopup(_config.FileOutput.FilterMode);
            _changeCheckForPacking |= EditorGUI.EndChangeCheck();

            // Make the sliders delayed, else the UX feels terrible
            EditorGUI.BeginChangeCheck();
            EventType eventTypeBeforerSliders = Event.current.type;
            bool wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            _config.FileOutput.Resolution = EditorGUILayout.Vector2IntField("Resolution", _config.FileOutput.Resolution);
            _config.ImageAdjust.Scale = EditorGUILayout.Vector2Field("Scale", _config.ImageAdjust.Scale);
            _config.ImageAdjust.Offset = EditorGUILayout.Vector2Field("Offset", _config.ImageAdjust.Offset);
            _config.ImageAdjust.Rotation = EditorGUILayout.Slider("Rotation", _config.ImageAdjust.Rotation, -180, 180);
            _config.ImageAdjust.Hue = EditorGUILayout.Slider("Hue", _config.ImageAdjust.Hue, 0, 1);
            _config.ImageAdjust.Saturation = EditorGUILayout.Slider("Saturation", _config.ImageAdjust.Saturation, 0, 3);
            _config.ImageAdjust.Brightness = EditorGUILayout.Slider("Brightness", _config.ImageAdjust.Brightness, 0, 3);
            _config.ImageAdjust.ChangeCheck |= EditorGUI.EndChangeCheck();
            EditorGUIUtility.wideMode = wasWide;
            if (_config.ImageAdjust.ChangeCheck && (eventTypeBeforerSliders == EventType.MouseUp || (eventTypeBeforerSliders == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)))
            {
                _changeCheckForPacking = true;
                _config.ImageAdjust.ChangeCheck = false;
            }

            DrawKernelGUI();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawConnections();

            if (didInputTexturesChange)
            {
                Packer.DetermineImportSettings(_config);
            }
            if (_changeCheckForPacking)
            {
                Pack();
                Repaint();
            }

            GUILayout.Space(20);
            DrawSaveGUI();
            GUILayout.EndVertical();

            HandleConnectionEditing();
            HandleConnectionCreation();

            GUILayout.EndScrollView();
        }

        void HandleConnectionEditing()
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                InteractionWithConnection toEdit = CheckIfConnectionClicked(50);
                if (toEdit.ListIndex != -1)
                {
                    _config.Connections.RemoveAt(toEdit.ListIndex);
                    // Remove the connection on one side
                    if (toEdit.DistanceX > 0.5)
                        _creatingConnection = new Connection(toEdit.Data.FromTextureIndex, toEdit.Data.FromChannel);
                    else
                        _creatingConnection = new Connection(-1, TextureChannelIn.None, toEdit.Data.ToChannel);
                    Pack();
                    Repaint();
                }
            }
        }

        void AddNewConnection(Connection connection)
        {
            // check if both channels are set
            if (connection.FromChannel == TextureChannelIn.None || connection.ToChannel == TextureChannelOut.None)
            {
                return;
            }
            // check if already exists
            if (_config.Connections.Exists(c => c.ToChannel == connection.ToChannel && c.FromTextureIndex == connection.FromTextureIndex && c.FromChannel == connection.FromChannel))
            {
                return;
            }
            _config.Connections.Add(connection);
            _changeCheckForPacking = true;
            Pack();
            Repaint();
        }

        void HandleConnectionCreation()
        {
            // Connections are not nullable anymore, since they are serialized
            if (_creatingConnection != null && (_creatingConnection.Value.FromChannel != TextureChannelIn.None || _creatingConnection.Value.ToChannel != TextureChannelOut.None))
            {
                // if user clicked anywhere on the screen, stop creating the connection
                if (Event.current.type == EventType.MouseUp)
                {
                    // Check if mouse position is over any input / output slot
                    Vector2 mousePosition = Event.current.mousePosition;
                    for (int t = 0; t < 4; t++)
                    {
                        for (int c = 0; c < 5; c++)
                        {
                            if (_rectsChannelIn[t * 5 + c].Contains(mousePosition))
                            {
                                AddNewConnection(new Connection(t, (TextureChannelIn)c, _creatingConnection.Value.ToChannel));
                                _creatingConnection = null;
                                return;
                            }
                        }
                    }
                    for (int c = 0; c < 4; c++)
                    {
                        if (_rectsChannelOut[c].Contains(mousePosition))
                        {
                            AddNewConnection(new Connection(_creatingConnection.Value.FromTextureIndex, _creatingConnection.Value.FromChannel, (TextureChannelOut)c));
                            _creatingConnection = null;
                            return;
                        }
                    }
                    _creatingConnection = null;
                    return;
                }

                Vector2 bezierStart, bezierEnd, bezierStartTangent, bezierEndTangent;
                Color color = Color.white;

                bezierEnd = Event.current.mousePosition;

                if (_creatingConnection.Value.FromChannel != TextureChannelIn.None)
                {
                    bezierStart = _positionsChannelIn[_creatingConnection.Value.FromTextureIndex * 5 + (int)_creatingConnection.Value.FromChannel];
                    bezierStartTangent = bezierStart + Vector2.right * 50;
                    bezierEndTangent = bezierEnd + Vector2.left * 50;
                    color = Packer.GetColor(_creatingConnection.Value.FromChannel);
                }
                else
                {
                    bezierStart = _positionsChannelOut[(int)_creatingConnection.Value.ToChannel];
                    bezierStartTangent = bezierStart + Vector2.left * 50;
                    bezierEndTangent = bezierEnd + Vector2.right * 50;
                    color = Packer.GetColor(_creatingConnection.Value.ToChannel);
                }

                Handles.DrawBezier(bezierStart, bezierEnd, bezierStartTangent, bezierEndTangent, color, null, 2);
                Repaint();
            }
        }

        InteractionWithConnection CheckIfConnectionClicked(float maxDistance)
        {
            Vector2 mousePos = Event.current.mousePosition;
            float minDistance = maxDistance;
            InteractionWithConnection clickedConnection = new InteractionWithConnection();
            clickedConnection.ListIndex = -1;
            for (int i = 0; i < _config.Connections.Count; i++)
            {
                Connection c = _config.Connections[i];
                var points = _connectionPoints[c];
                Vector3 from = points.Start;
                Vector3 to = points.End;
                float topY = Mathf.Max(from.y, to.y);
                float bottomY = Mathf.Min(from.y, to.y);
                float leftX = Mathf.Min(from.x, to.x);
                float rightX = Mathf.Max(from.x, to.x);
                // check if mouse is in the area of the bezier curve
                if (mousePos.x > leftX && mousePos.x < rightX)
                {
                    if (mousePos.y > bottomY && mousePos.y < topY)
                    {
                        // check if mouse is close to the bezier curve
                        float distance = HandleUtility.DistancePointBezier(mousePos, points.Start, points.End, points.StartTangent, points.EndTangent);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            clickedConnection.ListIndex = i;
                            clickedConnection.Data = c;
                            clickedConnection.DistanceX = (mousePos.x - leftX) / (rightX - leftX);
                        }
                    }
                }
            }
            return clickedConnection;
        }

        void DrawConfigGUI()
        {
            Rect bg = new Rect(position.width / 2 - 200, 10, 400, 30);
            Rect rObjField = new RectOffset(5, 100, 5, 5).Remove(bg);

            GUI.DrawTexture(bg, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Colors.backgroundDark, 0, 10);
            string[] options = TexturePackerConfig.AllImportersWithConfigs.Select(x => x.assetPath).Prepend("<None>").ToArray();
            int selectedIndex = EditorGUI.Popup(rObjField, "Load previous project", 0, options) - 1;
            if (selectedIndex >= 0 && selectedIndex < options.Length)
            {

                _config = TexturePackerConfig.Deserialize(TexturePackerConfig.AllImportersWithConfigs[selectedIndex].userData);
                // make sure textures exist in project
                foreach (var src in _config.Sources)
                {
                    if (src.InputType == InputType.Texture && src.Texture != null)
                    {
                        string path = AssetDatabase.GetAssetPath(src.Texture);
                        if (string.IsNullOrEmpty(path))
                        {
                            ThryLogger.LogWarn("TexturePacker", $"Removing faulty input texture {src.Texture.name} as it could not be found in the project");
                            src.SetInputTexture(null);
                        }
                        else if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) is Texture2D == false)
                        {
                            ThryLogger.LogWarn("TexturePacker", $"Removing faulty input texture {path} as it is not a Texture2D");
                            src.SetInputTexture(null);
                        }
                    }
                }
                try
                    {
                        Pack();
                    }
                    catch (Exception e)
                    {
                        ThryLogger.LogErr("TexturePacker", $"Could not correctly load config from {options[selectedIndex + 1]}: {e.Message}");
                    }
            }

            Rect rButton = new Rect(rObjField.x + rObjField.width + 5, rObjField.y, 90, rObjField.height);
            if (GUI.Button(rButton, "Clear"))
            {
                _config = TexturePackerConfig.GetNewConfig();
                _outputTexture = null;
            }
        }

        void DrawKernelGUI()
        {
            Rect r_enum = EditorGUILayout.GetControlRect(false, 20);

            EditorGUI.BeginChangeCheck();
            _config.KernelPreset = (KernelPreset)EditorGUI.EnumPopup(r_enum, "Kernel Filter", _config.KernelPreset);
            if (EditorGUI.EndChangeCheck())
            {
                _config.KernelSettings.X = _config.KernelSettings.GetKernel(_config.KernelPreset, true);
                _config.KernelSettings.Y = _config.KernelSettings.GetKernel(_config.KernelPreset, false);
                _config.KernelSettings.LoadPreset(_config.KernelPreset);
                Pack();
                Repaint();
            }

            this.minSize = new Vector2(MIN_WIDTH, _config.KernelPreset == KernelPreset.None ? MIN_HEIGHT : MIN_HEIGHT + 250);

            if (_config.KernelPreset != KernelPreset.None)
            {
                EventType eventTypeBeforerSliders = Event.current.type;
                _config.KernelSettings.Loops = EditorGUILayout.IntSlider("Loops", _config.KernelSettings.Loops, 1, 25);
                _config.KernelSettings.Strength = EditorGUILayout.Slider("Strength", _config.KernelSettings.Strength, 0, 1);
                _config.KernelSettings.TwoPass = EditorGUILayout.Toggle("Two Pass", _config.KernelSettings.TwoPass);
                _config.KernelSettings.GrayScale = EditorGUILayout.Toggle("Gray Scale", _config.KernelSettings.GrayScale);
                Rect r_channels = EditorGUILayout.GetControlRect(false, 20);
                r_channels.width /= 4;
                float prevLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 10;
                _config.KernelSettings.Channels[0] = EditorGUI.Toggle(r_channels, "R", _config.KernelSettings.Channels[0]);
                r_channels.x += r_channels.width;
                _config.KernelSettings.Channels[1] = EditorGUI.Toggle(r_channels, "G", _config.KernelSettings.Channels[1]);
                r_channels.x += r_channels.width;
                _config.KernelSettings.Channels[2] = EditorGUI.Toggle(r_channels, "B", _config.KernelSettings.Channels[2]);
                r_channels.x += r_channels.width;
                _config.KernelSettings.Channels[3] = EditorGUI.Toggle(r_channels, "A", _config.KernelSettings.Channels[3]);
                EditorGUIUtility.labelWidth = prevLabelWidth;
                if (Event.current.type == EventType.Used && (eventTypeBeforerSliders == EventType.MouseUp || (eventTypeBeforerSliders == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)))
                {
                    Pack();
                    Repaint();
                }

                if (_config.KernelSettings.TwoPass)
                {
                    Rect r_buttons = EditorGUILayout.GetControlRect(false, 20);
                    if (GUI.Button(new Rect(r_buttons.x, r_buttons.y, r_buttons.width / 2, r_buttons.height), "X")) _kernelEditHorizontal = true;
                    if (GUI.Button(new Rect(r_buttons.x + r_buttons.width / 2, r_buttons.y, r_buttons.width / 2, r_buttons.height), "Y")) _kernelEditHorizontal = false;
                }

                Rect r = EditorGUILayout.GetControlRect(false, 130);
                EditorGUI.BeginChangeCheck();
                // draw 5x5 matrix inside the r_kernelX rect
                for (int x = 0; x < 5; x++)
                {
                    for (int y = 0; y < 5; y++)
                    {
                        Rect r_cell = new Rect(r.x + x * r.width / 5, r.y + y * r.height / 5, r.width / 5, r.height / 5);
                        if (_kernelEditHorizontal || !_config.KernelSettings.TwoPass) _config.KernelSettings.X[x + y * 5] = EditorGUI.DelayedFloatField(r_cell, _config.KernelSettings.X[x + y * 5]);
                        else _config.KernelSettings.Y[x + y * 5] = EditorGUI.DelayedFloatField(r_cell, _config.KernelSettings.Y[x + y * 5]);
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    _config.KernelPreset = KernelPreset.Custom;
                    Pack();
                    Repaint();
                }
            }
        }

        void DrawSaveGUI()
        {
            // Saving information
            // folder selection
            // determine folder & filename from asset name if not set
            if (string.IsNullOrEmpty(_config.FileOutput.SaveFolder))
            {
                Packer.DeterminePathAndFileNameIfEmpty(_config);
            }

            Rect r = EditorGUILayout.BeginHorizontal();

            Rect background = new Rect(r.x + r.width / 2 - 400, r.y - 5, 800, 97);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Colors.backgroundDark, 0, 10);

            GUILayout.FlexibleSpace();
            // show current path
            GUILayout.Label("Save to: ");
            GUILayout.Label(_config.FileOutput.SaveFolder + "\\");
            _config.FileOutput.FileName = GUILayout.TextField(_config.FileOutput.FileName, GUILayout.MinWidth(50));
            _config.FileOutput.SaveType = (SaveType)EditorGUILayout.EnumPopup(_config.FileOutput.SaveType, GUILayout.Width(70));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _config.FileOutput.AlphaIsTransparency = EditorGUILayout.Toggle("Alpha is Transparency", _config.FileOutput.AlphaIsTransparency);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Change Folder", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("Select folder", _config.FileOutput.SaveFolder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Make path relative to Assets folder
                    path = path.Replace(Application.dataPath, "Assets");
                    _config.FileOutput.SaveFolder = path;
                }
            }
            if (GUILayout.Button("Save", GUILayout.Width(100)))
            {
                Packer.Save(_outputTexture, _config);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _channel_export[0] = GUILayout.Toggle(_channel_export[0], "R", GUILayout.Width(26));
            _channel_export[1] = GUILayout.Toggle(_channel_export[1], "G", GUILayout.Width(26));
            _channel_export[2] = GUILayout.Toggle(_channel_export[2], "B", GUILayout.Width(26));
            _channel_export[3] = GUILayout.Toggle(_channel_export[3], "A", GUILayout.Width(26));
            if (GUILayout.Button("Export Channels", GUILayout.Width(130)))
            {
                ExportChannels(false);
            }
            if (GUILayout.Button("Export Channels (B&W)", GUILayout.Width(150)))
            {
                ExportChannels(true);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DrawConnections()
        {
            // Draw connections as lines
            foreach (Connection c in _config.Connections)
            {
                _connectionPoints[c] = new ConnectionBezierPoints(c, _positionsChannelIn, _positionsChannelOut);
                var points = _connectionPoints[c];
                Handles.DrawBezier(points.Start, points.End, points.StartTangent, points.EndTangent, Packer.GetColor(c.FromChannel), null, 2);
                // Draw remapping input in center of curve
                Vector3 center = (points.Start + points.End) / 2;
                Rect rect = new Rect(center.x - 50, center.y - 20, 150, 40);
                Color backgroundColor = Packer.GetColor(c.FromChannel);
                backgroundColor.a = 0.5f;
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, backgroundColor, 0, 10);

                Rect headerRect = rect;
                headerRect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(headerRect, $"Remap", Styles.editorHeaderLabel);
                Rect note = rect;
                note.y += EditorGUIUtility.singleLineHeight / 2;
                EditorGUI.LabelField(note, $"{c.Remapping.x} - {c.Remapping.y} to {c.Remapping.z} - {c.Remapping.w}");
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
            Rect background = new Rect(buttonR.x + 10, rect.y - 20, (rect.x + rect.width + 5) - (buttonR.x + 10), rect.height + 25);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1, Colors.backgroundDark, 0, 10);

            if (_showTransparency)
                EditorGUI.DrawTextureTransparent(rect, texture != null ? texture : Texture2D.blackTexture, ScaleMode.ScaleToFit, 1);
            else
                EditorGUI.DrawPreviewTexture(rect, texture != null ? texture : Texture2D.blackTexture, null, ScaleMode.ScaleToFit);

            // Show transparency toggle
            Rect rectTransparency = new Rect(rect.x + 8, rect.y - 20, rect.width, 20);
            _showTransparency = EditorGUI.Toggle(rectTransparency, "Show Transparency", _showTransparency);

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
            _rectsChannelOut[0] = buttonR;
            _rectsChannelOut[1] = buttonG;
            _rectsChannelOut[2] = buttonB;
            _rectsChannelOut[3] = buttonA;
            _config.Targets[0] = DrawOutputChannel(buttonR, TextureChannelOut.R, _config.Targets[0]);
            _config.Targets[1] = DrawOutputChannel(buttonG, TextureChannelOut.G, _config.Targets[1]);
            _config.Targets[2] = DrawOutputChannel(buttonB, TextureChannelOut.B, _config.Targets[2]);
            _config.Targets[3] = DrawOutputChannel(buttonA, TextureChannelOut.A, _config.Targets[3]);
        }

        OutputTarget DrawOutputChannel(Rect position, TextureChannelOut channel, OutputTarget config)
        {
            // RGBA on the left side
            // fallback or (blendmode & invert) on the right side
            Rect channelRect = new Rect(position.x, position.y, 20, position.height);
            Rect fallbackRect = new Rect(position.x + 20, position.y, position.width - 20, position.height);
            Rect blendmodeRect = new Rect(fallbackRect.x, fallbackRect.y, fallbackRect.width, fallbackRect.height / 2);
            Rect invertRect = new Rect(fallbackRect.x, fallbackRect.y + fallbackRect.height / 2, fallbackRect.width, fallbackRect.height / 2);

            if (Event.current.type == EventType.MouseDown && channelRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                if (_creatingConnection != null) _creatingConnection = new Connection(_creatingConnection.Value.FromTextureIndex, _creatingConnection.Value.FromChannel, channel);
                else _creatingConnection = new Connection(-1, TextureChannelIn.None, channel);
            }
            GUI.Button(channelRect, channel.ToString());

            float fallback = config.Fallback;
            BlendMode blendmode = config.BlendMode;
            InvertMode invert = config.Invert;

            EditorGUI.BeginChangeCheck();
            if (DoFallback(channel))
            {
                fallback = EditorGUI.FloatField(fallbackRect, fallback);
            }
            else
            {
                blendmode = (BlendMode)EditorGUI.EnumPopup(blendmodeRect, blendmode);
                invert = (InvertMode)EditorGUI.EnumPopup(invertRect, invert);
            }
            _changeCheckForPacking |= EditorGUI.EndChangeCheck();

            return new OutputTarget(blendmode, invert, fallback);
        }

        bool DrawInput(TextureSource texture, int index, int textureHeight = 100)
        {
            int channelWidth = textureHeight / 5;
            Rect rect = GUILayoutUtility.GetRect(textureHeight, textureHeight + 40);
            Rect typeRect = new Rect(rect.x, rect.y, textureHeight, 20);
            Rect textureRect = new Rect(rect.x, rect.y + 20, textureHeight, textureHeight);
            Rect filterRect = new Rect(textureRect.x, textureRect.y + textureHeight, textureRect.width, 20);

            Rect background = new Rect(rect.x - 5, rect.y - 5, rect.width + channelWidth + 40, rect.height + 10);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 1, Colors.backgroundDark, 0, 10);

            // Draw textrue & filtermode. Change filtermode if texture is changed
            EditorGUI.BeginChangeCheck();
            texture.InputType = (InputType)EditorGUI.EnumPopup(typeRect, texture.InputType);
            bool didTextureChange = false;
            switch (texture.InputType)
            {
                case InputType.Texture:
                    EditorGUI.BeginChangeCheck();
                    texture.ImageTexture = (Texture2D)EditorGUI.ObjectField(textureRect, texture.ImageTexture, typeof(Texture2D), false);
                    didTextureChange = EditorGUI.EndChangeCheck();
                    if (didTextureChange && texture.Texture != null) texture.FilterMode = texture.Texture.filterMode;
                    if (didTextureChange) Packer.DetermineOutputResolution(_config);
                    texture.FilterMode = (FilterMode)EditorGUI.EnumPopup(filterRect, texture.FilterMode);
                    break;
                case InputType.Gradient:
                    if (texture.GradientTexture == null) EditorGUI.DrawRect(textureRect, Color.black);
                    else EditorGUI.DrawPreviewTexture(textureRect, texture.GradientTexture);
                    if (Event.current.type == EventType.MouseDown && textureRect.Contains(Event.current.mousePosition))
                    {
                        if (texture.Gradient == null) texture.Gradient = new Gradient();
                        GradientEditor2.Open(texture.Gradient, (Gradient gradient, Texture2D tex) =>
                        {
                            texture.Gradient = gradient;
                            texture.GradientTexture = tex;
                            // Needs to call these itself because it's in a callback not the OnGUI method
                            Pack();
                            Repaint();
                        }, texture.GradientDirection == GradientDirection.Vertical, false, _config.FileOutput.Resolution, new Vector2Int(8192, 8192));

                    }
                    EditorGUI.BeginChangeCheck();
                    texture.GradientDirection = (GradientDirection)EditorGUI.EnumPopup(filterRect, texture.GradientDirection);
                    if (EditorGUI.EndChangeCheck() && texture.Gradient != null)
                    {
                        texture.GradientTexture = Converter.GradientToTexture(texture.Gradient, _config.FileOutput.Resolution.x, _config.FileOutput.Resolution.y, texture.GradientDirection == GradientDirection.Vertical);
                    }
                    break;
                case InputType.Color:
                    EditorGUI.BeginChangeCheck();
                    texture.Color = EditorGUI.ColorField(textureRect, texture.Color);
                    if (EditorGUI.EndChangeCheck())
                    {
                        texture.ColorTexture = Converter.ColorToTexture(texture.Color, 16, 16);
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
            }
            else
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
            _rectsChannelIn[index * 5 + 0] = circleR;
            _rectsChannelIn[index * 5 + 1] = circleG;
            _rectsChannelIn[index * 5 + 2] = circleB;
            _rectsChannelIn[index * 5 + 3] = circleA;
            _rectsChannelIn[index * 5 + 4] = circleMax;
            DrawInputChannel(circleR, index, TextureChannelIn.R);
            DrawInputChannel(circleG, index, TextureChannelIn.G);
            DrawInputChannel(circleB, index, TextureChannelIn.B);
            DrawInputChannel(circleA, index, TextureChannelIn.A);
            DrawInputChannel(circleMax, index, TextureChannelIn.Max);

            return didTextureChange;
        }

        void DrawInputChannel(Rect position, int index, TextureChannelIn channel)
        {
            if (Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                if (_creatingConnection == null) _creatingConnection = new Connection(index, channel, TextureChannelOut.None);
                else _creatingConnection = new Connection(index, channel, _creatingConnection.Value.ToChannel);
            }
            GUI.Button(position, channel.ToString());
        }



        bool DoFallback(TextureChannelOut channel)
        {
            return _config.Connections.Any(c => c.ToChannel == channel && c.FromTextureIndex != -1
                && _config.Sources[c.FromTextureIndex].Texture != null) == false;
        }

        void Pack()
        {
            // SaveConfig();
            // Update all gradient textures (incase max size changed)
            Vector2Int gradientSize = _config.FileOutput.Resolution;
            foreach (TextureSource source in _config.Sources)
            {
                if (source.InputType == InputType.Gradient && source.GradientTexture != null && source.GradientTexture.width != gradientSize.x && source.GradientTexture.height != gradientSize.y)
                {
                    source.GradientTexture = Converter.GradientToTexture(source.Gradient, gradientSize.x, gradientSize.y, source.GradientDirection == GradientDirection.Vertical);
                }
            }

            _outputTexture = Packer.Pack(_config);
            if (OnChange != null) OnChange(_outputTexture, _config);
        }

        void ExportChannels(bool exportAsBlackAndWhite)
        {
            Pack();
            Packer.DeterminePathAndFileNameIfEmpty(_config);
            Packer.ExportChannels(_outputTexture, _config, _channel_export, exportAsBlackAndWhite);
        }
    }
}