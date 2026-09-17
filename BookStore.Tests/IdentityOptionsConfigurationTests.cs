using BookStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using NUnit.Framework;

namespace BookStore.Tests;

[TestFixture]
public sealed class IdentityOptionsConfigurationTests
{
    [Test]
    public void Configure_RequiresSixCharacterPassword()
    {
        var options = new IdentityOptions();

        IdentityOptionsConfiguration.Configure(options);

        Assert.That(options.Password.RequiredLength, Is.EqualTo(6));
    }
}
