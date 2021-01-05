using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts.Core.UI
{
    public class RuntimeTextureUI : MonoBehaviour
    {
        [SerializeField] private RawImage rawImage = null;
        [SerializeField] private RuntimeTexture runtimeImage = null;

        private void Awake()
        {
            rawImage.texture = runtimeImage.Texture;
        }
    }
}
