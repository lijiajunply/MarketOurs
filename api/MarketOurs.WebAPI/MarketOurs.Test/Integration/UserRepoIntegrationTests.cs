using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class UserRepoIntegrationTests : IntegrationTestBase
{
    private IUserRepo _userRepo;
    private IDbContextFactory<MarketContext> _factory;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(TestAssemblySetup.DbConnectionString)
            .Options;
        
        _factory = new TestDbContextFactory(options);
        _userRepo = new UserRepo(_factory);

        // Clear database
        using var context = _factory.CreateDbContext();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
    }

    [Test]
    public async Task CreateAsync_ShouldGenerateHashIdAndPersist()
    {
        // Arrange
        var user = new UserModel 
        { 
            Name = "Integration Test User", 
            Email = "integration@test.com", 
            Phone = "123456789",
            Password = "hashed_password"
        };

        // Act
        await _userRepo.CreateAsync(user);

        // Assert
        Assert.That(user.Id, Is.Not.Null);
        var retrieved = await _userRepo.GetByIdAsync(user.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("Integration Test User"));
        Assert.That(retrieved.Email, Is.EqualTo("integration@test.com"));
    }

    [Test]
    public async Task GetByAccountAsync_ShouldFindUserByEmailOrPhone()
    {
        // Arrange
        var user = new UserModel { Name = "User 1", Email = "user1@test.com", Phone = "111", Password = "p" };
        await _userRepo.CreateAsync(user);

        // Act & Assert
        var byEmail = await _userRepo.GetByAccountAsync("user1@test.com");
        var byPhone = await _userRepo.GetByAccountAsync("111");

        Assert.That(byEmail?.Id, Is.EqualTo(user.Id));
        Assert.That(byPhone?.Id, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task UpdateAsync_ShouldModifyExistingUser()
    {
        // Arrange
        var user = new UserModel { Name = "Old Name", Email = "update@test.com", Password = "p" };
        await _userRepo.CreateAsync(user);

        // Act
        user.Name = "New Name";
        await _userRepo.UpdateAsync(user);

        // Assert
        var updated = await _userRepo.GetByIdAsync(user.Id);
        Assert.That(updated!.Name, Is.EqualTo("New Name"));
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}