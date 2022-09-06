// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using MonoGame.OpenGL;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class VertexDeclaration
    {
        private readonly Dictionary<int, VertexDeclarationAttributeInfo> _shaderAttributeInfo = new Dictionary<int, VertexDeclarationAttributeInfo>();

        internal VertexDeclarationAttributeInfo GetAttributeInfo(Shader shader, int programHash)
        {
            VertexDeclarationAttributeInfo attrInfo;
            if (_shaderAttributeInfo.TryGetValue(programHash, out attrInfo))
                return attrInfo;

            // Get the vertex attribute info and cache it
            attrInfo = new VertexDeclarationAttributeInfo(GraphicsDevice.MaxVertexAttributes);

            foreach (var ve in InternalVertexElements)
            {
                var attributeLocation = shader.GetAttribLocation(ve.VertexElementUsage, ve.UsageIndex);
                // XNA appears to ignore usages it can't find a match for, so we will do the same
                if (attributeLocation < 0)
                    continue;

                attrInfo.Elements.Add(new VertexDeclarationAttributeInfo.Element
                {
                    AttributeLocation = attributeLocation,
                    NumberOfElements = ve.VertexElementFormat.OpenGLNumberOfElements(),
                    VertexAttribPointerType = ve.VertexElementFormat.OpenGLVertexAttribPointerType(),
                    Normalized = ve.OpenGLVertexAttribNormalized(),
                    Offset = ve.Offset,
                });
                attrInfo.EnabledAttributes[attributeLocation] = true;
            }

            _shaderAttributeInfo.Add(programHash, attrInfo);
            return attrInfo;
        }


		internal void Apply(Shader shader, IntPtr offset, int programHash)
		{
            var attrInfo = GetAttributeInfo(shader, programHash);

            // Apply the vertex attribute info
            for (int i=0; i< attrInfo.Elements.Count; i++)
            {
                var element = attrInfo.Elements[i];
                GL.VertexAttribPointer(element.AttributeLocation,
                    element.NumberOfElements,
                    element.VertexAttribPointerType,
                    element.Normalized,
                    VertexStride,
                    (IntPtr)(offset.ToInt64() + element.Offset));
                GraphicsExtensions.CheckGLError();

#if !(GLES || MONOMAC)
                if (GraphicsDevice.GraphicsCapabilities.SupportsInstancing)
                {
                    GL.VertexAttribDivisor(element.AttributeLocation, 0);
                    GraphicsExtensions.CheckGLError();
                }
#endif
            }
            GraphicsDevice.SetVertexAttributeArray(attrInfo.EnabledAttributes);
		    GraphicsDevice._attribsDirty = true;
		}

        /// <summary>
        /// Vertex attribute information for a particular shader/vertex declaration combination.
        /// </summary>
        internal class VertexDeclarationAttributeInfo
        {
            internal bool[] EnabledAttributes;

            internal class Element
            {
                public int AttributeLocation;
                public int NumberOfElements;
                public VertexAttribPointerType VertexAttribPointerType;
                public bool Normalized;
                public int Offset;
            }

            internal List<Element> Elements;

            internal VertexDeclarationAttributeInfo(int maxVertexAttributes)
            {
                EnabledAttributes = new bool[maxVertexAttributes];
                Elements = new List<Element>();
            }
        }
    }
}
