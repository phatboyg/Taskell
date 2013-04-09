namespace Taskell.Tests
{
    using NUnit.Framework;


    [TestFixture]
    public class Using_a_delay
    {
        [Test]
        public void Should_delay_then_execute()
        {
            bool called = false;

            var composer = new TaskComposer<int>();

            composer.Delay(100);
            composer.Execute(() => called = true);

            composer.Finish().Wait();

            Assert.IsTrue(called);
        }
    }
}
