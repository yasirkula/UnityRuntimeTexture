using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace RuntimeTextureNamespace
{
    [ScriptedImporter(1, new string[0], new string[] { "png", "jpg", "jpeg" })]
	public class RuntimeTextureImporter : ScriptedImporter
	{
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
		private bool exportAsPNG = true;
		[SerializeField]
		[Range( 0, 100 )]
		private int jpegQuality = 100;

		public override void OnImportAsset( AssetImportContext ctx )
		{
            byte[] bytes = File.ReadAllBytes(ctx.assetPath);
            Texture2D tex = new Texture2D(2, 2, exportAsPNG ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tex.LoadImage(bytes);

			originalSize = new Vector2Int( tex.width, tex.height );

			if( scaledSize.x < 0 || scaledSize.y < 0 )
			{
                ctx.LogImportError("Scaled Size can't be smaller than 0!");
				scaledSize = new Vector2Int( 0, 0 );
			}

			if( scaledSize.x == 0 )
				scaledSize.x = originalSize.x;
			if( scaledSize.y == 0 )
				scaledSize.y = originalSize.y;

			if( padding.x < 0 || padding.y < 0 )
			{
                ctx.LogImportError("Padding can't be smaller than 0!");
				padding = new Vector2Int( 0, 0 );
			}
			else if( padding.x >= scaledSize.x || padding.y >= scaledSize.y )
			{
                ctx.LogImportError("Padding can't be greater than or equal to Scaled Size!");
				padding = new Vector2Int( 0, 0 );
			}

            // Image has alpha channel if it is saved as PNG and either it has transparent padding or its pixels aren't all opaque
            bool hasAlphaChannel = exportAsPNG && ((padding.sqrMagnitude > 0 && paddingColor.a < 1f) || Array.FindIndex(tex.GetPixels32(0), (pixel) => pixel.a < 255) >= 0);
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

				bytes = exportAsPNG ? tex.EncodeToPNG() : tex.EncodeToJPG( Mathf.Clamp( jpegQuality, 1, 100 ) );
			}
			else
			{
				// Check if encoding the image as JPEG or PNG will produce a smaller image file
				byte[] bytes2 = exportAsPNG ? tex.EncodeToPNG() : tex.EncodeToJPG( Mathf.Clamp( jpegQuality, 1, 100 ) );
				if( bytes2.Length < bytes.Length )
					bytes = bytes2;
			}

            // Generate icon
            ResizeTexture(ref tex, textureFormat, new Vector2Int(64, 64), Vector2Int.zero, Color.clear);

            RuntimeTexture runtimeTexture = ScriptableObject.CreateInstance<RuntimeTexture>();
            runtimeTexture.bytes = bytes;
            runtimeTexture.Width = scaledSize.x;
            runtimeTexture.Height = scaledSize.y;
            runtimeTexture.HasAlphaChannel = hasAlphaChannel;
            runtimeTexture.GenerateMipMaps = generateMipMaps;
            runtimeTexture.ReadWriteEnabled = readWriteEnabled;
            runtimeTexture.WrapMode = wrapMode;
            runtimeTexture.FilterMode = filterMode;
            runtimeTexture.AnisoLevel = Mathf.Clamp(anisoLevel, 0, 16);

			ctx.AddObjectToAsset( "main obj", runtimeTexture, tex );
			ctx.SetMainObject( runtimeTexture );
		}

		internal static void ResizeTexture( ref Texture2D texture, TextureFormat format, Vector2Int size, Vector2Int padding, Color clearColor )
		{
			Texture2D result = null;

			RenderTexture rt = RenderTexture.GetTemporary( size.x - padding.x, size.y - padding.y );
			RenderTexture activeRT = RenderTexture.active;

			try
			{
				Graphics.Blit( texture, rt );
				RenderTexture.active = rt;

				result = new Texture2D( size.x, size.y, format, false );
                if (padding.sqrMagnitude > 0)
                {
                    Color32[] pixels = new Color32[result.width * result.height];
                    Array.Fill(pixels, clearColor);
                    result.SetPixels32(pixels);
                }

				result.ReadPixels( new Rect( 0, 0, rt.width, rt.height ), padding.x / 2, padding.y / 2, false );
				result.Apply( false, false );
			}
            catch (Exception e)
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
