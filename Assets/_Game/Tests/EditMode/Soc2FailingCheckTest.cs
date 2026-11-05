using NUnit.Framework;

namespace DungeonCrawler.Tests
{
    // SOC 2 CC8.1 test T3. this test fails on purpose so the "Run Unity tests" check fails,
    // to show branch protection blocks the merge. the PR it's in gets closed, never merged
    public class Soc2FailingCheckTest
    {
        [Test]
        public void Soc2_T3_FailsOnPurpose_SoTheMergeIsBlocked()
        {
            Assert.Fail("Intentional failure for SOC 2 CC8.1 test T3. This PR must not be merged.");
        }
    }
}
