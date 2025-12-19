using MeshingServiceLib;

namespace MeshingServiceTests
{
    public class TriangleIndexOfVertexTests
    {
        static Triangle MakeTriangle()
        {
            return new Triangle(
                index: 42,
                vtx0: 10, vtx1: 20, vtx2: 30,
                adj0: -1, adj1: -1, adj2: -1,
                con0: 0, con1: 0, con2: 0,
                state: TriangleState.Keep);
        }


        [Fact]
        public void IndexOf_Returns0_ForVtx0()
        {
            Triangle t = MakeTriangle();
            int idx = t.IndexOf(10);
            Assert.Equal(0, idx);
        }

        [Fact]
        public void IndexOf_Returns1_ForVtx1()
        {
            Triangle t = MakeTriangle();
            int idx = t.IndexOf(20);
            Assert.Equal(1, idx);
        }

        [Fact]
        public void IndexOf_Returns2_ForVtx2()
        {
            Triangle t = MakeTriangle();
            int idx = t.IndexOf(30);
            Assert.Equal(2, idx);
        }

        [Fact]
        public void IndexOf_ReturnsMinus1_ForMissingVertex()
        {
            Triangle t = MakeTriangle();
            int idx = t.IndexOf(999);
            Assert.Equal(-1, idx);
        }
    }
}
