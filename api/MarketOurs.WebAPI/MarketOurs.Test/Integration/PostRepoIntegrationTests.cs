using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.DataAPI.Repos;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace MarketOurs.Test.Integration;

[TestFixture]
[Category("Integration")]
public class PostRepoIntegrationTests : IntegrationTestBase
{
    private IPostRepo _postRepo;
    private IUserRepo _userRepo;
    private IDbContextFactory<MarketContext> _factory;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MarketContext>()
            .UseNpgsql(TestAssemblySetup.DbConnectionString)
            .Options;
        
        _factory = new TestDbContextFactory(options);
        _postRepo = new PostRepo(_factory);
        _userRepo = new UserRepo(_factory);

        // Clear database
        using var context = _factory.CreateDbContext();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"users\" CASCADE");
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE \"posts\" CASCADE");
    }

    [Test]
    public async Task CreateAsync_ShouldPersistPost()
    {
        // Arrange
        var user = new UserModel { Name = "Author", Email = "author@test.com", Password = "p" };
        await _userRepo.CreateAsync(user);

        var post = new PostModel { Title = "Title", Content = "Content", UserId = user.Id };

        // Act
        await _postRepo.CreateAsync(post);

        // Assert
        var retrieved = await _postRepo.GetByIdAsync(post.Id);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Title, Is.EqualTo("Title"));
        Assert.That(retrieved.UserId, Is.EqualTo(user.Id));
    }

    [Test]
    public async Task SetLikesAsync_ShouldUpdateCounterAndRelation()
    {
        // Arrange
        var author = new UserModel { Name = "Author", Email = "author@test.com", Password = "p" };
        await _userRepo.CreateAsync(author);
        
        var liker = new UserModel { Name = "Liker", Email = "liker@test.com", Password = "p" };
        await _userRepo.CreateAsync(liker);

        var post = new PostModel { Title = "T", Content = "C", UserId = author.Id };
        await _postRepo.CreateAsync(post);

        // Act
        await _postRepo.SetLikesAsync(liker, post.Id);

        // Assert
        var updatedPost = await _postRepo.GetByIdAsync(post.Id);
        Assert.That(updatedPost!.Likes, Is.EqualTo(1));
        
        var likeUsers = await _postRepo.GetLikeUsersAsync(post.Id);
        Assert.That(likeUsers, Is.Not.Null);
        Assert.That(likeUsers!.Any(u => u.Id == liker.Id), Is.True);
    }

    [Test]
    public async Task DeleteLikesAsync_ShouldUpdateCounterAndRelation()
    {
        // Arrange
        var author = new UserModel { Name = "Author", Email = "author@test.com", Password = "p" };
        await _userRepo.CreateAsync(author);
        
        var liker = new UserModel { Name = "Liker", Email = "liker@test.com", Password = "p" };
        await _userRepo.CreateAsync(liker);

        var post = new PostModel { Title = "T", Content = "C", UserId = author.Id };
        await _postRepo.CreateAsync(post);
        await _postRepo.SetLikesAsync(liker, post.Id);

        // Act
        await _postRepo.DeleteLikesAsync(post.Id, liker.Id);

        // Assert
        var updatedPost = await _postRepo.GetByIdAsync(post.Id);
        Assert.That(updatedPost!.Likes, Is.EqualTo(0));
        
        var likeUsers = await _postRepo.GetLikeUsersAsync(post.Id);
        Assert.That(likeUsers!.Any(u => u.Id == liker.Id), Is.False);
    }

    private class TestDbContextFactory(DbContextOptions<MarketContext> options) : IDbContextFactory<MarketContext>
    {
        public MarketContext CreateDbContext() => new MarketContext(options);
    }
}