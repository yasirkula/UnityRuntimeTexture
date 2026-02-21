using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("RuntimeTexture.Editor")]
public class RuntimeTexture : ScriptableObject
{
    private Texture2D m_texture;
    public Texture2D Texture
    {
        get
        {
            if (!m_texture)
                m_texture = CreateTexture(!ReadWriteEnabled);

            return m_texture;
        }
    }

    [SerializeField]
    internal byte[] bytes;

    [field: SerializeField]
    public int Width { get; internal set; }
    [field: SerializeField]
    public int Height { get; internal set; }

    [field: SerializeField]
    public bool HasAlphaChannel { get; internal set; }

    [field: SerializeField]
    public bool ReadWriteEnabled { get; set; }
    [field: SerializeField]
    public bool GenerateMipMaps { get; set; }
    [field: SerializeField]
    public TextureWrapMode WrapMode { get; set; }
    [field: SerializeField]
    public FilterMode FilterMode { get; set; }
    [field: SerializeField]
    public int AnisoLevel { get; set; }

    internal Texture2D CreateTexture(bool markNonReadable)
    {
        Texture2D result = new Texture2D(2, 2, HasAlphaChannel ? TextureFormat.RGBA32 : TextureFormat.RGB24, GenerateMipMaps)
        {
            wrapMode = WrapMode,
            filterMode = FilterMode,
            anisoLevel = AnisoLevel
        };

        result.LoadImage(bytes, markNonReadable);
        return result;
    }

    public bool DestroyTexture()
    {
        if (m_texture != null)
        {
            DestroyImmediate(m_texture);
            m_texture = null;
            return true;
        }

        return false;
    }
}
