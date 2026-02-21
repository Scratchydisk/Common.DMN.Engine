using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace net.adamec.lib.common.dmn.engine.test.complex
{
    [TestClass]
    [TestCategory("Complex tests")]
    public class CircularReferenceTests15 : CircularReferenceTests14
    {
        protected override SourceEnum Source => SourceEnum.File15;
    }
}
