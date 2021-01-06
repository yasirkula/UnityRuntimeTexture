using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using SC = System.StringComparison;

namespace RuntimeTextureNamespace
{
	[ScriptedImporter( 1, EXTENSION )]
	public class RuntimeTextureImporter : ScriptedImporter
	{
		private const int ICON_SIZE = 64;

		public const string EXTENSION = "img";
		public const string EXTENSION_WITH_PERIOD = ".img";

#pragma warning disable 0649
		[SerializeField]
		private Vector2Int originalSize;
		[SerializeField]
		private Vector2Int scaledSize;
		[SerializeField]
		private Vector2Int padding;

		[SerializeField]
		private Color paddingColor;

		[SerializeField]
		private bool preserveAspectRatio = true;

		[SerializeField]
		private bool readWriteEnabled = false;
		[SerializeField]
		private bool generateMipMaps = true;
		[SerializeField]
		private TextureWrapMode wrapMode = TextureWrapMode.Repeat;
		[SerializeField]
		private FilterMode filterMode = FilterMode.Bilinear;
		[SerializeField]
		[Range( 0, 16 )]
		private int anisoLevel = 1;

		[SerializeField]
		private bool saveAsPNG = true;
		[SerializeField]
		[Range( 0, 100 )]
		private int jpegQuality = 100;
#pragma warning restore 0649

		public override void OnImportAsset( AssetImportContext ctx )
		{
			byte[] bytes = File.ReadAllBytes( ctx.assetPath );
			Texture2D tex = new Texture2D( 2, 2, saveAsPNG ? TextureFormat.RGBA32 : TextureFormat.RGB24, false );
			tex.LoadImage( bytes );

			originalSize = new Vector2Int( tex.width, tex.height );

			if( scaledSize.x < 0 || scaledSize.y < 0 )
			{
				LogImportError( ctx, "Scaled Size can't be smaller than 0!" );
				scaledSize = new Vector2Int( 0, 0 );
			}

			if( scaledSize.x == 0 )
				scaledSize.x = originalSize.x;
			if( scaledSize.y == 0 )
				scaledSize.y = originalSize.y;

			if( padding.x < 0 || padding.y < 0 )
			{
				LogImportError( ctx, "Padding can't be smaller than 0!" );
				padding = new Vector2Int( 0, 0 );
			}
			else if( padding.x >= scaledSize.x || padding.y >= scaledSize.y )
			{
				LogImportError( ctx, "Padding can't be greater than or equal to Scaled Size!" );
				padding = new Vector2Int( 0, 0 );
			}

			// Image has alpha channel if it is saved as PNG and either it has transparent padding or its pixels aren't all opaque
			bool hasAlphaChannel = saveAsPNG;
			if( hasAlphaChannel && ( ( padding.x == 0 && padding.y == 0 ) || paddingColor.a >= 1f ) )
			{
				bool hasTransparentPixels = false;
				Color32[] pixels = tex.GetPixels32();
				for( int i = 0; i < pixels.Length; i++ )
				{
					if( pixels[i].a < 255 )
					{
						hasTransparentPixels = true;
						break;
					}
				}

				if( !hasTransparentPixels )
					hasAlphaChannel = false;
			}

			TextureFormat textureFormat = hasAlphaChannel ? TextureFormat.RGBA32 : TextureFormat.RGB24;

			if( scaledSize != originalSize || padding.x != 0 || padding.y != 0 )
			{
				if( !preserveAspectRatio )
					ResizeTexture( ref tex, textureFormat, scaledSize, padding, paddingColor );
				else
				{
					float aspectRatio = (float) originalSize.x / originalSize.y;

					Vector2Int imageContentsSize = scaledSize - padding;
					int targetWidth = Mathf.RoundToInt( imageContentsSize.y * aspectRatio );
					int targetHeight = Mathf.RoundToInt( imageContentsSize.x / aspectRatio );

					if( targetWidth <= imageContentsSize.x )
						imageContentsSize.x = targetWidth;
					else if( targetHeight <= imageContentsSize.y )
						imageContentsSize.y = targetHeight;
					else
					{
						if( (float) targetWidth / imageContentsSize.x < (float) targetHeight / imageContentsSize.y )
							imageContentsSize.x = targetWidth;
						else
							imageContentsSize.y = targetHeight;
					}

					ResizeTexture( ref tex, textureFormat, scaledSize, scaledSize - imageContentsSize, paddingColor );
				}

				bytes = saveAsPNG ? tex.EncodeToPNG() : tex.EncodeToJPG( Mathf.Clamp( jpegQuality, 1, 100 ) );
			}
			else
			{
				// Check if encoding the image as JPEG or PNG will produce a smaller image file
				byte[] bytes2 = saveAsPNG ? tex.EncodeToPNG() : tex.EncodeToJPG( Mathf.Clamp( jpegQuality, 1, 100 ) );
				if( bytes2.Length < bytes.Length )
					bytes = bytes2;
			}

			// Generate icon
			ResizeTexture( ref tex, textureFormat, new Vector2Int( ICON_SIZE, ICON_SIZE ), new Vector2Int( 0, 0 ), new Color( 0, 0, 0, 0 ) );

			RuntimeTexture runtimeTexture = ScriptableObject.CreateInstance<RuntimeTexture>();
			( (RuntimeTexture.IEditorInterface) runtimeTexture ).Initialize( bytes, scaledSize, hasAlphaChannel, generateMipMaps, readWriteEnabled, wrapMode, filterMode, anisoLevel );

#if UNITY_2017_3_OR_NEWER
			ctx.AddObjectToAsset( "main obj", runtimeTexture, tex );
			ctx.SetMainObject( runtimeTexture );
#else
			ctx.SetMainAsset( "main obj", runtimeTexture, tex );
#endif
		}

		private void LogImportError( AssetImportContext ctx, string error )
		{
#if UNITY_2018_1_OR_NEWER
			ctx.LogImportError( error );
#else
			Debug.LogError( error );
#endif
		}

		[MenuItem( "Assets/Convert To Runtime Texture (" + EXTENSION_WITH_PERIOD + ")", priority = 400 )]
		private static void ConvertToRuntimeTexture()
		{
			Texture[] selection = Selection.GetFiltered<Texture>( SelectionMode.Assets );
			List<string> runtimeTexturePaths = new List<string>();
			try
			{
				AssetDatabase.StartAssetEditing();

				for( int i = 0; i < selection.Length; i++ )
				{
					string path = AssetDatabase.GetAssetPath( selection[i] );
					if( string.IsNullOrEmpty( path ) )
						continue;

					byte[] fileContents = null;
					if( !path.EndsWith( ".png", SC.OrdinalIgnoreCase ) && !path.EndsWith( ".jpeg", SC.OrdinalIgnoreCase ) && !path.EndsWith( ".jpg", SC.OrdinalIgnoreCase ) )
					{
						// Can't load e.g. a PSD image via Texture2D.LoadImage, save the image as PNG in this case
						Texture2D originalTexture = selection[i] as Texture2D;
						if( !originalTexture )
							continue;

						ResizeTexture( ref originalTexture, TextureFormat.RGBA32, new Vector2Int( originalTexture.width, originalTexture.height ), new Vector2Int( 0, 0 ), new Color( 0f, 0f, 0f, 0f ) );
						fileContents = originalTexture.EncodeToPNG();
					}

					string runtimeTexturePath = Path.ChangeExtension( path, EXTENSION_WITH_PERIOD );
					if( fileContents == null )
						File.Copy( path, runtimeTexturePath, true );
					else
						File.WriteAllBytes( runtimeTexturePath, fileContents );

					runtimeTexturePaths.Add( runtimeTexturePath );
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh();
			}

			// Update Unity's selection
			if( runtimeTexturePaths.Count > 0 )
			{
				Object[] newSelection = new Object[runtimeTexturePaths.Count];
				for( int i = 0; i < runtimeTexturePaths.Count; i++ )
					newSelection[i] = AssetDatabase.LoadAssetAtPath<RuntimeTexture>( runtimeTexturePaths[i] );

				Selection.objects = newSelection;
			}
		}

		[MenuItem( "Assets/Convert To Runtime Texture (" + EXTENSION_WITH_PERIOD + ")", validate = true )]
		private static bool ConvertToRuntimeTextureValidate()
		{
			return Selection.GetFiltered<Texture>( SelectionMode.Assets ).Length > 0;
		}

		private static void ResizeTexture( ref Texture2D texture, TextureFormat format, Vector2Int size, Vector2Int padding, Color clearColor )
		{
			Texture2D result = null;

			RenderTexture rt = RenderTexture.GetTemporary( size.x - padding.x, size.y - padding.y );
			RenderTexture activeRT = RenderTexture.active;

			try
			{
				Graphics.Blit( texture, rt );
				RenderTexture.active = rt;

				result = new Texture2D( size.x, size.y, format, false );
				if( padding.x > 0 || padding.y > 0 )
				{
					Color32[] pixels = new Color32[result.width * result.height];
					Color32 clearPixel = clearColor;
					for( int i = 0; i < pixels.Length; i++ )
						pixels[i] = clearPixel;

					result.SetPixels32( pixels );
				}

				result.ReadPixels( new Rect( 0, 0, rt.width, rt.height ), padding.x / 2, padding.y / 2, false );
				result.Apply( false, false );
			}
			catch( System.Exception e )
			{
				Debug.LogException( e );

				DestroyImmediate( result );
				result = null;
			}
			finally
			{
				RenderTexture.active = activeRT;
				RenderTexture.ReleaseTemporary( rt );

				if( !EditorUtility.IsPersistent( texture ) )
					DestroyImmediate( texture );
			}

			texture = result;
		}
	}
}