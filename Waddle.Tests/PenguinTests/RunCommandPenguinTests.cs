using NUnit.Framework.Constraints;
using Penguins.ClientPenguins;
using Waddle.Config;

namespace Waddle.Tests.PenguinTests
{
    public class RunCommandPenguinTests
    {
        [SetUp]
        public void Setup()
        {
            Utils.ResetClientOutputStream();
        }

        [Test]
        [Platform("Linux, MacOsX")]
        [TestCase("echo Hello World!", "Hello World!\n")]
        [TestCase("asdfasdfasdf", ": line 1: asdfasdfasdf: command not found\n")]
        public async Task TestOutput(string cmd, string expectedOutput)
        {
            RunCommandPenguin p = new(Utils.ClientOnlyContext)
            {
                Command = cmd,
                Name = "RunCommandPenguinTest",
            };

            try
            {
                await p.Execute(CancellationToken.None);
            }
            catch (CommandException) { }

            Assert.That(
                Utils.ReadAllClientOutput(Utils.ClientOnlyContext),
                Does.EndWith(expectedOutput)
            );
        }
    }
}
