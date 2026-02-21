using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace net.adamec.lib.common.dmn.engine.test.complex
{
    [TestClass]
    [TestCategory("Complex tests")]
    public class EndToEndTests14 : EndToEndTests13Ext
    {
        protected override SourceEnum Source => SourceEnum.File14;
    }
}
