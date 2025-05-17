#if UNITY_EDITOR
using DragonWater.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater.Editor.Build
{
    internal class DragonShaderStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader.name == Constants.Shader.WaterShaderName && snippet.shaderType == ShaderType.Fragment)
            {
                var before = data.Count;
                StripShaderVariants(shader, ref data, ref DragonWaterManager.Instance.Config.buildWaterShaderStripping);
                var after = data.Count;
                DragonWaterDebug.Log($"[Shader Stripper] Before: {before};  After: {after};  Difference: {before-after}");
            }
        }

        void StripShaderVariants(Shader shader, ref IList<ShaderCompilerData> data, ref List<ShaderKeywordStrip> stripping)
        {
            var localStripping = stripping.Select(v => { v.localKeyword = new(shader, v.keyword); return v; }).ToList();

            for (int i = 0; i < data.Count; i++)
            {
                for (int j = 0; j<localStripping.Count; j++)
                {
                    if (ShouldStrip(data[i], localStripping[j]))
                    {
                        data.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
        }

        bool ShouldStrip(ShaderCompilerData variant, ShaderKeywordStrip strip)
        {
            if (strip.mode == KeywordStrippingMode.AlwaysInclude)
            {
                return !variant.shaderKeywordSet.IsEnabled(strip.localKeyword);
            }
            else if (strip.mode == KeywordStrippingMode.NeverInclude)
            {
                return variant.shaderKeywordSet.IsEnabled(strip.localKeyword);
            }
            else
            {
                return false;
            }
        }
    }
}
#endif