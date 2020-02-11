using UnityEngine;

public class RuntimeTexture : ScriptableObject
#if UNITY_EDITOR
	, RuntimeTexture.IEditorInterface
#endif
{
#if UNITY_EDITOR
	public interface IEditorInterface
	{
		long Length { get; }
		Texture2D Texture { get; }

		void Initialize( byte[] bytes, Vector2Int size, bool generateMipMaps, bool readWriteEnabled );
	}
#endif

#pragma warning disable 0649
	[SerializeField]
	[HideInInspector]
	private byte[] bytes;

	private Texture2D m_texture;
	public Texture2D Texture
	{
		get
		{
			if( !m_texture )
				m_texture = GetTexture( !readWriteEnabled );

			return m_texture;
		}
	}

	[SerializeField]
	[HideInInspector]
	private int m_width;
	public int Width { get { return m_width; } }

	[SerializeField]
	[HideInInspector]
	private int m_height;
	public int Height { get { return m_height; } }

	[SerializeField]
	private bool readWriteEnabled = false;
	[SerializeField]
	private bool generateMipMaps = true;
#pragma warning restore 0649

	private Texture2D GetTexture( bool markNonReadable )
	{
		Texture2D result = new Texture2D( 2, 2, TextureFormat.RGBA32, generateMipMaps );
		result.LoadImage( bytes, markNonReadable );

		return result;
	}

#if UNITY_EDITOR
	long IEditorInterface.Length { get { return bytes.LongLength; } }
	Texture2D IEditorInterface.Texture { get { return GetTexture( false ); } }

	void IEditorInterface.Initialize( byte[] bytes, Vector2Int size, bool generateMipMaps, bool readWriteEnabled )
	{
		this.bytes = bytes;
		this.m_width = size.x;
		this.m_height = size.y;
		this.generateMipMaps = generateMipMaps;
		this.readWriteEnabled = readWriteEnabled;
	}
#endif
}