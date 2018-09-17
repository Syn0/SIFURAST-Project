// Amplify Impostors
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

//#define AI_DEBUG_MODE

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AmplifyImpostors
{
	[CustomEditor( typeof( AmplifyImpostor ) )]
	public sealed class AmplifyImpostorInspector : Editor
	{
		bool m_billboardMesh = false;
#if AI_DEBUG_MODE
		bool m_debugOptions = false;
#endif
		GUIStyle m_foldout;

		AmplifyImpostor m_instance;

		SerializedProperty m_renderers;

		static GUIContent LockIconOpen = null;
		static GUIContent LockIconClosed = null;

		static GUIContent TextureIcon = null;
		static GUIContent CreateIcon = null;
		static GUIContent SettingsIcon = null;

		private readonly int[] m_sizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
		private readonly GUIContent[] m_sizesStr = { new GUIContent( "32" ), new GUIContent( "64" ), new GUIContent( "128" ), new GUIContent( "256" ), new GUIContent( "512" ), new GUIContent( "1024" ), new GUIContent( "2048" ), new GUIContent( "4096" ), new GUIContent( "8192" ) };

		private Mesh m_previewMesh;
		private int m_activeHandle = -1;
		private Vector2 m_lastMousePos;
		private Vector2 m_originalPos;
		private int m_lastPointSelected = -1;
		private bool m_recalculateMesh = false;

		private bool m_recalculatePreviewTexture = false;

		private Material m_alphaMaterial;
		private const string AlphaGUID = "31bd3cd74692f384a916d9d7ea87710d";

		private static readonly GUIContent AssetFieldStr = new GUIContent( "Impostor Asset", "Asset that will hold most of the impostor data" );
		private static readonly GUIContent LODGroupStr = new GUIContent( "LOD Group", "If it exists this allows to automatically setup the impostor in the given LOD Group" );
		private static readonly GUIContent RenderersStr = new GUIContent( "References", "References to the renderers that will be used to bake the impostor" );
		private static readonly GUIContent BakeTypeStr = new GUIContent( "Bake Type", "Technique used for both baking and rendering the impostor" );
		private static readonly GUIContent TextureSizeStr = new GUIContent( "Texture Size", "The texture size in pixels for the final baked images. Higher resolution images provides better results at closer ranges, but are heavier in both storage and runtime." );
		private static readonly GUIContent AxisFramesStr = new GUIContent( "Axis Frames", "The amount frames per axis" );
		private static readonly GUIContent PixelPaddingStr = new GUIContent( "Pixel Padding", "Padding size in pixels. Padding expands the edge pixels of the individual shots to avoid rendering artifacts caused by mipmapping." );
		private static readonly GUIContent RenderingMapsStr = new GUIContent( "Rendering Maps", "Allows to individually turn on/off the rendering of the baked maps" );
		private static readonly GUIContent LODModeStr = new GUIContent( "LOD Insert Mode", "A rule of how the impostor will be automatically included in the LOD Group" );
		private static readonly GUIContent LODTargetIndexStr = new GUIContent( "LOD Target Index", "Target index for the current insert mode" );
		private static readonly GUIContent NormalCompressionStr = new GUIContent( "Normal Compression", "Changes normals quality compression in the texture importer. Higher quality takes longer to import." );
		private static readonly GUIContent MaxVerticesStr = new GUIContent( "Max Vertices", "Maximum number of vertices that ensures the final created amount does not exceed it" );
		private static readonly GUIContent OutlineToleranceStr = new GUIContent( "Outline Tolerance", "Allows the final shape to more tightly fit the object by increasing its number or vertices" );
		private static readonly GUIContent NormalScaleStr = new GUIContent( "Normal Scale", "Scales the vertices out according to the shape normals" );


		private Color m_flashColor = new Color( 0.35f, 0.55f, 1f );
		private Color m_flash = Color.white;
		private AmplifyImpostorAsset m_currentData;

		private double lastTime;

		private void OnEnable()
		{
			m_instance = ( target as AmplifyImpostor );
			if( m_instance.Data == null )
			{
				m_currentData = ScriptableObject.CreateInstance<AmplifyImpostorAsset>();
				m_currentData.ImpostorType = (ImpostorType)Enum.Parse( typeof( ImpostorType ), EditorPrefs.GetString( ImpostorBakingTools.PrefDataImpType, m_currentData.ImpostorType.ToString() ) );
				m_currentData.SelectedSize = EditorPrefs.GetInt( ImpostorBakingTools.PrefDataTexSizeSelected, m_currentData.SelectedSize );
				m_currentData.LockedSizes = EditorPrefs.GetBool( ImpostorBakingTools.PrefDataTexSizeLocked, m_currentData.LockedSizes );
				m_currentData.TexSize.x = EditorPrefs.GetFloat( ImpostorBakingTools.PrefDataTexSizeX, m_currentData.TexSize.x );
				m_currentData.TexSize.y = EditorPrefs.GetFloat( ImpostorBakingTools.PrefDataTexSizeY, m_currentData.TexSize.y );
				m_currentData.DecoupleAxisFrames = EditorPrefs.GetBool( ImpostorBakingTools.PrefDataDecoupledFrames, m_currentData.DecoupleAxisFrames );
				m_currentData.HorizontalFrames = EditorPrefs.GetInt( ImpostorBakingTools.PrefDataXFrames, m_currentData.HorizontalFrames );
				m_currentData.VerticalFrames = EditorPrefs.GetInt( ImpostorBakingTools.PrefDataYFrames, m_currentData.VerticalFrames );
				m_currentData.PixelPadding = EditorPrefs.GetInt( ImpostorBakingTools.PrefDataPixelBleeding, m_currentData.PixelPadding );

				m_currentData.Tolerance = EditorPrefs.GetFloat( ImpostorBakingTools.PrefDataTolerance, m_currentData.Tolerance );
				m_currentData.NormalScale = EditorPrefs.GetFloat( ImpostorBakingTools.PrefDataNormalScale, m_currentData.NormalScale );
				m_currentData.MaxVertices = EditorPrefs.GetInt( ImpostorBakingTools.PrefDataMaxVertices, m_currentData.MaxVertices );
			}
			else
			{
				m_currentData = m_instance.Data;
			}

			ImpostorBakingTools.LoadDefaults();

			Shader alphaShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( AlphaGUID ) );
			m_alphaMaterial = new Material( alphaShader );

			if( m_instance.m_cutMode == CutMode.Automatic )
			{
				m_recalculateMesh = true;
			}

			if( m_instance.RootTransform == null )
				m_instance.RootTransform = m_instance.transform;

			// should we skip some renderers here?
			if( m_instance.LodGroup == null )
			{
				m_instance.LodGroup = m_instance.GetComponent<LODGroup>();
				if( ( m_instance.Renderers == null || m_instance.Renderers.Length == 0 ) && m_instance.RootTransform != null )
				{
					if( m_instance.LodGroup != null )
					{
						LOD[] lods = m_instance.LodGroup.GetLODs();

						// is last lod a billboard?
						int vertexCount = 0;
						Renderer[] rend = lods[ lods.Length - 1 ].renderers;

						for( int i = 0; i < rend.Length; i++ )
						{
							MeshFilter mf = rend[ i ].GetComponent<MeshFilter>();
							if( mf != null )
								vertexCount += mf.sharedMesh.vertexCount;
						}

						int lastIndex = lods.Length - 1;

						if( vertexCount < 8 )
						{
							lastIndex--;
						}

						for( int i = lastIndex - 1; i >= 0; i-- )
						{
							if( lods[ i ].renderers != null && lods[ i ].renderers.Length > 0 )
							{
								m_instance.Renderers = lods[ i ].renderers;
								break;
							}
						}
						m_instance.m_insertIndex = lastIndex;
						if( vertexCount < 8 )
							m_instance.m_lodReplacement = LODReplacement.ReplaceLast;
					}
					else
					{
						m_instance.Renderers = m_instance.RootTransform.GetComponentsInChildren<Renderer>();
					}
				}
			}

			m_renderers = serializedObject.FindProperty( "m_renderers" );
		}

		public void ReCheckRenderers()
		{
			LOD[] lods = m_instance.LodGroup.GetLODs();
			int lastIndex = lods.Length - 1;

			switch( m_instance.m_lodReplacement )
			{
				default:
				case LODReplacement.DoNothing:
				{
					m_flash = Color.white;
				}
				return;
				//break;
				case LODReplacement.ReplaceCulled:
				{
					m_instance.m_insertIndex = lastIndex;
				}
				break;
				case LODReplacement.ReplaceLast:
				{
					lastIndex = lods.Length - 2;
					m_instance.m_insertIndex = lastIndex + 1;
				}
				break;
				case LODReplacement.ReplaceAllExceptFirst:
				{
					lastIndex = 0;
					m_instance.m_insertIndex = lastIndex;
				}
				break;
				case LODReplacement.ReplaceSpecific:
				{
					lastIndex = m_instance.m_insertIndex - 1;
					m_instance.m_insertIndex = lastIndex + 1;
				}
				break;
				case LODReplacement.InsertAfter:
				case LODReplacement.ReplaceAfterSpecific:
				{
					lastIndex = m_instance.m_insertIndex;
					m_instance.m_insertIndex = lastIndex;
				}
				break;
			}

			for( int i = lastIndex; i >= 0; i-- )
			{
				if( lods[ i ].renderers != null && lods[ i ].renderers.Length > 0 )
				{
					m_instance.Renderers = lods[ i ].renderers;
					break;
				}
			}

			m_flash = m_flashColor;
		}

		private void OnDisable()
		{
			DestroyImmediate( m_alphaMaterial );
			m_alphaMaterial = null;

			if( m_instance.Data == null && m_currentData != null && !AssetDatabase.IsMainAsset( m_currentData ) )
				DestroyImmediate( m_currentData );
		}

		override public void OnInspectorGUI()
		{
			//base.OnInspectorGUI();

			if( m_foldout == null )
				m_foldout = "foldout";

			if( LockIconOpen == null )
				LockIconOpen = new GUIContent( EditorGUIUtility.IconContent( "LockIcon-On" ) );

			if( LockIconClosed == null )
				LockIconClosed = new GUIContent( EditorGUIUtility.IconContent( "LockIcon" ) );

			if( TextureIcon == null )
			{
				TextureIcon = new GUIContent( EditorGUIUtility.IconContent( "Texture Icon" ) );
				TextureIcon.text = " Bake Impostor";
			}

			if( CreateIcon == null )
			{
				CreateIcon = new GUIContent( EditorGUIUtility.IconContent( "Toolbar Plus" ) );
				CreateIcon.text = "";
			}

			if( SettingsIcon == null )
				SettingsIcon = new GUIContent( EditorGUIUtility.IconContent( "icons/d_TerrainInspector.TerrainToolSettings.png" ) );

			m_instance = ( target as AmplifyImpostor );

			bool triangulateMesh = false;
			bool autoChangeToManual = false;
			bool bakeTextures = false;

			if( m_instance.LodGroup != null && m_instance.m_lastImpostor == null )
			{
				double deltaTime = Time.realtimeSinceStartup - lastTime;
				lastTime = Time.realtimeSinceStartup;
				m_flash = Color.Lerp( m_flash, Color.white, (float)deltaTime * 3f );
			}
			else
			{
				m_flash = Color.white;
			}
			EditorGUI.BeginChangeCheck();
			m_instance.Data = EditorGUILayout.ObjectField( AssetFieldStr, m_instance.Data, typeof( AmplifyImpostorAsset ), false ) as AmplifyImpostorAsset;
			if( m_instance.Data != null )
				m_currentData = m_instance.Data;

			m_instance.LodGroup = EditorGUILayout.ObjectField( LODGroupStr, m_instance.LodGroup, typeof( LODGroup ), true ) as LODGroup;

			EditorGUILayout.BeginHorizontal();
			Color tempC = GUI.color;
			GUI.color = m_flash;
			EditorGUILayout.PropertyField( m_renderers, RenderersStr, true );
			GUI.color = tempC;
			EditorGUILayout.EndHorizontal();

			GUILayout.Space( 9 );

			EditorGUILayout.BeginHorizontal();
			if( m_instance.Data != null )
			{
				EditorGUI.BeginChangeCheck();
				ImpostorBakingTools.GlobalBakingOptions = GUILayout.Toggle( ImpostorBakingTools.GlobalBakingOptions, SettingsIcon, "buttonleft", GUILayout.Width( 32 ), GUILayout.Height( 24 ) );
				if( EditorGUI.EndChangeCheck() )
				{
					EditorPrefs.SetBool( ImpostorBakingTools.PrefGlobalBakingOptions, ImpostorBakingTools.GlobalBakingOptions );
				}
			}
			else
			{
				if( GUILayout.Button( CreateIcon, "buttonleft", GUILayout.Width( 32 ), GUILayout.Height( 24 ) ) )
				{
					m_instance.CreateAssetFile( m_currentData );
				}
			}

			if( GUILayout.Button( TextureIcon, "buttonright", GUILayout.Height( 24 ) ) )
			{
				if( m_instance.m_alphaTex == null )
					m_recalculatePreviewTexture = true;

				bakeTextures = true;
			}

			EditorGUILayout.EndHorizontal();

			if( !( m_instance.transform.transform.localScale.x == m_instance.transform.transform.localScale.y && m_instance.transform.transform.localScale.y == m_instance.transform.transform.localScale.z ) )
			{
				EditorGUILayout.HelpBox( "Impostors can't render non-uniform scales correctly. Please consider scaling the object uniformly or generate it as a child of one.", MessageType.Warning );
			}

			if( ( m_currentData.HorizontalFrames > 16 || m_currentData.VerticalFrames > 16 ) )
			{
				EditorGUILayout.HelpBox( "Creating impostors with the axis frame number above 16 may take a lot longer to bake and save and the extra frames usually aren't necessary.", MessageType.Info );
			}

			if( ( m_currentData.TexSize.x > 2048 || m_currentData.TexSize.y > 2048 ) )
			{
				EditorGUILayout.HelpBox( "Creating impostors with texture resolution above 2048px may take a while to bake and save.", MessageType.Info );
			}

			if( ImpostorBakingTools.GlobalBakingOptions && m_instance.Data != null )
			{
				EditorGUILayout.BeginVertical( "helpbox" );
				{
					EditorGUI.BeginChangeCheck();
					m_currentData.ImpostorType = (ImpostorType)EditorGUILayout.EnumPopup( BakeTypeStr, m_currentData.ImpostorType );
					if( EditorGUI.EndChangeCheck() )
					{
						m_recalculatePreviewTexture = true;
						m_instance.m_alphaTex = null;
						if( m_instance.m_cutMode == CutMode.Automatic )
							m_recalculateMesh = true;
					}

					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginChangeCheck();
					if( m_currentData.LockedSizes )
					{
						m_currentData.SelectedSize = EditorGUILayout.IntPopup( TextureSizeStr, m_currentData.SelectedSize, m_sizesStr, m_sizes );
						m_currentData.LockedSizes = GUILayout.Toggle( m_currentData.LockedSizes, LockIconOpen, "minibutton", GUILayout.Width( 22 ) );
						m_currentData.TexSize.Set( m_currentData.SelectedSize, m_currentData.SelectedSize );
					}
					else
					{
						m_currentData.TexSize = EditorGUILayout.Vector2Field( TextureSizeStr, m_currentData.TexSize );
						m_currentData.LockedSizes = GUILayout.Toggle( m_currentData.LockedSizes, LockIconClosed, "minibutton", GUILayout.Width( 22 ) );
					}
					if( EditorGUI.EndChangeCheck() )
					{
						m_instance.m_alphaTex = null;
						m_recalculatePreviewTexture = true;
						if( m_instance.m_cutMode == CutMode.Automatic )
							m_recalculateMesh = true;
					}
					EditorGUILayout.EndHorizontal();

					if( !m_currentData.DecoupleAxisFrames || m_currentData.ImpostorType != ImpostorType.Spherical )
					{
						EditorGUILayout.BeginHorizontal();
						m_currentData.HorizontalFrames = EditorGUILayout.IntSlider( AxisFramesStr, m_currentData.HorizontalFrames, 1, 32 );
						m_currentData.VerticalFrames = m_currentData.HorizontalFrames;
						if( m_currentData.ImpostorType == ImpostorType.Spherical )
							m_currentData.DecoupleAxisFrames = !GUILayout.Toggle( !m_currentData.DecoupleAxisFrames, LockIconOpen, "minibutton", GUILayout.Width( 22 ) );
						EditorGUILayout.EndHorizontal();
					}
					else
					{
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField( AxisFramesStr, "" );
						m_currentData.DecoupleAxisFrames = !GUILayout.Toggle( !m_currentData.DecoupleAxisFrames, LockIconClosed, "minibutton", GUILayout.Width( 22 ) );
						EditorGUILayout.EndHorizontal();
						EditorGUI.indentLevel++;
						m_currentData.HorizontalFrames = EditorGUILayout.IntSlider( "X", m_currentData.HorizontalFrames, 1, 32 );
						m_currentData.VerticalFrames = EditorGUILayout.IntSlider( "Y", m_currentData.VerticalFrames, 1, 32 );
						EditorGUI.indentLevel--;
					}
					m_currentData.PixelPadding = EditorGUILayout.IntSlider( PixelPaddingStr, m_currentData.PixelPadding, 0, 64 );
#if UNITY_2017_3_OR_NEWER
					m_currentData.BufferMask = (DeferredBuffers)EditorGUILayout.EnumFlagsField( RenderingMapsStr, m_currentData.BufferMask );
#else
					m_currentData.BufferMask = (DeferredBuffers)EditorGUILayout.EnumMaskField( RenderingMapsStr, m_currentData.BufferMask );
#endif
					m_currentData.NormalCompression = (NormalCompression)EditorGUILayout.EnumPopup( NormalCompressionStr, m_currentData.NormalCompression );
					EditorGUI.BeginDisabledGroup( m_instance.m_lastImpostor != null || m_instance.LodGroup == null );
					EditorGUI.BeginChangeCheck();
					m_instance.m_lodReplacement = (LODReplacement)EditorGUILayout.EnumPopup( LODModeStr, m_instance.m_lodReplacement );
					EditorGUI.BeginDisabledGroup( m_instance.m_lodReplacement < LODReplacement.ReplaceSpecific || m_instance.LodGroup == null );
					{
						int maxLods = 0;
						if( m_instance.LodGroup != null )
							maxLods = m_instance.LodGroup.lodCount - 1;

						m_instance.m_insertIndex = EditorGUILayout.IntSlider( LODTargetIndexStr, m_instance.m_insertIndex, 0, maxLods );
					}
					EditorGUI.EndDisabledGroup();
					if( EditorGUI.EndChangeCheck() )
					{
						ReCheckRenderers();
					}
					EditorGUI.EndDisabledGroup();

					if( ( m_billboardMesh || m_recalculatePreviewTexture ) && m_instance.m_alphaTex == null )
					{
						m_instance.RenderCombinedAlpha( m_currentData );
						RenderTexture.active = m_instance.CombinedAlphaTexture;
						m_instance.m_alphaTex = new Texture2D( m_instance.CombinedAlphaTexture.width, m_instance.CombinedAlphaTexture.height, TextureFormat.RGBAFloat, false );
						m_instance.m_alphaTex.ReadPixels( new Rect( 0, 0, m_instance.CombinedAlphaTexture.width, m_instance.CombinedAlphaTexture.height ), 0, 0 );
						m_instance.m_alphaTex.Apply();
						m_instance.ClearCombinedAlphaBuffer();

						m_recalculatePreviewTexture = false;
					}

					EditorGUI.indentLevel++;
					m_billboardMesh = GUILayout.Toggle( m_billboardMesh, "Billboard Mesh", "foldout" );
					EditorGUI.indentLevel--;
					int cutPreviewSize = 160;
					EditorGUILayout.BeginHorizontal();
					if( m_billboardMesh )
					{
						Rect rect = GUILayoutUtility.GetRect( cutPreviewSize + 10, cutPreviewSize + 10, cutPreviewSize + 10, cutPreviewSize + 10 );
						int controlID = GUIUtility.GetControlID( "miniShapeEditorControl".GetHashCode(), FocusType.Passive, rect );
						Rect texRect = new Rect( 5, 5, cutPreviewSize, cutPreviewSize );
						Rect hotRect = new Rect( 0, 0, cutPreviewSize + 10, cutPreviewSize + 10 );
						GUI.BeginClip( rect );
						if( m_instance.m_alphaTex != null )
							Graphics.DrawTexture( texRect, m_instance.m_alphaTex, m_alphaMaterial, 3 );
						else
							Graphics.DrawTexture( texRect, Texture2D.blackTexture, m_alphaMaterial, 3 );

						switch( Event.current.GetTypeForControl( controlID ) )
						{
							case EventType.MouseDown:
							if( hotRect.Contains( Event.current.mousePosition ) )
							{
								for( int i = 0; i < m_currentData.ShapePoints.Length; i++ )
								{
									Rect handleRect = new Rect( m_currentData.ShapePoints[ i ].x * cutPreviewSize, m_currentData.ShapePoints[ i ].y * cutPreviewSize, 10, 10 );
									if( Event.current.type == EventType.MouseDown && handleRect.Contains( Event.current.mousePosition ) )
									{
										EditorGUI.FocusTextInControl( null );
										m_activeHandle = i;
										m_lastPointSelected = i;
										m_lastMousePos = Event.current.mousePosition;
										m_originalPos = m_currentData.ShapePoints[ i ];
									}
								}

								GUIUtility.hotControl = controlID;
								//Event.current.Use();
							}
							break;
							case EventType.Ignore:
							case EventType.MouseUp:
							if( GUIUtility.hotControl == controlID )
							{
								m_activeHandle = -1;
								triangulateMesh = true;
								GUIUtility.hotControl = 0;
								//Event.current.Use();
								GUI.changed = true;

							}
							break;
							case EventType.MouseDrag:
							if( GUIUtility.hotControl == controlID && m_activeHandle > -1 )
							{
								m_currentData.ShapePoints[ m_activeHandle ] = m_originalPos + ( Event.current.mousePosition - m_lastMousePos ) / ( cutPreviewSize + 10 );
								if( Event.current.modifiers != EventModifiers.Control )
								{
									m_currentData.ShapePoints[ m_activeHandle ].x = (float)Math.Round( m_currentData.ShapePoints[ m_activeHandle ].x, 2 );
									m_currentData.ShapePoints[ m_activeHandle ].y = (float)Math.Round( m_currentData.ShapePoints[ m_activeHandle ].y, 2 );
								}

								m_currentData.ShapePoints[ m_activeHandle ].x = Mathf.Clamp01( m_currentData.ShapePoints[ m_activeHandle ].x );
								m_currentData.ShapePoints[ m_activeHandle ].y = Mathf.Clamp01( m_currentData.ShapePoints[ m_activeHandle ].y );
								autoChangeToManual = true;
								//Event.current.Use();
							}
							break;
						}

						if( Event.current.type == EventType.Repaint )
						{
							Vector3[] allpoints = new Vector3[ m_currentData.ShapePoints.Length + 1 ];
							for( int i = 0; i < m_currentData.ShapePoints.Length; i++ )
							{
								allpoints[ i ] = new Vector3( m_currentData.ShapePoints[ i ].x * cutPreviewSize + 5, m_currentData.ShapePoints[ i ].y * cutPreviewSize + 5, 0 );
							}
							allpoints[ m_currentData.ShapePoints.Length ] = allpoints[ 0 ];

							Dictionary<string, bool> drawnList = new Dictionary<string, bool>();
							for( int i = 0; i < m_currentData.ShapePoints.Length; i++ )
							{
								if( i == m_currentData.ShapePoints.Length - 1 )
									drawnList.Add( ( "0" + i ), true );
								else
									drawnList.Add( ( i + "" + ( i + 1 ) ), true );
							}

							if( m_previewMesh != null && m_instance.m_cutMode == CutMode.Manual )
							{
								//draw inside
								Color cache = Handles.color;
								Handles.color = new Color( 1, 1, 1, 0.5f );
								//Handles.color = Color.black;
								for( int i = 0; i < m_previewMesh.triangles.Length - 1; i += 3 )
								{
									int vert = m_previewMesh.triangles[ i ];
									int vert2 = m_previewMesh.triangles[ i + 1 ];
									int vert3 = m_previewMesh.triangles[ i + 2 ];
									string ab = vert < vert2 ? vert + "" + vert2 : vert2 + "" + vert;
									string bc = vert2 < vert3 ? vert2 + "" + vert3 : vert3 + "" + vert2;
									string ac = vert < vert3 ? vert + "" + vert3 : vert3 + "" + vert;

									Vector3 a = new Vector3( m_currentData.ShapePoints[ vert ].x * cutPreviewSize + 5, m_currentData.ShapePoints[ vert ].y * cutPreviewSize + 5, 0 );
									Vector3 b = new Vector3( m_currentData.ShapePoints[ vert2 ].x * cutPreviewSize + 5, m_currentData.ShapePoints[ vert2 ].y * cutPreviewSize + 5, 0 );
									Vector3 c = new Vector3( m_currentData.ShapePoints[ vert3 ].x * cutPreviewSize + 5, m_currentData.ShapePoints[ vert3 ].y * cutPreviewSize + 5, 0 );
									if( !drawnList.ContainsKey( ab ) )
									{
										Handles.DrawAAPolyLine( new Vector3[] { a, b } );
										drawnList.Add( ab, true );
									}
									if( !drawnList.ContainsKey( bc ) )
									{
										Handles.DrawAAPolyLine( new Vector3[] { b, c } );
										drawnList.Add( bc, true );
									}
									if( !drawnList.ContainsKey( ac ) )
									{
										Handles.DrawAAPolyLine( new Vector3[] { a, c } );
										drawnList.Add( ac, true );
									}
								}
								Handles.color = cache;
							}

							Handles.DrawAAPolyLine( allpoints );

							if( m_instance.m_cutMode == CutMode.Manual )
							{
								for( int i = 0; i < m_currentData.ShapePoints.Length; i++ )
								{
									Rect handleRect = new Rect( m_currentData.ShapePoints[ i ].x * cutPreviewSize + 1, m_currentData.ShapePoints[ i ].y * cutPreviewSize + 1, 8, 8 );
									Handles.DrawSolidRectangleWithOutline( handleRect, ( m_activeHandle == i ? Color.cyan : Color.clear ), ( m_lastPointSelected == i && m_instance.m_cutMode == CutMode.Manual ? Color.cyan : Color.white ) );
								}
							}
							else
							{
								for( int i = 0; i < m_currentData.ShapePoints.Length; i++ )
								{
									Rect handleRect = new Rect( m_currentData.ShapePoints[ i ].x * cutPreviewSize + 3, m_currentData.ShapePoints[ i ].y * cutPreviewSize + 3, 4, 4 );
									Handles.DrawSolidRectangleWithOutline( handleRect, Color.white, Color.white );
								}
							}
						}

						GUI.EndClip();

						EditorGUILayout.BeginVertical();
						{
							EditorGUI.BeginChangeCheck();
							m_instance.m_cutMode = (CutMode)GUILayout.Toolbar( (int)m_instance.m_cutMode, new[] { "Automatic", "Manual" } );
							if( EditorGUI.EndChangeCheck() )
							{
								if( m_instance.m_cutMode == CutMode.Automatic )
									m_recalculateMesh = true;

							}
							float cacheLabel = EditorGUIUtility.labelWidth;
							EditorGUIUtility.labelWidth = 120;

							switch( m_instance.m_cutMode )
							{
								default:
								case CutMode.Automatic:
								{
									EditorGUI.BeginChangeCheck();
									{
										m_currentData.MaxVertices = EditorGUILayout.IntSlider( MaxVerticesStr, m_currentData.MaxVertices, 4, 16 );
										m_currentData.Tolerance = EditorGUILayout.Slider( OutlineToleranceStr, m_currentData.Tolerance * 5, 0, 1f ) * 0.2f;
										m_currentData.NormalScale = EditorGUILayout.Slider( NormalScaleStr, m_currentData.NormalScale, 0, 1.0f );
									}
									if( EditorGUI.EndChangeCheck() )
									{
										m_recalculateMesh = true;
									}
								}
								break;
								case CutMode.Manual:
								{
									m_currentData.MaxVertices = EditorGUILayout.IntSlider( MaxVerticesStr, m_currentData.MaxVertices, 4, 16 );
									m_currentData.Tolerance = EditorGUILayout.Slider( OutlineToleranceStr, m_currentData.Tolerance * 5, 0, 1f ) * 0.2f;
									m_currentData.NormalScale = EditorGUILayout.Slider( NormalScaleStr, m_currentData.NormalScale, 0, 1.0f );
									if( GUILayout.Button( "Update" ) )
										m_recalculateMesh = true;

									m_lastPointSelected = Mathf.Clamp( m_lastPointSelected, 0, m_currentData.ShapePoints.Length - 1 );
									EditorGUILayout.Space();
									m_currentData.ShapePoints[ m_lastPointSelected ] = EditorGUILayout.Vector2Field( "", m_currentData.ShapePoints[ m_lastPointSelected ] );
									m_currentData.ShapePoints[ m_lastPointSelected ].x = Mathf.Clamp01( m_currentData.ShapePoints[ m_lastPointSelected ].x );
									m_currentData.ShapePoints[ m_lastPointSelected ].y = Mathf.Clamp01( m_currentData.ShapePoints[ m_lastPointSelected ].y );
								}
								break;
							}
							EditorGUIUtility.labelWidth = cacheLabel;
						}
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.EndVertical();
			}

			if( m_recalculateMesh && m_instance.m_alphaTex != null )
			{
				m_recalculateMesh = false;

				// create a 2d texture for calculations
				Rect testRect = new Rect( 0, 0, m_instance.m_alphaTex.width, m_instance.m_alphaTex.height );
				Vector2[][] paths;
				SpriteUtilityEx.GenerateOutline( m_instance.m_alphaTex, testRect, m_currentData.Tolerance, 254, false, out paths );
				int sum = 0;
				for( int i = 0; i < paths.Length; i++ )
				{
					sum += paths[ i ].Length;
				}

				m_currentData.ShapePoints = new Vector2[ sum ];
				int index = 0;
				for( int i = 0; i < paths.Length; i++ )
				{
					for( int j = 0; j < paths[ i ].Length; j++ )
					{
						m_currentData.ShapePoints[ index ] = (Vector2)( paths[ i ][ j ] ) + ( new Vector2( m_instance.m_alphaTex.width * 0.5f, m_instance.m_alphaTex.height * 0.5f ) );
						m_currentData.ShapePoints[ index ] = Vector2.Scale( m_currentData.ShapePoints[ index ], new Vector2( 1.0f / m_instance.m_alphaTex.width, 1.0f / m_instance.m_alphaTex.height ) );
						index++;
					}
				}

				// make it convex hull
				m_currentData.ShapePoints = Vector2Ex.ConvexHull( m_currentData.ShapePoints );

				// reduce vertices
				m_currentData.ShapePoints = Vector2Ex.ReduceVertices( m_currentData.ShapePoints, m_currentData.MaxVertices );

				// Resize the mesh using calculated normals
				m_currentData.ShapePoints = Vector2Ex.ScaleAlongNormals( m_currentData.ShapePoints, m_currentData.NormalScale );

				// clamp to box (needs a cut algorithm)
				for( int i = 0; i < m_currentData.ShapePoints.Length; i++ )
				{
					m_currentData.ShapePoints[ i ].x = Mathf.Clamp01( m_currentData.ShapePoints[ i ].x );
					m_currentData.ShapePoints[ i ].y = Mathf.Clamp01( m_currentData.ShapePoints[ i ].y );
				}

				// make it convex hull gain to clean edges
				m_currentData.ShapePoints = Vector2Ex.ConvexHull( m_currentData.ShapePoints );

				// invert Y
				for( int i = 0; i < m_currentData.ShapePoints.Length; i++ )
				{
					m_currentData.ShapePoints[ i ] = new Vector2( m_currentData.ShapePoints[ i ].x, 1 - m_currentData.ShapePoints[ i ].y );
				}

				triangulateMesh = true;

				EditorUtility.SetDirty( m_instance );
			}

			if( EditorGUI.EndChangeCheck() )
			{
				if( m_instance.Data == null )
				{
					EditorPrefs.SetString( ImpostorBakingTools.PrefDataImpType, m_currentData.ImpostorType.ToString() );
					EditorPrefs.SetInt( ImpostorBakingTools.PrefDataTexSizeSelected, m_currentData.SelectedSize );
					EditorPrefs.SetBool( ImpostorBakingTools.PrefDataTexSizeLocked, m_currentData.LockedSizes );
					EditorPrefs.SetFloat( ImpostorBakingTools.PrefDataTexSizeX, m_currentData.TexSize.x );
					EditorPrefs.SetFloat( ImpostorBakingTools.PrefDataTexSizeY, m_currentData.TexSize.y );
					EditorPrefs.SetBool( ImpostorBakingTools.PrefDataDecoupledFrames, m_currentData.DecoupleAxisFrames );
					EditorPrefs.SetInt( ImpostorBakingTools.PrefDataXFrames, m_currentData.HorizontalFrames );
					EditorPrefs.SetInt( ImpostorBakingTools.PrefDataYFrames, m_currentData.VerticalFrames );
					EditorPrefs.SetInt( ImpostorBakingTools.PrefDataPixelBleeding, m_currentData.PixelPadding );

					EditorPrefs.SetFloat( ImpostorBakingTools.PrefDataTolerance, m_currentData.Tolerance );
					EditorPrefs.SetFloat( ImpostorBakingTools.PrefDataNormalScale, m_currentData.NormalScale );
					EditorPrefs.SetInt( ImpostorBakingTools.PrefDataMaxVertices, m_currentData.MaxVertices );
				}

				EditorUtility.SetDirty( m_instance );
			}

			if( triangulateMesh )
				m_previewMesh = GeneratePreviewMesh( m_currentData.ShapePoints, true );

			if( autoChangeToManual /*&& Event.current.type == EventType.Layout*/ )
			{
				autoChangeToManual = false;
				m_instance.m_cutMode = CutMode.Manual;
				Event.current.Use();
			}

			// Bake textures after alpha generation
			if( bakeTextures )
			{
				bakeTextures = false;
				m_instance.RenderAllDeferredGroups( m_currentData );

				bool createLodGroup = false;
				if( ImpostorBakingTools.GlobalCreateLodGroup )
				{
					LODGroup group = m_instance.RootTransform.GetComponentInParent<LODGroup>();
					if( group == null )
						group = m_instance.RootTransform.GetComponentInChildren<LODGroup>();
					if( group == null && m_instance.LodGroup == null )
						createLodGroup = true;
				}

				if( createLodGroup && m_instance.m_lastImpostor != null )
				{
					GameObject lodgo = new GameObject( m_instance.name + "_LODGroup" );
					LODGroup lodGroup = lodgo.AddComponent<LODGroup>();
					lodGroup.transform.position = m_instance.transform.position;
					int hierIndex = m_instance.transform.GetSiblingIndex();

					m_instance.transform.SetParent( lodGroup.transform, true );
					m_instance.m_lastImpostor.transform.SetParent( lodGroup.transform, true );
					LOD[] lods = lodGroup.GetLODs();
					ArrayUtility.RemoveAt<LOD>( ref lods, 2 );
					lods[ 0 ].fadeTransitionWidth = 0.5f;
					lods[ 0 ].screenRelativeTransitionHeight = 0.15f;
					lods[ 0 ].renderers = m_instance.RootTransform.GetComponentsInChildren<Renderer>();
					lods[ 1 ].fadeTransitionWidth = 0.5f;
					lods[ 1 ].screenRelativeTransitionHeight = 0.01f;
					lods[ 1 ].renderers = m_instance.m_lastImpostor.GetComponentsInChildren<Renderer>();
					lodGroup.fadeMode = LODFadeMode.CrossFade;
					lodGroup.animateCrossFading = true;
					lodGroup.SetLODs( lods );
					lodgo.transform.SetSiblingIndex( hierIndex );
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		public override bool RequiresConstantRepaint()
		{
			return m_billboardMesh || ImpostorBakingTools.GlobalBakingOptions;
		}

		public Mesh GeneratePreviewMesh( Vector2[] points, bool invertY = true )
		{
			Triangulator tr = new Triangulator( points, invertY );
			int[] indices = tr.Triangulate();

			Vector3[] vertices = new Vector3[ tr.Points.Count ];
			for( int i = 0; i < vertices.Length; i++ )
			{
				vertices[ i ] = new Vector3( tr.Points[ i ].x, tr.Points[ i ].y, 0 );
			}

			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.uv = points;
			mesh.triangles = indices;

			return mesh;
		}
	}
}
