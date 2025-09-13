using osu.Framework.Testing;

namespace storytor.Game.Tests.Visual
{
    public abstract partial class storytorTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new storytorTestSceneTestRunner();

        private partial class storytorTestSceneTestRunner : storytorGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }
    }
}
