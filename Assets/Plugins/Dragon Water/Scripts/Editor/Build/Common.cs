#if UNITY_EDITOR
using System;
using UnityEngine.Rendering;

namespace DragonWater.Editor.Build
{
    internal enum KeywordStrippingMode
    {
        Both,
        AlwaysInclude,
        NeverInclude,
    }

    [Serializable]
    internal struct ShaderKeywordStrip
    {
        public string keyword;
        public KeywordStrippingMode mode;

        [NonSerialized]
        public LocalKeyword localKeyword;
    }
}
#endif