using Game.Core.Domain;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class AppIdentityTests
    {
        [Test]
        public void StoreNameAndAndroidPackageAreMemorizeWayHome()
        {
            Assert.That(AppIdentity.ProductName, Is.EqualTo("memorizewayhome"));
            Assert.That(AppIdentity.AndroidPackage, Is.EqualTo("com.nixin.memorizewayhome"));
        }
    }
}
