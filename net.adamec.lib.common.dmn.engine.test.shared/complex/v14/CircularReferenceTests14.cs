using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace net.adamec.lib.common.dmn.engine.test.complex
{
    [TestClass]
    [TestCategory("Complex tests")]
    public class CircularReferenceTests14 : CircularReferenceTests13Ext
    {
        protected override SourceEnum Source => SourceEnum.File14;
    }
}
