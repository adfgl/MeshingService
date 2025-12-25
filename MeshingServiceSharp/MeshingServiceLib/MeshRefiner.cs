using System;
using System.Collections.Generic;
using System.Text;

namespace MeshingServiceLib
{
    public sealed class MeshRefiner(Mesh mesh)
    {
        public MeshRefiner Refine()
        {
            return this;
        }
    }
}
