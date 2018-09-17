// Amplify Impostors
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

//#define AI_DEBUG_MODE

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AmplifyImpostors
{
	public enum LODReplacement
	{
		DoNothing = 0,
		ReplaceCulled = 1,
		ReplaceLast = 2,
		ReplaceAllExceptFirst = 3,
		ReplaceSpecific = 4,
		ReplaceAfterSpecific = 5,
		InsertAfter = 6
	}

	public enum CutMode
	{
		Automatic = 0,
		Manual = 1
	}

	public enum FolderMode
	{
		RelativeToPrefab = 0,
		Global = 1
	}
#if UNITY_EDITOR
	[Serializable]
	public class DataHolder
	{
		public bool SRGB = true;
		public bool Normal = false;
		public DataHolder()	{}

		public DataHolder(bool sRGB, bool normal)
		{
			SRGB = sRGB;
			Normal = normal;
		}
	}

	public class AmplifyTextureImporter : AssetPostprocessor
	{
		public static bool Activated = false;
		public static int MaxSize = -1;
		public static NormalCompression Compression = NormalCompression.NormalQuality;
		public static Dictionary<string, DataHolder> ImportData = new Dictionary<string, DataHolder>();
		void OnPreprocessTexture()
		{
			if( Activated )
			{
				DataHolder data = new DataHolder();
				if( ImportData.TryGetValue(assetPath, out data )){
					TextureImporter textureImporter = (TextureImporter)assetImporter;
					textureImporter.sRGBTexture = data.SRGB;
					if( data.Normal )
					{
						switch( Compression )
						{
							case NormalCompression.None:
							textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
							break;
							case NormalCompression.LowQuality:
							textureImporter.textureCompression = TextureImporterCompression.CompressedLQ;
							break;
							default:
							case NormalCompression.NormalQuality:
							textureImporter.textureCompression = TextureImporterCompression.Compressed;
							break;
							case NormalCompression.HighQuality:
							textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
							break;
						}
					}
					if( MaxSize > -1 )
						textureImporter.maxTextureSize = MaxSize;
				}
			}
		}
	}
#endif
	public class AmplifyImpostor : MonoBehaviour
	{
		private const string ShaderGUID = "e82933f4c0eb9ba42aab0739f48efe21";
		private const string DilateGUID = "57c23892d43bc9f458360024c5985405";
		private const string PackerGUID = "31bd3cd74692f384a916d9d7ea87710d";
		private const string ShaderOctaGUID = "572f9be5706148142b8da6e9de53acdb";

		[SerializeField]
		private AmplifyImpostorAsset m_data;
		public AmplifyImpostorAsset Data { get { return m_data; } set { m_data = value; } }

		[SerializeField]
		private Transform m_rootTransform;
		public Transform RootTransform { get { return m_rootTransform; } set { m_rootTransform = value; } }

		[SerializeField]
		private LODGroup m_lodGroup;
		public LODGroup LodGroup { get { return m_lodGroup; } set { m_lodGroup = value; } }

		[SerializeField]
		private Renderer[] m_renderers;
		public Renderer[] Renderers { get { return m_renderers; } set { m_renderers = value; } }

		public LODReplacement m_lodReplacement = LODReplacement.ReplaceLast;

		public int m_insertIndex = 1;

		[SerializeField]
		public GameObject m_lastImpostor;

		[SerializeField]
		public string m_folderPath;

		[SerializeField]
		public string m_impostorName = string.Empty;

		[SerializeField]
		public CutMode m_cutMode = CutMode.Automatic;

		[NonSerialized]
		private const float StartXRotation = -90;
		[NonSerialized]
		private const float StartYRotation = 90;
		[NonSerialized]
		private const int MinAlphaResolution = 256;
		[NonSerialized]
		private RenderTexture[] m_rtGBuffers;
		[NonSerialized]
		private RenderTexture[] m_alphaGBuffers;
		[NonSerialized]
		private RenderTexture m_trueDepth;
		[NonSerialized]
		public Texture2D m_alphaTex;
		[NonSerialized]
		private RenderTexture m_combinedAlphaTexture;
		public RenderTexture CombinedAlphaTexture { get { return m_combinedAlphaTexture; } set { m_combinedAlphaTexture = value; } }
		[NonSerialized]
		private float m_trueFitsize = 0;
		[NonSerialized]
		private float m_depthFitsize = 0;
		[NonSerialized]
		private Bounds m_originalBound = new Bounds();


#if AI_DEBUG_MODE
		[SerializeField]
		private string m_renderInfo = string.Empty;
		public string RenderInfo { get { return m_renderInfo; } set { m_renderInfo = value; } }
		public bool m_createGameobject = true;
		public bool m_generateQuad = true;
#endif

		private void GenerateTextures()
		{
			m_rtGBuffers = new RenderTexture[ 4 ];
			for( int i = 0; i < m_rtGBuffers.Length - 2; ++i )
			{
				m_rtGBuffers[ i ] = new RenderTexture( (int)m_data.TexSize.x, (int)m_data.TexSize.y, 16, RenderTextureFormat.ARGB32 );
				m_rtGBuffers[ i ].Create();
			}

			m_rtGBuffers[ 2 ] = new RenderTexture( (int)m_data.TexSize.x, (int)m_data.TexSize.y, 16, RenderTextureFormat.ARGBHalf );
			m_rtGBuffers[ 2 ].Create();

			m_rtGBuffers[ 3 ] = new RenderTexture( (int)m_data.TexSize.x, (int)m_data.TexSize.y, 16, RenderTextureFormat.ARGBHalf );
			m_rtGBuffers[ 3 ].Create();

			m_trueDepth = new RenderTexture( (int)m_data.TexSize.x, (int)m_data.TexSize.y, 16, RenderTextureFormat.Depth );
			m_trueDepth.Create();
		}

		private void GenerateAlphaTextures( bool useMinResolution = false )
		{
			m_alphaGBuffers = new RenderTexture[ 4 ];

			int xMin = (int)m_data.TexSize.x / m_data.HorizontalFrames;
			int yMin = (int)m_data.TexSize.y / m_data.VerticalFrames;

			if( useMinResolution )
			{
				xMin = Mathf.Max( MinAlphaResolution, xMin );
				yMin = Mathf.Max( MinAlphaResolution, yMin );
			}

			for( int i = 0; i < m_alphaGBuffers.Length; ++i )
			{
				m_alphaGBuffers[ i ] = new RenderTexture( xMin, yMin, 16, RenderTextureFormat.ARGBHalf );
				m_alphaGBuffers[ i ].Create();
			}

			m_combinedAlphaTexture = new RenderTexture( xMin, yMin, 16, RenderTextureFormat.ARGBHalf );
			m_combinedAlphaTexture.Create();
		}

		private void ClearBuffers()
		{
			RenderTexture.active = null;
			foreach( var rt in m_rtGBuffers )
			{
				rt.Release();
			}
			m_rtGBuffers = null;

			m_trueDepth.Release();
			m_trueDepth = null;
		}

		private void ClearAlphaBuffers( bool clearCombinedRT = false )
		{
			RenderTexture.active = null;
			foreach( var rt in m_alphaGBuffers )
			{
				rt.Release();
			}
			m_alphaGBuffers = null;

			if( clearCombinedRT )
			{
				m_combinedAlphaTexture.Release();
				m_combinedAlphaTexture = null;
			}
		}

		public void ClearCombinedAlphaBuffer()
		{
			RenderTexture.active = null;
			m_combinedAlphaTexture.Release();
			m_combinedAlphaTexture = null;
		}

#if UNITY_EDITOR
		public void RenderToTexture( ref RenderTexture tex, string path )
		{
			Texture2D outfile = AssetDatabase.LoadAssetAtPath<Texture2D>( path );
			outfile = new Texture2D( (int)m_data.TexSize.x, (int)m_data.TexSize.y );
			outfile.name = Path.GetFileNameWithoutExtension( path );
			RenderTexture temp = RenderTexture.active;
			RenderTexture.active = tex;
			outfile.ReadPixels( new Rect( 0, 0, (int)m_data.TexSize.x, (int)m_data.TexSize.y ), 0, 0 );
			RenderTexture.active = temp;
			outfile.Apply();
			var bytes = outfile.EncodeToPNG();

			File.WriteAllBytes( path, bytes );
			EditorUtility.SetDirty( outfile );
		}

		public void ChangeTextureImporter( ref RenderTexture tex, string path, bool sRGB = true, bool changeResolution = false, bool normal = false, TextureImporterCompression compression = TextureImporterCompression.Compressed )
		{
			Texture2D outfile = AssetDatabase.LoadAssetAtPath<Texture2D>( path );
			TextureImporter tImporter = AssetImporter.GetAtPath( path ) as TextureImporter;
			if( tImporter != null )
			{
				if( ( normal && tImporter.textureCompression != compression ) || tImporter.sRGBTexture != sRGB || ( changeResolution && tImporter.maxTextureSize != (int)m_data.TexSize.x ) )
				{
					tImporter.sRGBTexture = sRGB;
					if( changeResolution )
						tImporter.maxTextureSize = (int)m_data.TexSize.x;
					if( normal )
						tImporter.textureCompression = compression;

					EditorUtility.SetDirty( tImporter );
					EditorUtility.SetDirty( outfile );
					tImporter.SaveAndReimport();
				}
			}
		}
		public void CalculateSheetBounds( ImpostorType impostorType )
		{
			m_trueFitsize = 0;
			m_depthFitsize = 0;

			int hframes = m_data.HorizontalFrames;
			int vframes = m_data.HorizontalFrames;
			if( impostorType == ImpostorType.Spherical )
			{
				vframes = m_data.HorizontalFrames - 1;
				if( m_data.DecoupleAxisFrames )
					vframes = m_data.VerticalFrames - 1;
			}

			for( int x = 0; x < hframes; x++ )
			{
				for( int y = 0; y <= vframes; y++ )
				{
					Bounds frameBounds = new Bounds();
					Matrix4x4 camMatrixRot = Matrix4x4.identity;

					if( impostorType == ImpostorType.Spherical ) //SPHERICAL
					{
						float fractionY = 0;
						if( vframes > 0 )
							fractionY = -( 180.0f / vframes );
						Quaternion hRot = Quaternion.Euler( fractionY * y + StartYRotation, 0, 0 );
						Quaternion vRot = Quaternion.Euler( 0, ( 360.0f / hframes ) * x + StartXRotation, 0 );
						camMatrixRot = Matrix4x4.Rotate( hRot * vRot );

					}
					else if( impostorType == ImpostorType.Octahedron ) //OCTAHEDRON
					{
						Vector3 forw = OctahedronToVector( ( (float)( x ) / ( (float)hframes - 1 ) ) * 2f - 1f, ( (float)( y ) / ( (float)vframes - 1 ) ) * 2f - 1f );
						Quaternion octa = Quaternion.LookRotation( new Vector3( forw.x * -1, forw.z * -1, forw.y * -1 ), Vector3.up );
						camMatrixRot = Matrix4x4.Rotate( octa ).inverse;
					}
					else if( impostorType == ImpostorType.HemiOctahedron ) //HEMIOCTAHEDRON
					{
						Vector3 forw = HemiOctahedronToVector( ( (float)( x ) / ( (float)hframes - 1 ) ) * 2f - 1f, ( (float)( y ) / ( (float)vframes - 1 ) ) * 2f - 1f );
						Quaternion octa = Quaternion.LookRotation( new Vector3( forw.x * -1, forw.z * -1, forw.y * -1 ), Vector3.up );
						camMatrixRot = Matrix4x4.Rotate( octa ).inverse;
					}

					for( int i = 0; i < Renderers.Length; i++ )
					{
						if( Renderers[ i ] == null )
							continue;

						if( i == 0 )
							frameBounds = Renderers[ i ].bounds;
						else
							frameBounds.Encapsulate( Renderers[ i ].bounds );
					}

					if( x == 0 && y == 0 )
						m_originalBound = frameBounds;

					frameBounds = frameBounds.Transform( camMatrixRot * m_rootTransform.worldToLocalMatrix );

					m_trueFitsize = Mathf.Max( m_trueFitsize, frameBounds.size.x, frameBounds.size.y );
					m_depthFitsize = Mathf.Max( m_depthFitsize, frameBounds.size.z );
				}
			}
#if AI_DEBUG_MODE
			m_renderInfo = "";
			m_renderInfo += "\nXY fit:\t" + m_trueFitsize;
			m_renderInfo += "\nDepth:\t" + m_depthFitsize;
#endif
		}

		public void DilateRenderTextureUsingMask( ref RenderTexture mainTex, ref RenderTexture maskTex, int pixelBleed, bool alpha, Material dilateMat = null )
		{
			if( pixelBleed == 0 )
				return;

			bool destroyMaterial = false;
			if( dilateMat == null )
			{
				destroyMaterial = true;
				Shader dilateShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( DilateGUID ) );
				dilateMat = new Material( dilateShader );
			}

			int width = mainTex.width;
			int height = mainTex.height;
			int depth = mainTex.depth;
			RenderTextureFormat format = mainTex.format;

			RenderTexture tempTex = RenderTexture.GetTemporary( width, height, depth, format );
			RenderTexture tempMask = RenderTexture.GetTemporary( width, height, depth, format );
			RenderTexture dilatedMask = RenderTexture.GetTemporary( width, height, depth, format );

			Graphics.Blit( maskTex, dilatedMask );

			for( int i = 0; i < pixelBleed; i++ )
			{
				dilateMat.SetTexture( "_MaskTex", dilatedMask );

				Graphics.Blit( mainTex, tempTex, dilateMat, alpha ? 1 : 0 );
				Graphics.Blit( tempTex, mainTex );

				Graphics.Blit( dilatedMask, tempMask, dilateMat, 1 );
				Graphics.Blit( tempMask, dilatedMask );
			}

			RenderTexture.ReleaseTemporary( tempTex );
			RenderTexture.ReleaseTemporary( tempMask );
			RenderTexture.ReleaseTemporary( dilatedMask );

			if( destroyMaterial )
			{
				DestroyImmediate( dilateMat );
				dilateMat = null;
			}
		}

		public void PackingRemapping( ref RenderTexture src, ref RenderTexture dst, int passIndex, Material packerMat = null, Texture extraTex = null )
		{
			bool destroyMaterial = false;
			if( packerMat == null )
			{
				destroyMaterial = true;
				Shader packerShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( PackerGUID ) );
				packerMat = new Material( packerShader );
			}

			if( extraTex != null )
				packerMat.SetTexture( "_A", extraTex );

			if( src == dst )
			{
				int width = src.width;
				int height = src.height;
				int depth = src.depth;
				RenderTextureFormat format = src.format;

				RenderTexture tempTex = RenderTexture.GetTemporary( width, height, depth, format );
				Graphics.Blit( src, tempTex, packerMat, passIndex );
				Graphics.Blit( tempTex, dst );
				RenderTexture.ReleaseTemporary( tempTex );
			}
			else
			{
				Graphics.Blit( src, dst, packerMat, passIndex );
			}

			if( destroyMaterial )
			{
				DestroyImmediate( packerMat );
				packerMat = null;
			}
		}

		public void CalculatePixelBounds()
		{
			bool sRGBcache = GL.sRGBWrite;
			GL.sRGBWrite = true;

			CalculateSheetBounds( m_data.ImpostorType );
			GenerateAlphaTextures( true );
			RenderImpostor( m_data.ImpostorType, false, true, true );

			GL.sRGBWrite = sRGBcache;

			PackingRemapping( ref m_alphaGBuffers[ 2 ], ref m_combinedAlphaTexture, 2 );

			RenderTexture.active = m_combinedAlphaTexture;
			Texture2D tempTex = new Texture2D( m_combinedAlphaTexture.width, m_combinedAlphaTexture.height, TextureFormat.RGBAFloat, false );
			tempTex.ReadPixels( new Rect( 0, 0, m_combinedAlphaTexture.width, m_combinedAlphaTexture.height ), 0, 0 );
			tempTex.Apply();
			RenderTexture.active = null;

			Rect testRect = new Rect( 0, 0, tempTex.width, tempTex.height );
			Vector2[][] paths;
			SpriteUtilityEx.GenerateOutline( tempTex, testRect, 0.2f, 0, false, out paths );
			int sum = 0;
			for( int i = 0; i < paths.Length; i++ )
			{
				sum += paths[ i ].Length;
			}

			Vector2[] minMaxPoints = new Vector2[ sum ];
			int index = 0;
			for( int i = 0; i < paths.Length; i++ )
			{
				for( int j = 0; j < paths[ i ].Length; j++ )
				{
					minMaxPoints[ index ] = (Vector2)( paths[ i ][ j ] ) + ( new Vector2( tempTex.width * 0.5f, tempTex.height * 0.5f ) );
					minMaxPoints[ index ] = Vector2.Scale( minMaxPoints[ index ], new Vector2( 1.0f / tempTex.width, 1.0f / tempTex.height ) );
					index++;
				}
			}

			Vector2 mins = Vector2.one;
			Vector2 maxs = Vector2.zero;

			for( int i = 0; i < minMaxPoints.Length; i++ )
			{
				mins.x = Mathf.Min( minMaxPoints[ i ].x, mins.x );
				mins.y = Mathf.Min( minMaxPoints[ i ].y, mins.y );
				maxs.x = Mathf.Max( minMaxPoints[ i ].x, maxs.x );
				maxs.y = Mathf.Max( minMaxPoints[ i ].y, maxs.y );
			}

			float scalarMin = Mathf.Min( mins.x, 1 - maxs.x );
			scalarMin = Mathf.Min( scalarMin, mins.y, 1 - maxs.y );
			m_trueFitsize *= ( 1 - ( scalarMin * 2 ) );
		}

		public void RenderCombinedAlpha( AmplifyImpostorAsset data = null )
		{
			AmplifyImpostorAsset tempData = m_data;
			if( data != null )
				m_data = data;

			CalculatePixelBounds();
			GenerateAlphaTextures();

			bool sRGBcache = GL.sRGBWrite;
			GL.sRGBWrite = true;

			RenderImpostor( m_data.ImpostorType, false, true, false );

			GL.sRGBWrite = sRGBcache;

			PackingRemapping( ref m_alphaGBuffers[ 2 ], ref m_combinedAlphaTexture, 2 );

			ClearAlphaBuffers();

			m_data = tempData;
		}

		public void CreateAssetFile( AmplifyImpostorAsset data = null )
		{
			string folderPath = this.OpenFolderForImpostor();

			if( string.IsNullOrEmpty( folderPath ) )
				return;

			string fileName = m_impostorName;

			if( string.IsNullOrEmpty( fileName ) )
				fileName = m_rootTransform.name + "_Impostor";

			folderPath = folderPath.TrimEnd( new char[] { '/', '*', '.', ' ' } );
			folderPath += "/";
			folderPath = folderPath.TrimStart( new char[] { '/', '*', '.', ' ' } );

			if( m_data == null )
			{
				Undo.RegisterCompleteObjectUndo( this, "Create Impostor Asset" );
				AmplifyImpostorAsset existingAsset = AssetDatabase.LoadAssetAtPath<AmplifyImpostorAsset>( folderPath + fileName + ".asset" );
				if( existingAsset != null )
				{
					m_data = existingAsset;
				}
				else
				{
					m_data = ScriptableObject.CreateInstance<AmplifyImpostorAsset>();
					AssetDatabase.CreateAsset( m_data, folderPath + fileName + ".asset" );
				}
			}
		}

		public void RenderAllDeferredGroups( AmplifyImpostorAsset data = null )
		{
			string folderPath = m_folderPath;
			if( m_data == null )
			{
				folderPath = this.OpenFolderForImpostor();
			}
			else if( string.IsNullOrEmpty( folderPath ) )
			{
				m_impostorName = m_data.name;
				folderPath = Path.GetDirectoryName( AssetDatabase.GetAssetPath( m_data ) ).Replace( "\\", "/" ) + "/";
			}

			if( string.IsNullOrEmpty( folderPath ) )
				return;

			string fileName = m_impostorName;

			if( string.IsNullOrEmpty( fileName ) )
				fileName = m_rootTransform.name + "_Impostor";

			m_folderPath = folderPath;
			folderPath = folderPath.TrimEnd( new char[] { '/', '*', '.', ' ' } );
			folderPath += "/";
			folderPath = folderPath.TrimStart( new char[] { '/', '*', '.', ' ' } );
			m_impostorName = fileName;

			Undo.RegisterCompleteObjectUndo( this, "Create Impostor" );
			if( m_data == null )
			{
				AmplifyImpostorAsset existingAsset = AssetDatabase.LoadAssetAtPath<AmplifyImpostorAsset>( folderPath + fileName + ".asset" );
				if( existingAsset != null )
				{
					m_data = existingAsset;
				}
				else
				{
					m_data = ScriptableObject.CreateInstance<AmplifyImpostorAsset>();
					AssetDatabase.CreateAsset( m_data, folderPath + fileName + ".asset" );
				}
			}
			else if( data != null )
			{
				m_data = data;
			}
			bool chache = GL.sRGBWrite;
			GL.sRGBWrite = true;

			if( !m_data.DecoupleAxisFrames )
				m_data.HorizontalFrames = m_data.VerticalFrames;

			string guid = m_data.ImpostorType == ImpostorType.Spherical ? ShaderGUID : ShaderOctaGUID;

			GenerateTextures();

			CalculatePixelBounds();

			bool restoreKey = false;
			if( Shader.IsKeywordEnabled( "LIGHTPROBE_SH" ) )
			{
				restoreKey = true;
				Shader.DisableKeyword( "LIGHTPROBE_SH" );
			}

			RenderImpostor( m_data.ImpostorType, true, false, true );

			if( restoreKey )
				Shader.EnableKeyword( "LIGHTPROBE_SH" );

			Shader packerShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( PackerGUID ) );
			Material packerMat = new Material( packerShader );

			// Switch alpha with occlusion
			int width = m_rtGBuffers[ 0 ].width;
			int height = m_rtGBuffers[ 0 ].height;
			int depth = m_rtGBuffers[ 0 ].depth;
			RenderTextureFormat format = m_rtGBuffers[ 3 ].format;

			RenderTexture tempTex = RenderTexture.GetTemporary( width, height, depth, m_rtGBuffers[ 0 ].format );
			RenderTexture tempTex2 = RenderTexture.GetTemporary( width, height, depth, m_rtGBuffers[ 3 ].format );

			packerMat.SetTexture( "_A", m_rtGBuffers[ 3 ] );
			Graphics.Blit( m_rtGBuffers[ 0 ], tempTex, packerMat, 4 ); //A.b
			packerMat.SetTexture( "_A", m_rtGBuffers[ 0 ] );
			Graphics.Blit( m_rtGBuffers[ 3 ], tempTex2, packerMat, 4 ); //B.a
			Graphics.Blit( tempTex, m_rtGBuffers[ 0 ] );
			Graphics.Blit( tempTex2, m_rtGBuffers[ 3 ] );
			RenderTexture.ReleaseTemporary( tempTex );
			RenderTexture.ReleaseTemporary( tempTex2 );

			// Fix Albedo
			PackingRemapping( ref m_rtGBuffers[ 0 ], ref m_rtGBuffers[ 0 ], 5, packerMat, m_rtGBuffers[ 1 ] );

			// Pack Depth
			PackingRemapping( ref m_rtGBuffers[ 2 ], ref m_rtGBuffers[ 2 ], 0, packerMat, m_trueDepth );

			// Fix Emission
#if UNITY_2017_3_OR_NEWER
			PackingRemapping( ref m_rtGBuffers[ 3 ], ref m_rtGBuffers[ 3 ], 1, packerMat );
#endif
			DestroyImmediate( packerMat );
			packerMat = null;

			Shader dilateShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( DilateGUID ) );
			Material dilateMat = new Material( dilateShader );

			// Dilation
			if( ( m_data.BufferMask & DeferredBuffers.AlbedoAlpha ) == DeferredBuffers.AlbedoAlpha )
				DilateRenderTextureUsingMask( ref m_rtGBuffers[ 0 ], ref m_rtGBuffers[ 0 ], m_data.PixelPadding, false, dilateMat );
			if( ( m_data.BufferMask & DeferredBuffers.SpecularSmoothness ) == DeferredBuffers.SpecularSmoothness )
				DilateRenderTextureUsingMask( ref m_rtGBuffers[ 1 ], ref m_rtGBuffers[ 0 ], m_data.PixelPadding, true, dilateMat );
			if( ( m_data.BufferMask & DeferredBuffers.NormalDepth ) == DeferredBuffers.NormalDepth )
				DilateRenderTextureUsingMask( ref m_rtGBuffers[ 2 ], ref m_rtGBuffers[ 0 ], m_data.PixelPadding, true, dilateMat );
			if( ( m_data.BufferMask & DeferredBuffers.EmissionOcclusion ) == DeferredBuffers.EmissionOcclusion )
				DilateRenderTextureUsingMask( ref m_rtGBuffers[ 3 ], ref m_rtGBuffers[ 0 ], m_data.PixelPadding, true, dilateMat );

			DestroyImmediate( dilateMat );
			dilateMat = null;

			bool isPrefab = false;
			if( PrefabUtility.GetPrefabType( this.gameObject ) == PrefabType.Prefab )
				isPrefab = true;

			// Create billboard
#if AI_DEBUG_MODE
			if( m_createGameobject )
#endif
			//{
			Shader defaultShader = AssetDatabase.LoadAssetAtPath<Shader>( AssetDatabase.GUIDToAssetPath( guid ) );
			Material material = m_data.Material;// AssetDatabase.LoadAssetAtPath<Material>( folderPath + fileName + ".mat" );
			if( material == null )
			{
				material = new Material( defaultShader );
				material.name = fileName;
				material.enableInstancing = true;
				//AssetDatabase.CreateAsset( material, folderPath + material.name + ".mat" );
				AssetDatabase.AddObjectToAsset( material, m_data );
				m_data.Material = material;
				EditorUtility.SetDirty( material );
			}
			else
			{
				material.shader = defaultShader;
				material.name = fileName;
				EditorUtility.SetDirty( material );
			}

			// construct file names
			Texture2D tex;
			bool hasDifferentResolution = false;
			tex = material.GetTexture( "_Albedo" ) as Texture2D;
			string albedoFilepath = string.Empty;
			if( tex != null )
			{
				albedoFilepath = Path.GetDirectoryName( AssetDatabase.GetAssetPath( tex ) ).Replace( "\\", "/" ) + "/";
				if( tex.width != (int)m_data.TexSize.x )
					hasDifferentResolution = true;
			}
			else
				albedoFilepath = folderPath;
			albedoFilepath += fileName + ImpostorBakingTools.GlobalAlbedoOcclusion + ".png";

			tex = material.GetTexture( "_Specular" ) as Texture2D;
			string specularFilepath = string.Empty;
			if( tex != null )
			{
				specularFilepath = Path.GetDirectoryName( AssetDatabase.GetAssetPath( tex ) ).Replace( "\\", "/" ) + "/";
				if( tex.width != (int)m_data.TexSize.x )
					hasDifferentResolution = true;
			}
			else
				specularFilepath = folderPath;
			specularFilepath += fileName + ImpostorBakingTools.GlobalSpecularSmoothness + ".png";

			tex = material.GetTexture( "_Normals" ) as Texture2D;
			string normalsFilepath = string.Empty;
			if( tex != null )
			{
				normalsFilepath = Path.GetDirectoryName( AssetDatabase.GetAssetPath( tex ) ).Replace( "\\", "/" ) + "/";
				if( tex.width != (int)m_data.TexSize.x )
					hasDifferentResolution = true;
			}
			else
				normalsFilepath = folderPath;
			normalsFilepath += fileName + ImpostorBakingTools.GlobalNormalDepth + ".png";

			tex = material.GetTexture( "_Emission" ) as Texture2D;
			string emissionFilepath = string.Empty;
			if( tex != null )
			{
				emissionFilepath = Path.GetDirectoryName( AssetDatabase.GetAssetPath( tex ) ).Replace( "\\", "/" ) + "/";
				if( tex.width != (int)m_data.TexSize.x )
					hasDifferentResolution = true;
			}
			else
				emissionFilepath = folderPath;
			emissionFilepath += fileName + ImpostorBakingTools.GlobalEmissionAlpha + ".png";

			bool resizeTextures = false;
			if( hasDifferentResolution && EditorPrefs.GetInt( ImpostorBakingTools.PrefGlobalTexImport, 0 ) == 0 )
				resizeTextures = EditorUtility.DisplayDialog( "Resize Textures?", "Do you wish to override the Texture Import settings to match the provided Impostor Texture Size?", "Yes", "No" );
			else if( EditorPrefs.GetInt( ImpostorBakingTools.PrefGlobalTexImport, 0 ) == 1 )
				resizeTextures = true;
			else
				resizeTextures = false;

			// save to texture files
			AmplifyTextureImporter.ImportData.Clear();
			if( resizeTextures )
				AmplifyTextureImporter.MaxSize = (int)m_data.TexSize.x;
			AmplifyTextureImporter.Compression = m_data.NormalCompression;
			if( ( m_data.BufferMask & DeferredBuffers.AlbedoAlpha ) == DeferredBuffers.AlbedoAlpha )
			{
				RenderToTexture( ref m_rtGBuffers[ 0 ], albedoFilepath );
				AmplifyTextureImporter.ImportData.Add( albedoFilepath, new DataHolder() );
			}
			if( ( m_data.BufferMask & DeferredBuffers.SpecularSmoothness ) == DeferredBuffers.SpecularSmoothness )
			{
				RenderToTexture( ref m_rtGBuffers[ 1 ], specularFilepath );
				AmplifyTextureImporter.ImportData.Add( specularFilepath, new DataHolder() );
			}
			if( ( m_data.BufferMask & DeferredBuffers.NormalDepth ) == DeferredBuffers.NormalDepth )
			{
				RenderToTexture( ref m_rtGBuffers[ 2 ], normalsFilepath );
				AmplifyTextureImporter.ImportData.Add( normalsFilepath, new DataHolder( false, true ) );
			}
			if( ( m_data.BufferMask & DeferredBuffers.EmissionOcclusion ) == DeferredBuffers.EmissionOcclusion )
			{
				RenderToTexture( ref m_rtGBuffers[ 3 ], emissionFilepath );
				AmplifyTextureImporter.ImportData.Add( emissionFilepath, new DataHolder( false, false ) );
			}

			GL.sRGBWrite = chache;

			GameObject impostorObject = null;
			RenderCombinedAlpha();
			Vector4 offsetCalc = transform.worldToLocalMatrix * new Vector4( m_originalBound.center.x, m_originalBound.center.y, m_originalBound.center.z, 1 );
			Vector3 offset = new Vector3( offsetCalc.x, offsetCalc.y, offsetCalc.z );

			bool justCreated = false;
			UnityEngine.Object targetPrefab = null;
			GameObject tempGO = null;
			//GameObject deleteGO = null;
#if AI_DEBUG_MODE
			if( m_generateQuad )
#endif
			{
				Mesh mesh = m_data.Mesh;// AssetDatabase.LoadAssetAtPath<Mesh>( folderPath + fileName + m_quadName + ".asset" );
				if( mesh == null )
				{
					mesh = GenerateMesh( m_data.ShapePoints, offset, m_trueFitsize, m_trueFitsize, true );
					mesh.name = fileName;
					//AssetDatabase.CreateAsset( mesh, folderPath + fileName + m_quadName + ".asset" );
					AssetDatabase.AddObjectToAsset( mesh, m_data );
					m_data.Mesh = mesh;
					EditorUtility.SetDirty( mesh );
				}
				else
				{
					Mesh tempmesh = GenerateMesh( m_data.ShapePoints, offset, m_trueFitsize, m_trueFitsize, true );
					EditorUtility.CopySerialized( tempmesh, mesh );
					mesh.vertices = tempmesh.vertices;
					mesh.triangles = tempmesh.triangles;
					mesh.uv = tempmesh.uv;
					mesh.normals = tempmesh.normals;
					mesh.bounds = tempmesh.bounds;
					mesh.name = fileName;
					EditorUtility.SetDirty( mesh );
				}
				//GameObject go = null;
				if( isPrefab )
				{
					if( m_lastImpostor != null && PrefabUtility.GetPrefabType( m_lastImpostor ) == PrefabType.Prefab )
					{
						impostorObject = m_lastImpostor;
					}
					else
					{
						GameObject mainGO = new GameObject( "Impostor", new Type[] { typeof( MeshFilter ), typeof( MeshRenderer ) } );
						//impostorObject = PrefabUtility.CreatePrefab( folderPath + fileName + ".prefab", mainGO );
						//DestroyImmediate( mainGO );
						impostorObject = mainGO;
						//go = mainGO;
						justCreated = true;
					}
				}
				else
				{
					if( m_lastImpostor != null )
					{
						impostorObject = m_lastImpostor;
						//impostorObject.transform.position = m_originalBound.center;
						impostorObject.transform.position = m_rootTransform.position;
						impostorObject.transform.rotation = m_rootTransform.rotation;
					}
					else
					{
						impostorObject = new GameObject( "Impostor", new Type[] { typeof( MeshFilter ), typeof( MeshRenderer ) } );
						Undo.RegisterCreatedObjectUndo( impostorObject, "Create Impostor" );
						//impostorObject.transform.position = m_originalBound.center;
						impostorObject.transform.position = m_rootTransform.position;
						impostorObject.transform.rotation = m_rootTransform.rotation;

						justCreated = true;
					}
				}
				m_lastImpostor = impostorObject;
				impostorObject.transform.localScale = m_rootTransform.localScale;
				impostorObject.GetComponent<MeshFilter>().sharedMesh = mesh;

				if( justCreated )
				{
					if( LodGroup != null )
					{
						if( isPrefab )
						{
							targetPrefab = PrefabUtility.GetPrefabObject( ( Selection.activeObject as GameObject ).transform.root.gameObject );
							GameObject targetGO = AssetDatabase.LoadAssetAtPath( folderPath+ ( Selection.activeObject as GameObject ).transform.root.gameObject.name + ".prefab", typeof( GameObject ) ) as GameObject;
							UnityEngine.Object inst = PrefabUtility.InstantiatePrefab( targetGO );
							tempGO = inst as GameObject;
							AmplifyImpostor ai = tempGO.GetComponentInChildren<AmplifyImpostor>();
							impostorObject.transform.SetParent( ai.LodGroup.transform );
							ai.m_lastImpostor = impostorObject;
							PrefabUtility.ReplacePrefab( tempGO, targetPrefab, ReplacePrefabOptions.ConnectToPrefab );
							ai = targetGO.GetComponentInChildren<AmplifyImpostor>();
							impostorObject = ai.m_lastImpostor;
							DestroyImmediate( tempGO );
						}
						else
						{
							impostorObject.transform.SetParent( LodGroup.transform );
						}

						switch( m_lodReplacement )
						{
							default:
							case LODReplacement.DoNothing:
							break;
							case LODReplacement.ReplaceCulled:
							{
								LOD[] lods = LodGroup.GetLODs();
								Array.Resize( ref lods, lods.Length + 1 );
								LOD lastLOD = new LOD();
								lastLOD.screenRelativeTransitionHeight = 0;
								lastLOD.renderers = impostorObject.GetComponents<Renderer>();
								lods[ lods.Length - 1 ] = lastLOD;
								LodGroup.SetLODs( lods );
							}
							break;
							case LODReplacement.ReplaceLast:
							{
								LOD[] lods = LodGroup.GetLODs();

								foreach( Renderer item in lods[ lods.Length - 1 ].renderers )
									item.enabled = false;

								lods[ lods.Length - 1 ].renderers = impostorObject.GetComponents<Renderer>();
								LodGroup.SetLODs( lods );
							}
							break;
							case LODReplacement.ReplaceAllExceptFirst:
							{
								LOD[] lods = LodGroup.GetLODs();
								for( int i = lods.Length - 1; i > 0; i-- )
								{
									foreach( Renderer item in lods[ i ].renderers )
										item.enabled = false;
								}
								float lastTransition = lods[ lods.Length - 1 ].screenRelativeTransitionHeight;
								Array.Resize( ref lods, 2 );
								lods[ lods.Length - 1 ].screenRelativeTransitionHeight = lastTransition;
								lods[ lods.Length - 1 ].renderers = impostorObject.GetComponents<Renderer>();
								LodGroup.SetLODs( lods );
							}
							break;
							case LODReplacement.ReplaceSpecific:
							{
								LOD[] lods = LodGroup.GetLODs();
								foreach( Renderer item in lods[ m_insertIndex ].renderers )
									item.enabled = false;

								lods[ m_insertIndex ].renderers = impostorObject.GetComponents<Renderer>();
								LodGroup.SetLODs( lods );
							}
							break;
							case LODReplacement.ReplaceAfterSpecific:
							{
								LOD[] lods = LodGroup.GetLODs();
								for( int i = lods.Length - 1; i > m_insertIndex; i-- )
								{
									foreach( Renderer item in lods[ i ].renderers )
										item.enabled = false;
								}
								float lastTransition = lods[ lods.Length - 1 ].screenRelativeTransitionHeight;
								if( m_insertIndex == lods.Length - 1 )
									lastTransition = 0;
								Array.Resize( ref lods, 2 + m_insertIndex );
								lods[ lods.Length - 1 ].screenRelativeTransitionHeight = lastTransition;
								lods[ lods.Length - 1 ].renderers = impostorObject.GetComponents<Renderer>();
								LodGroup.SetLODs( lods );
							}
							break;
							case LODReplacement.InsertAfter:
							{
								LOD[] lods = LodGroup.GetLODs();
								Array.Resize( ref lods, lods.Length + 1 );
								for( int i = lods.Length - 1; i > m_insertIndex; i-- )
								{
									lods[ i ].screenRelativeTransitionHeight = lods[ i - 1 ].screenRelativeTransitionHeight;
									lods[ i ].fadeTransitionWidth = lods[ i - 1 ].fadeTransitionWidth;
									lods[ i ].renderers = lods[ i - 1 ].renderers;
								}
								lods[ m_insertIndex + 1 ].renderers = impostorObject.GetComponents<Renderer>();
								lods[ m_insertIndex + 1 ].screenRelativeTransitionHeight = ( lods[ m_insertIndex + 2 ].screenRelativeTransitionHeight + lods[ m_insertIndex ].screenRelativeTransitionHeight ) / 2f;
								LodGroup.SetLODs( lods );
							}
							break;
						}
						Undo.RegisterCompleteObjectUndo( LodGroup, "Create Impostor" );
					}
					else if( !isPrefab )
					{
						impostorObject.transform.SetParent( m_rootTransform.parent );
						int sibIndex = m_rootTransform.GetSiblingIndex();
						impostorObject.transform.SetSiblingIndex( sibIndex + 1 );
						m_rootTransform.SetSiblingIndex( sibIndex );
					}
				}
			}
#if AI_DEBUG_MODE
			else
			{
				GameObject mainGO = new GameObject( "Impostor", new Type[] { typeof( MeshFilter ), typeof( MeshRenderer ) } );
				impostorObject = Instantiate( mainGO, m_rootTransform.position, m_rootTransform.rotation );
				impostorObject.transform.localScale = Vector3.one * m_trueFitsize * 0.5f;
			}
#endif
			EditorUtility.SetDirty( m_data );

			impostorObject.name = fileName;
			impostorObject.GetComponent<Renderer>().sharedMaterial = material;
			EditorUtility.SetDirty( impostorObject );

			// saving and refreshing to make sure textures can be set properly into the material
			AmplifyTextureImporter.Activated = true;
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			AmplifyTextureImporter.Activated = false;
			AmplifyTextureImporter.MaxSize = -1;
			AmplifyTextureImporter.ImportData.Clear();

			//Texture2D tex;
			//bool 
			hasDifferentResolution = false;
			if( ( m_data.BufferMask & DeferredBuffers.AlbedoAlpha ) == DeferredBuffers.AlbedoAlpha )
			{
				tex = material.GetTexture( "_Albedo" ) as Texture2D;
				if( tex == null )
					tex = AssetDatabase.LoadAssetAtPath<Texture2D>( albedoFilepath );
				if( tex != null )
					material.SetTexture( "_Albedo", tex );

				if( tex.width != m_data.TexSize.x )
					hasDifferentResolution = true;
			}

			if( ( m_data.BufferMask & DeferredBuffers.SpecularSmoothness ) == DeferredBuffers.SpecularSmoothness )
			{
				tex = material.GetTexture( "_Specular" ) as Texture2D;
				if( tex == null )
					tex = AssetDatabase.LoadAssetAtPath<Texture2D>( specularFilepath );
				if( tex != null )
					material.SetTexture( "_Specular", tex );

				if( tex.width != m_data.TexSize.x )
					hasDifferentResolution = true;
			}

			if( ( m_data.BufferMask & DeferredBuffers.NormalDepth ) == DeferredBuffers.NormalDepth )
			{
				tex = material.GetTexture( "_Normals" ) as Texture2D;
				if( tex == null )
					tex = AssetDatabase.LoadAssetAtPath<Texture2D>( normalsFilepath );
				if( tex != null )
					material.SetTexture( "_Normals", tex );

				if( tex.width != m_data.TexSize.x )
					hasDifferentResolution = true;
			}

			if( ( m_data.BufferMask & DeferredBuffers.EmissionOcclusion ) == DeferredBuffers.EmissionOcclusion )
			{
				tex = material.GetTexture( "_Emission" ) as Texture2D;
				if( tex == null )
					tex = AssetDatabase.LoadAssetAtPath<Texture2D>( emissionFilepath );
				if( tex != null )
					material.SetTexture( "_Emission", tex );

				if( tex.width != m_data.TexSize.x )
					hasDifferentResolution = true;
			}

			if( m_data.ImpostorType == ImpostorType.HemiOctahedron )
			{
				material.SetFloat( "_Hemi", 1 );
				material.EnableKeyword( "_HEMI_ON" );
			}
			else
			{
				material.SetFloat( "_Hemi", 0 );
				material.DisableKeyword( "_HEMI_ON" );
			}

			material.SetFloat( "_Frames", m_data.HorizontalFrames );
			material.SetFloat( "_ImpostorSize", m_trueFitsize );
			material.SetVector( "_Offset", offset );
			material.SetFloat( "_DepthSize", m_depthFitsize * m_rootTransform.localScale.z );
			material.SetFloat( "_FramesX", m_data.HorizontalFrames );
			material.SetFloat( "_FramesY", m_data.VerticalFrames );
			EditorUtility.SetDirty( material );

			if( hasDifferentResolution && resizeTextures )
				resizeTextures = true;
			else
				resizeTextures = false;

			TextureImporterCompression compression = TextureImporterCompression.Compressed;
			switch( m_data.NormalCompression )
			{
				case NormalCompression.None:
				compression = TextureImporterCompression.Uncompressed;
				break;
				case NormalCompression.LowQuality:
				compression = TextureImporterCompression.CompressedLQ;
				break;
				default:
				case NormalCompression.NormalQuality:
				compression = TextureImporterCompression.Compressed;
				break;
				case NormalCompression.HighQuality:
				compression = TextureImporterCompression.CompressedHQ;
				break;
			}

			if( ( m_data.BufferMask & DeferredBuffers.AlbedoAlpha ) == DeferredBuffers.AlbedoAlpha )
				ChangeTextureImporter( ref m_rtGBuffers[ 0 ], albedoFilepath, true, resizeTextures );
			if( ( m_data.BufferMask & DeferredBuffers.SpecularSmoothness ) == DeferredBuffers.SpecularSmoothness )
				ChangeTextureImporter( ref m_rtGBuffers[ 1 ], specularFilepath, true, resizeTextures );
			if( ( m_data.BufferMask & DeferredBuffers.NormalDepth ) == DeferredBuffers.NormalDepth )
				ChangeTextureImporter( ref m_rtGBuffers[ 2 ], normalsFilepath, false, resizeTextures, true, compression );
			if( ( m_data.BufferMask & DeferredBuffers.EmissionOcclusion ) == DeferredBuffers.EmissionOcclusion )
				ChangeTextureImporter( ref m_rtGBuffers[ 3 ], emissionFilepath, false, resizeTextures );

			ClearBuffers();

			Data.Version = VersionInfo.FullNumber;
		}
#endif

		/// <summary>
		/// Renders Impostors maps to render textures
		/// </summary>
		/// <param name="impostorType"></param>
		/// <param name="impostorMaps">set to true to render all selected maps</param>
		/// <param name="combinedAlphas">set to true to render the combined alpha map which is used to generate the mesh</param>
		public void RenderImpostor( ImpostorType impostorType, bool impostorMaps = true, bool combinedAlphas = false, bool useMinResolution = false )
		{
			if( !impostorMaps && !combinedAlphas ) //leave early
				return;

			CommandBuffer commandBuffer = new CommandBuffer();
			if( impostorMaps )
			{
				commandBuffer.name = "GBufferCatcher";
				RenderTargetIdentifier[] rtIDs = new RenderTargetIdentifier[] { m_rtGBuffers[ 0 ], m_rtGBuffers[ 1 ], m_rtGBuffers[ 2 ], m_rtGBuffers[ 3 ] };
				commandBuffer.SetRenderTarget( rtIDs, m_trueDepth );
				commandBuffer.ClearRenderTarget( true, true, Color.clear, 1 );
			}

			CommandBuffer commandAlphaBuffer = new CommandBuffer();
			if( combinedAlphas )
			{
				commandAlphaBuffer.name = "DepthAlphaCatcher";
				RenderTargetIdentifier[] rtIDsAlpha = new RenderTargetIdentifier[] { m_alphaGBuffers[ 0 ], m_alphaGBuffers[ 1 ], m_alphaGBuffers[ 2 ], m_alphaGBuffers[ 3 ] };
				commandAlphaBuffer.SetRenderTarget( rtIDsAlpha, rtIDsAlpha[ 0 ] );
				commandAlphaBuffer.ClearRenderTarget( true, true, Color.clear, 1 );
			}

			int hframes = m_data.HorizontalFrames;
			int vframes = m_data.HorizontalFrames;

			if( impostorType == ImpostorType.Spherical )
			{
				vframes = m_data.HorizontalFrames - 1;
				if( m_data.DecoupleAxisFrames )
					vframes = m_data.VerticalFrames - 1;
			}

			for( int x = 0; x < hframes; x++ )
			{
				for( int y = 0; y <= vframes; y++ )
				{
					Bounds frameBounds = new Bounds();
					Matrix4x4 camMatrixRot = Matrix4x4.identity;

					if( impostorType == ImpostorType.Spherical ) //SPHERICAL
					{
						float fractionY = 0;
						if( vframes > 0 )
							fractionY = -( 180.0f / vframes );
						Quaternion hRot = Quaternion.Euler( fractionY * y + StartYRotation, 0, 0 );
						Quaternion vRot = Quaternion.Euler( 0, ( 360.0f / hframes ) * x + StartXRotation, 0 );
						camMatrixRot = Matrix4x4.Rotate( hRot * vRot );

					}
					else if( impostorType == ImpostorType.Octahedron ) //OCTAHEDRON
					{
						Vector3 forw = OctahedronToVector( ( (float)( x ) / ( (float)hframes - 1 ) ) * 2f - 1f, ( (float)( y ) / ( (float)vframes - 1 ) ) * 2f - 1f );
						Quaternion octa = Quaternion.LookRotation( new Vector3( forw.x * -1, forw.z * -1, forw.y * -1 ), Vector3.up );
						camMatrixRot = Matrix4x4.Rotate( octa ).inverse;
					}
					else if( impostorType == ImpostorType.HemiOctahedron ) //HEMIOCTAHEDRON
					{
						Vector3 forw = HemiOctahedronToVector( ( (float)( x ) / ( (float)hframes - 1 ) ) * 2f - 1f, ( (float)( y ) / ( (float)vframes - 1 ) ) * 2f - 1f );
						Quaternion octa = Quaternion.LookRotation( new Vector3( forw.x * -1, forw.z * -1, forw.y * -1 ), Vector3.up );
						camMatrixRot = Matrix4x4.Rotate( octa ).inverse;
					}

					for( int i = 0; i < Renderers.Length; i++ )
					{
						if( Renderers[ i ] == null )
							continue;

						if( i == 0 )
							frameBounds = Renderers[ i ].bounds;
						else
							frameBounds.Encapsulate( Renderers[ i ].bounds );
					}

					if( x == 0 && y == 0 )
						m_originalBound = frameBounds;

					frameBounds = frameBounds.Transform( camMatrixRot * m_rootTransform.worldToLocalMatrix );

					Matrix4x4 V = camMatrixRot.inverse * Matrix4x4.LookAt( frameBounds.center - new Vector3( 0, 0, m_depthFitsize * 0.5f ), frameBounds.center, Vector3.up );
					float fitSize = m_trueFitsize * 0.5f;

					Matrix4x4 P = Matrix4x4.Ortho( -fitSize, fitSize, -fitSize, fitSize, 0, -m_depthFitsize );

					if( impostorMaps )
					{
						commandBuffer.SetViewProjectionMatrices( V.inverse, P );
						commandBuffer.SetViewport( new Rect( ( m_data.TexSize.x / hframes ) * x, ( m_data.TexSize.y / ( vframes + ( impostorType == ImpostorType.Spherical ? 1 : 0 ) ) ) * y, ( m_data.TexSize.x / m_data.HorizontalFrames ), ( m_data.TexSize.y / m_data.VerticalFrames ) ) );
					}

					if( combinedAlphas )
					{
						int xMin = (int)m_data.TexSize.x / m_data.HorizontalFrames;
						int yMin = (int)m_data.TexSize.y / m_data.VerticalFrames;

						if( useMinResolution )
						{
							xMin = Mathf.Max( MinAlphaResolution, xMin );
							yMin = Mathf.Max( MinAlphaResolution, yMin );
						}

						commandAlphaBuffer.SetViewProjectionMatrices( V.inverse, P );
						commandAlphaBuffer.SetViewport( new Rect( 0, 0, xMin, yMin ) );
					}

					for( int j = 0; j < Renderers.Length; j++ )
					{
						if( Renderers[ j ] == null )
							continue;

						Transform childTransform = Renderers[ j ].transform;
						Material[] meshMaterials = Renderers[ j ].sharedMaterials;

						// skip non-meshes, for now
						var meshFilter = childTransform.GetComponent<MeshFilter>();
						if( meshFilter == null )
						{
							continue;
						}

						for( int k = 0; k < meshMaterials.Length; k++ )
						{
							Mesh mesh = meshFilter.sharedMesh;
							Material renderMaterial = meshMaterials[ k ];
							int pass = renderMaterial.FindPass( "DEFERRED" );
							if( pass == -1 )
								pass = renderMaterial.FindPass( "Deferred" );
							if( pass == -1 ) // last resort fallback
							{
								pass = 0;
								for( int sp = 0; sp < renderMaterial.passCount; sp++ )
								{
									string lightmode = renderMaterial.GetTag( "LightMode", true );
									if( lightmode.Equals( "Deferred" ) )
									{
										pass = sp;
										break;
									}
								}
							}

							// Only useful for 2017.1 and 2017.2
							commandBuffer.EnableShaderKeyword( "UNITY_HDR_ON" );

							Matrix4x4 localMatrix = m_rootTransform.worldToLocalMatrix * childTransform.localToWorldMatrix;
							if( impostorMaps )
								commandBuffer.DrawMesh( mesh, localMatrix, renderMaterial, k, pass );

							if( combinedAlphas )
								commandAlphaBuffer.DrawMesh( mesh, localMatrix, renderMaterial, k, pass );
						}
					}

					if( impostorMaps )
						Graphics.ExecuteCommandBuffer( commandBuffer );

					if( combinedAlphas )
						Graphics.ExecuteCommandBuffer( commandAlphaBuffer );
				}
			}

			commandBuffer.Release();
			commandBuffer = null;

			commandAlphaBuffer.Release();
			commandAlphaBuffer = null;
		}

		private Vector3 OctahedronToVector( Vector2 oct )
		{
			Vector3 N = new Vector3( oct.x, oct.y, 1.0f - Mathf.Abs( oct.x ) - Mathf.Abs( oct.y ) );
			float t = Mathf.Clamp01( -N.z );
			N.Set( N.x + ( N.x >= 0.0f ? -t : t ), N.y + ( N.y >= 0.0f ? -t : t ), N.z );
			N = Vector3.Normalize( N );
			return N;
		}

		private Vector3 OctahedronToVector( float x, float y )
		{
			Vector3 N = new Vector3( x, y, 1.0f - Mathf.Abs( x ) - Mathf.Abs( y ) );
			float t = Mathf.Clamp01( -N.z );
			N.Set( N.x + ( N.x >= 0.0f ? -t : t ), N.y + ( N.y >= 0.0f ? -t : t ), N.z );
			N = Vector3.Normalize( N );
			return N;
		}

		private Vector3 HemiOctahedronToVector( float x, float y )
		{
			float tempx = x;
			float tempy = y;

			x = ( tempx + tempy ) * 0.5f;
			y = ( tempx - tempy ) * 0.5f;
			Vector3 N = new Vector3( x, y, 1.0f - Mathf.Abs( x ) - Mathf.Abs( y ) );
			N = Vector3.Normalize( N );
			return N;
		}

		public Mesh GenerateMesh( Vector2[] points, Vector3 offset, float width = 1, float height = 1, bool invertY = true )
		{
			Vector2[] newPoints = new Vector2[ points.Length ];
			Vector2[] UVs = new Vector2[ points.Length ];
			Array.Copy( points, newPoints, points.Length );
			float halfWidth = width * 0.5f;
			float halfHeight = height * 0.5f;

			if( invertY )
			{
				for( int i = 0; i < newPoints.Length; i++ )
				{
					newPoints[ i ] = new Vector2( newPoints[ i ].x, 1 - newPoints[ i ].y );
				}
			}

			Array.Copy( newPoints, UVs, newPoints.Length );

			for( int i = 0; i < newPoints.Length; i++ )
			{
				newPoints[ i ] = new Vector2( newPoints[ i ].x * width - halfWidth, newPoints[ i ].y * height - halfHeight );
			}

			Triangulator tr = new Triangulator( newPoints );
			int[] indices = tr.Triangulate();

			Vector3[] vertices = new Vector3[ tr.Points.Count ];
			for( int i = 0; i < vertices.Length; i++ )
			{
				vertices[ i ] = new Vector3( tr.Points[ i ].x, tr.Points[ i ].y, 0 );
			}

			Mesh mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.uv = UVs;
			mesh.triangles = indices;
			mesh.RecalculateNormals();
			mesh.bounds = new Bounds( offset, m_originalBound.size );

			return mesh;
		}
	}
}
