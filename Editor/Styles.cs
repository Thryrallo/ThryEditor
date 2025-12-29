// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using UnityEditor;
using UnityEngine;

namespace Thry.ThryEditor
{
	public class Styles
	{
		private static GUIStyle _masterLabel;
		public static GUIStyle masterLabel
		{
			get
			{
				if (_masterLabel == null)
					_masterLabel = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter };
				return _masterLabel;
			}
		}

		private static GUIStyle _editorHeaderLabel;
		public static GUIStyle editorHeaderLabel
		{
			get
			{
				if (_editorHeaderLabel == null)
					_editorHeaderLabel = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
				return _editorHeaderLabel;
			}
		}

		private static GUIStyle _dropdownHeader;
		public static GUIStyle dropdownHeader
		{
			get
			{
				if (_dropdownHeader == null)
				{
					_dropdownHeader = new GUIStyle(new GUIStyle("ShurikenModuleTitle"))
					{
						font = new GUIStyle(EditorStyles.label).font,
						fontSize = GUI.skin.font.fontSize,
						border = new RectOffset(15, 7, 4, 4),
						fixedHeight = 22,
						contentOffset = new Vector2(20f, -2f)
					};
				}
				return _dropdownHeader;
			}
		}

		private static GUIStyle _flatHeader;
		public static GUIStyle flatHeader
		{
			get
			{
				if (_flatHeader == null)
				{
					_flatHeader = new GUIStyle(GUI.skin.button)
					{
						font = EditorStyles.label.font,
						fontSize = 12,
						alignment = TextAnchor.MiddleLeft,
						padding = new RectOffset(20, 4, 2, 2),
						fixedHeight = 21
					};
				}
				return _flatHeader;
			}
		}

		private static GUIStyle _animatedIndicatorStyle;
		public static GUIStyle animatedIndicatorStyle
		{
			get
			{
				if (_animatedIndicatorStyle == null)
				{
					_animatedIndicatorStyle = new GUIStyle()
					{
						normal = new GUIStyleState() { textColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 1f, 0.3f) : new Color(0f, 0.5f, 0f) },
						alignment = TextAnchor.MiddleRight
					};
				}
				return _animatedIndicatorStyle;
			}
		}

		private static GUIStyle _presetIndicatorStyle;
		public static GUIStyle presetIndicatorStyle
		{
			get
			{
				if (_presetIndicatorStyle == null)
				{
					_presetIndicatorStyle = new GUIStyle()
					{
						normal = new GUIStyleState() { textColor = EditorGUIUtility.isProSkin ? new Color(0f, 1f, 1f) : new Color(0f, 0.5f, 0.71f) },
						alignment = TextAnchor.MiddleRight
					};
				}
				return _presetIndicatorStyle;
			}
		}

		private static GUIStyle _madeByLabel;
		public static GUIStyle madeByLabel
		{
			get
			{
				if (_madeByLabel == null)
					_madeByLabel = new GUIStyle(EditorStyles.label) { fontSize = 10 };
				return _madeByLabel;
			}
		}

		private static GUIStyle _notification;
		public static GUIStyle notification
		{
			get
			{
				if (_notification == null)
					_notification = new GUIStyle(GUI.skin.box) { fontSize = 12, wordWrap = true, normal = new GUIStyleState() { textColor = Color.red } };
				return _notification;
			}
		}

		private static GUIStyle _label_property_note;
		public static GUIStyle label_property_note
		{
			get
			{
				if (_label_property_note == null)
				{
					_label_property_note = new GUIStyle(EditorStyles.label)
					{
						alignment = TextAnchor.MiddleRight,
						padding = new RectOffset(0, 0, 0, 4),
						normal = new GUIStyleState { textColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.6f) : new Color(0f, 0f, 0f, 1f) }
					};
				}
				return _label_property_note;
			}
		}

		public static readonly GUIStyle vectorPropertyStyle = new GUIStyle() { padding = new RectOffset(0, 0, 2, 2) };
		public static readonly GUIStyle orangeStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = new Color(0.9f, 0.5f, 0) } };
		public static readonly GUIStyle cyanStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.cyan } };
		public static readonly GUIStyle redStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } };
		public static readonly GUIStyle greenStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = new Color(0, 0.5f, 0) } };

		private static GUIStyle _upperRight;
		public static GUIStyle upperRight
		{
			get
			{
				if (_upperRight == null)
					_upperRight = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.UpperRight };
				return _upperRight;
			}
		}

		private static GUIStyle _upperLeft_richText;
		public static GUIStyle upperLeft_richText
		{
			get
			{
				if (_upperLeft_richText == null)
					_upperLeft_richText = new GUIStyle(EditorStyles.label) { richText = true };
				return _upperLeft_richText;
			}
		}

		private static GUIStyle _upperLeft_richText_wordWrap;
		public static GUIStyle upperLeft_richText_wordWrap
		{
			get
			{
				if (_upperLeft_richText_wordWrap == null)
					_upperLeft_richText_wordWrap = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
				return _upperLeft_richText_wordWrap;
			}
		}

		private static GUIStyle _middleCenter_richText_wordWrap;
		public static GUIStyle middleCenter_richText_wordWrap
		{
			get
			{
				if (_middleCenter_richText_wordWrap == null)
					_middleCenter_richText_wordWrap = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true, alignment = TextAnchor.MiddleCenter };
				return _middleCenter_richText_wordWrap;
			}
		}

		public static readonly GUIStyle padding2pxHorizontal1pxVertical = new GUIStyle() { padding = new RectOffset(2, 2, 1, 1) };

		// Variant Stuff
		public static readonly GUIContent revertContent = EditorGUIUtility.TrTextContent("Revert");
		public static readonly GUIContent revertAllContent = EditorGUIUtility.TrTextContent("Revert all Overrides");
		public static readonly GUIContent lockContent = EditorGUIUtility.TrTextContent("Lock in children");
		public static readonly GUIContent lockOriginContent = EditorGUIUtility.TrTextContent("See lock origin");
		public static string revertMultiText = L10n.Tr("Revert on {0} Material(s)");
		public static string applyToMaterialText = L10n.Tr("Apply to Material '{0}'");
		public static string applyToVariantText = L10n.Tr("Apply as Override in Variant '{0}'");
		public static readonly GUIContent resetContent = EditorGUIUtility.TrTextContent("Reset");
	}

	public class Colors
	{
		public static readonly Color foreground = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : Color.black;
		public static readonly Color backgroundDark = EditorGUIUtility.isProSkin ? new Color(0.27f, 0.27f, 0.27f) : new Color(0.65f, 0.65f, 0.65f);
		public static readonly Color backgroundLight = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.85f, 0.85f, 0.85f);

		// Drawer colors - centralized for consistency
		public static readonly Color graphBackground = new Color(0.16f, 0.16f, 0.16f, 1f);
		public static readonly Color graphGutter = new Color(0.1f, 0.1f, 0.1f, 1f);
		public static readonly Color handleOrange = new Color(1f, 0.6f, 0.2f, 1f);
		public static readonly Color handleBlue = new Color(0.4f, 0.8f, 1f, 1f);
		public static readonly Color borderDark = new Color(0, 0, 0, 0.6f);
		public static readonly Color referenceLine = new Color(0.3f, 0.3f, 0.3f, 0.5f);
	}

	public class Icons
	{
		public static readonly GUIStyle help = CreateIconStyle(EditorGUIUtility.IconContent("_Help@2x"));
		public static readonly GUIStyle menu = CreateIconStyle(EditorGUIUtility.IconContent("_Menu"));
		public static readonly GUIStyle settings = CreateIconStyle(EditorGUIUtility.IconContent("_Popup@2x"));
		public static readonly GUIStyle search = CreateIconStyle(EditorGUIUtility.IconContent("Search Icon"));
		public static readonly GUIStyle presets = CreateIconStyle(EditorGUIUtility.IconContent("Preset.Context"));
		public static readonly GUIStyle add = CreateIconStyle(EditorGUIUtility.IconContent("PrefabOverlayAdded Icon"));
		public static readonly GUIStyle remove = CreateIconStyle(EditorGUIUtility.IconContent("PrefabOverlayRemoved Icon"));
		public static readonly GUIStyle refresh = CreateIconStyle(EditorGUIUtility.IconContent("d_Refresh"));
		public static readonly GUIStyle shaders = CreateIconStyle(EditorGUIUtility.IconContent("d_ShaderVariantCollection Icon"));
		public static readonly GUIStyle tools = CreateIconStyle(EditorGUIUtility.IconContent("d_SceneViewTools@2x"));
		public static readonly GUIStyle linked = CreateIconStyle(LoadTextureByGUID(RESOURCE_GUID.ICON_LINK));
		public static readonly GUIStyle thryIcon = CreateIconStyle(LoadTextureByGUID(RESOURCE_GUID.ICON_THRY));
		public static readonly GUIStyle github = CreateIconStyle(LoadTextureByGUID(RESOURCE_GUID.ICON_GITHUB));

		static GUIStyle CreateIconStyle(GUIContent content)
		{
			return CreateIconStyle(content.image as Texture2D);
		}

		static GUIStyle CreateIconStyle(Texture2D texture)
		{
			return new GUIStyle()
			{
				stretchWidth = true,
				stretchHeight = true,
				fixedHeight = 0,
				fixedWidth = 0,
				normal = new GUIStyleState()
				{
					background = texture
				}
			};
		}

		private static Texture2D LoadTextureByGUID(string guid)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path == null) return Texture2D.whiteTexture;
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
	}
}
