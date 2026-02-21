using Microsoft.VisualStudio.TestTools.UnitTesting;
using net.adamec.lib.common.dmn.engine.engine.execution.context;

namespace net.adamec.lib.common.dmn.engine.test.complex
{
    [TestClass]
    [TestCategory("Complex tests")]
    public class SfeelExpressionsTests15 : SfeelExpressionsTests14
    {
        protected override SourceEnum Source => SourceEnum.File15;

        private static DmnExecutionContext ctxFile15;
        protected override DmnExecutionContext Ctx => ctxFile15;

        [ClassInitialize]
        public static void InitCtxFile15(TestContext testContext)
        {
            ctxFile15 = CTX("sfeel.dmn", SourceEnum.File15);
        }
    }
}
