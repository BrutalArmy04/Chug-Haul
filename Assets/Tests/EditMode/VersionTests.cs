using NUnit.Framework;
using ChugHaul.Core;

namespace ChugHaul.Core.Tests
{
    public class VersionTests
    {
        [Test]
        public void VersionValueIsNotEmpty()
        {
            Assert.That(Version.Value, Is.Not.Empty);
        }
    }
}