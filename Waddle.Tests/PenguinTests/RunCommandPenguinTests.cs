using System.Text.RegularExpressions;
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
        [TestCase("echo Hello World!", "Hello World!\n")]
        [TestCase("echo World Hello!", "World Hello!\n")]
        [TestCase("echo Hello World! && echo World Hello!", "Hello World!\nWorld Hello!\n")]
        public async Task TestOutput(string cmd, string expectedOutput)
        {
            RunCommandPenguin p = new(Utils.ClientOnlyContext)
            {
                Command = cmd,
                Name = "RunCommandPenguinTest",
            };

            Assert.That(async () => await p.Execute(CancellationToken.None), Throws.Nothing);
            Assert.That(
                Utils.ReadAllClientOutput(Utils.ClientOnlyContext),
                Is.EqualTo(expectedOutput)
            );
        }

        [Test]
        [TestCase(
            "echo hi",
            new string[] { "sh", "-c" },
            "hi\n",
            IncludePlatform = "Linux, MacOsX, Unix",
            TestName = "Explicitly defined shell: sh"
        )]
        [TestCase(
            "echo hi",
            new string[] { "bash", "-c" },
            "hi\n",
            IncludePlatform = "Linux, MacOsX, Unix",
            TestName = "Explicitly defined shell: bash"
        )]
        [TestCase(
            "echo hi",
            new string[] { "cmd.exe", "/c" },
            "hi\n",
            IncludePlatform = "Win",
            TestName = "Explicitly defined shell: cmd.exe"
        )]
        [TestCase(
            "echo hi",
            new string[] { "PowerShell", "/c" },
            "hi\n",
            IncludePlatform = "Win",
            TestName = "Explicitly defined shell: PowerShell"
        )]
        public async Task TestDifferentShell(string cmd, string[] shell, string expected)
        {
            RunCommandPenguin p = new(Utils.ClientOnlyContext)
            {
                Command = cmd,
                Name = "RunCommandPenguinTest",
                Shell = [.. shell],
            };

            Assert.That(async () => await p.Execute(CancellationToken.None), Throws.Nothing);
            Assert.That(Utils.ReadAllClientOutput(Utils.ClientOnlyContext), Is.EqualTo(expected));
        }

        [Test]
        [TestCase("abhiuewakabawehuai", typeof(CommandException), TestName = "Command not found")]
        [TestCase(
            "echo 'non zero exitcode' >&2; false",
            typeof(CommandException),
            TestName = "Non-zero exitcode Linux",
            IncludePlatform = "Linux, MacOsX, Unix"
        )]
        [TestCase(
            "1>&2 echo errorr && exit 1",
            typeof(CommandException),
            TestName = "Non-zero exitcode Windows",
            IncludePlatform = "Win"
        )]
        public async Task TestExceptions(string cmd, Type expected)
        {
            RunCommandPenguin p = new(Utils.ClientOnlyContext)
            {
                Command = cmd,
                Name = "RunCommandPenguinTest",
            };
            Assert.That(
                async () => await p.Execute(CancellationToken.None),
                Throws.InstanceOf(expected)
            );
        }
    }
}
