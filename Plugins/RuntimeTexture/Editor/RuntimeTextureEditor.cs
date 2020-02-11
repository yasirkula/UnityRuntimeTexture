using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace RuntimeTextureNamespace
{
	[CustomEditor( typeof( RuntimeTextureImporter ) )]
	[CanEditMultipleObjects]
	public class RuntimeTextureEditor : ScriptedImporterEditor
	{
		private Object[] runtimeTextures;
		private Texture2D[] textures;

		private SerializedProperty originalSizeProp;
		private SerializedProperty scaledSizeProp;
		private SerializedProperty preserveAspectRatioProp;
		private SerializedProperty paddingProp;
		private SerializedProperty paddingColorProp;
		private SerializedProperty readWriteEnabledProp;
		private SerializedProperty generateMipMapsProp;
		private SerializedProperty saveAsPNGProp;
		private SerializedProperty jpegQualityProp;

		private string fileSizeInfoString;

		private static bool previewShowBackground = false;
		public override bool showImportedObject { get { return false; } }

		public override void OnEnable()
		{
			base.OnEnable();

			runtimeTextures = targets;
			textures = new Texture2D[runtimeTextures.Length];
			long totalSize = 0L;
			for( int i = 0; i < runtimeTextures.Length; i++ )
			{
				RuntimeTexture.IEditorInterface runtimeTexture = AssetDatabase.LoadAssetAtPath<RuntimeTexture>( ( (RuntimeTextureImporter) runtimeTextures[i] ).assetPath );
				textures[i] = runtimeTexture.Texture;
				totalSize += runtimeTexture.Length;
			}

			if( runtimeTextures.Length == 1 )
				fileSizeInfoString = "Size: " + GetReadableFilesize( totalSize );
			else
				fileSizeInfoString = string.Concat( "Size: ", GetReadableFilesize( totalSize ), " (", runtimeTextures.Length, " objects)" );

			originalSizeProp = serializedObject.FindProperty( "originalSize" );
			scaledSizeProp = serializedObject.FindProperty( "scaledSize" );
			preserveAspectRatioProp = serializedObject.FindProperty( "preserveAspectRatio" );
			paddingProp = serializedObject.FindProperty( "padding" );
			paddingColorProp = serializedObject.FindProperty( "paddingColor" );
			readWriteEnabledProp = serializedObject.FindProperty( "readWriteEnabled" );
			generateMipMapsProp = serializedObject.FindProperty( "generateMipMaps" );
			saveAsPNGProp = serializedObject.FindProperty( "saveAsPNG" );
			jpegQualityProp = serializedObject.FindProperty( "jpegQuality" );
		}

		public override void OnDisable()
		{
			if( textures != null )
			{
				for( int i = 0; i < textures.Length; i++ )
					DestroyImmediate( textures[i] );
			}

			base.OnDisable();
		}

		public override void OnInspectorGUI()
		{
			bool guiEnabled = GUI.enabled;
			GUI.enabled = false;
			EditorGUILayout.PropertyField( originalSizeProp );
			GUI.enabled = guiEnabled;

			EditorGUILayout.PropertyField( scaledSizeProp );
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField( preserveAspectRatioProp );
			EditorGUI.indentLevel--;

			EditorGUILayout.PropertyField( paddingProp );
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField( paddingColorProp );
			EditorGUI.indentLevel--;

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField( readWriteEnabledProp );
			EditorGUILayout.PropertyField( generateMipMapsProp );

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField( saveAsPNGProp );
			if( saveAsPNGProp.hasMultipleDifferentValues || !saveAsPNGProp.boolValue )
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField( jpegQualityProp );
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			EditorGUILayout.HelpBox( fileSizeInfoString, MessageType.None );

			ApplyRevertGUI();
		}

		public override bool HasPreviewGUI()
		{
			return true;
		}

		public override void OnPreviewSettings()
		{
			previewShowBackground = GUILayout.Toggle( previewShowBackground, previewShowBackground ? "Background: Visible" : "Background: Hidden", EditorStyles.toolbarButton );
		}

		public override void OnPreviewGUI( Rect r, GUIStyle background )
		{
			GUI.Label( r, "", background );

			int index = System.Array.IndexOf( runtimeTextures, target );
			if( index >= 0 )
			{
				// Preserve texture aspect ratio and while displaying the padding area
				Texture2D texture = textures[index];
				float aspectRatio = (float) texture.width / texture.height;
				float width = aspectRatio * r.height;
				if( width <= r.width )
				{
					r.x += ( r.width - width ) * 0.5f;
					r.width = width;
				}
				else
				{
					float height = r.width / aspectRatio;
					r.y += ( r.height - height ) * 0.5f;
					r.height = height;
				}

				if( previewShowBackground )
				{
					Color c = GUI.color;
					GUI.color = new Color( 1f, 1f, 1f, 0.5f );
					GUI.DrawTexture( r, Texture2D.whiteTexture );
					GUI.color = c;
				}

				GUI.DrawTexture( r, textures[index] );
			}
		}

		private string GetReadableFilesize( long bytes )
		{
			if( bytes >= 1024 * 1024 )
				return ( (double) bytes / ( 1024 * 1024 ) ).ToString( "F2" ) + "MB";
			if( bytes >= 1024 )
				return ( (double) bytes / 1024 ).ToString( "F2" ) + "KB";

			return bytes + " bytes";
		}
	}
}