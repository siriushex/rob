using System;

namespace DragonWater.Attributes
{
    internal class DragonShaderStrippableAttribute : Attribute
    {
        public string Shader { get; private set; }
        public string Description { get; private set; }

        public DragonShaderStrippableAttribute(string shader, string description)
        {
            Shader = shader;
            Description = description;
        }
    }
}
