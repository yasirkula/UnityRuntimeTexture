using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace RuntimeTextureNamespace
{
    [CustomEditor(typeof(RuntimeTextureImporter))]
    [CanEditMultipleObjects]
    public class RuntimeTextureImporterEditor : ScriptedImporterEditor
	{
		private SerializedProperty originalSizeProp;
		private SerializedProperty scaledSizeProp;
		private SerializedProperty preserveAspectRatioProp;
		private SerializedProperty paddingProp;
		private SerializedProperty paddingColorProp;
		private SerializedProperty readWriteEnabledProp;
		private SerializedProperty generateMipMapsProp;
		private SerializedProperty wrapModeProp;
		private SerializedProperty filterModeProp;
		private SerializedProperty anisoLevelProp;
		private SerializedProperty exportAsPNGProp;
		private SerializedProperty jpegQualityProp;

		public override bool showImportedObject { get { return false; } }

		public override void OnEnable()
		{
			base.OnEnable();

			originalSizeProp = serializedObject.FindProperty( "originalSize" );
			scaledSizeProp = serializedObject.FindProperty( "scaledSize" );
			preserveAspectRatioProp = serializedObject.FindProperty( "preserveAspectRatio" );
			paddingProp = serializedObject.FindProperty( "padding" );
			paddingColorProp = serializedObject.FindProperty( "paddingColor" );
			readWriteEnabledProp = serializedObject.FindProperty( "readWriteEnabled" );
			generateMipMapsProp = serializedObject.FindProperty( "generateMipMaps" );
			wrapModeProp = serializedObject.FindProperty( "wrapMode" );
			filterModeProp = serializedObject.FindProperty( "filterMode" );
			anisoLevelProp = serializedObject.FindProperty( "anisoLevel" );
            exportAsPNGProp = serializedObject.FindProperty("exportAsPNG");
			jpegQualityProp = serializedObject.FindProperty( "jpegQuality" );
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField( originalSizeProp );
            EditorGUI.EndDisabledGroup();

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
			EditorGUILayout.PropertyField( wrapModeProp );
			EditorGUILayout.PropertyField( filterModeProp );
			EditorGUILayout.PropertyField( anisoLevelProp );

			// Source: https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/Inspector/TextureInspector.cs#L502-L511
			if( anisoLevelProp.intValue > 1 )
			{
				if( QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable )
					EditorGUILayout.HelpBox( "Anisotropic filtering is disabled for all textures in Quality Settings.", MessageType.Info );
				else if( QualitySettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable )
					EditorGUILayout.HelpBox( "Anisotropic filtering is enabled for all textures in Quality Settings.", MessageType.Info );
			}

			EditorGUILayout.Space();

            EditorGUILayout.PropertyField(exportAsPNGProp);
            if (exportAsPNGProp.hasMultipleDifferentValues || !exportAsPNGProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(jpegQualityProp);
                EditorGUI.indentLevel--;
            }

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();

			ApplyRevertGUI();
		}
	}
}
