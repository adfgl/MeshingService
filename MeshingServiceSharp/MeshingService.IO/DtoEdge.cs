using System;
using System.Collections.Generic;
using System.Text;

namespace MeshingService.IO
{
    public sealed class DtoEdge
    {
        public string? Id { get; set; }
        public DtoVertex Start { get; set; }
        public DtoVertex End { get; set; }
    }
}
