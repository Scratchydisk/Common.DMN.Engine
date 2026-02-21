using Microsoft.VisualStudio.TestTools.UnitTesting;
using net.adamec.lib.common.dmn.engine.engine.execution.context;

namespace net.adamec.lib.common.dmn.engine.test.complex
{
    [TestClass]
    [TestCategory("Complex tests")]
    public class SfeelExpressionsTests14 : SfeelExpressionsTests13Ext
    {
        protected override SourceEnum Source => SourceEnum.File14;

        private static DmnExecutionContext ctxFile14;
        protected override DmnExecutionContext Ctx => ctxFile14;

        [ClassInitialize]
        public static void InitCtxFile14(TestContext testContext)
        {
            ctxFile14 = CTX("sfeel.dmn", SourceEnum.File14);
        }
    }
}
