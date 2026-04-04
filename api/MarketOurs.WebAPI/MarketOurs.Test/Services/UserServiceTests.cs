using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Repos;
using MarketOurs.DataAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace MarketOurs.Test.Services;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUserRepo> _mockUserRepo;
    private Mock<IEmailService> _mockEmailService;
    private Mock<ILogger<UserService>> _mockLogger;
    private Mock<IConnectionMultiplexer> _mockRedis;
    private Mock<IDatabase> _mockDatabase;
    private UserService _userService;

    [SetUp]
    public void Setup()
    {
        _mockUserRepo = new Mock<IUserRepo>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

        var redisList = new List<IConnectionMultiplexer> { _mockRedis.Object };

        _userService = new UserService(
            _mockUserRepo.Object,
            _mockEmailService.Object,
            redisList,
            _mockLogger.Object
        );
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<UserModel>
        {
            new UserModel { Id = "1", Name = "Test 1", Email = "test1@test.com" },
            new UserModel { Id = "2", Name = "Test 2", Email = "test2@test.com" }
        };
        _mockUserRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _userService.GetAllAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0].Id, Is.EqualTo("1"));
        Assert.That(result[1].Id, Is.EqualTo("2"));
    }

    [Test]
    public async Task GetByIdAsync_WhenUserExists_ShouldReturnUserDto()
    {
        // Arrange
        var user = new UserModel { Id = "1", Name = "Test User" };
        _mockUserRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);

        // Act
        var result = await _userService.GetByIdAsync("1");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("1"));
        Assert.That(result.Name, Is.EqualTo("Test User"));
    }

    [Test]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync((UserModel?)null);

        // Act
        var result = await _userService.GetByIdAsync("1");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithValidCredentialsAndActiveUser_ShouldReturnUserDto()
    {
        // Arrange
        var passwordHash = "password".StringToHash();
        var user = new UserModel { Id = "1", Password = passwordHash, IsActive = true };
        _mockUserRepo.Setup(r => r.GetByAccountAsync("test")).ReturnsAsync(user);

        // Act
        var result = await _userService.LoginAsync("test", "password");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("1"));
    }

    [Test]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnNull()
    {
        // Arrange
        var passwordHash = "password".StringToHash();
        var user = new UserModel { Id = "1", Password = passwordHash, IsActive = true };
        _mockUserRepo.Setup(r => r.GetByAccountAsync("test")).ReturnsAsync(user);

        // Act
        var result = await _userService.LoginAsync("test", "wrongpassword");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void LoginAsync_WithLockedUser_ShouldThrowAuthException()
    {
        // Arrange
        var passwordHash = "password".StringToHash();
        var user = new UserModel { Id = "1", Password = passwordHash, IsActive = false };
        _mockUserRepo.Setup(r => r.GetByAccountAsync("test")).ReturnsAsync(user);

        // Act & Assert
        var ex = Assert.ThrowsAsync<AuthException>(async () => await _userService.LoginAsync("test", "password"));
        Assert.That(ex.Message, Does.Contain("账号已被锁定"));
    }

    [Test]
    public async Task CreateAsync_WithEmailAccount_ShouldCreateAndReturnUserDto()
    {
        // Arrange
        var createDto = new UserCreateDto { Account = "test@example.com", Password = "password", Name = "Test", Role = "User" };
        UserModel? createdUser = null;

        _mockUserRepo.Setup(r => r.CreateAsync(It.IsAny<UserModel>()))
            .Callback<UserModel>(u => createdUser = u)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(createdUser, Is.Not.Null);
        Assert.That(createdUser!.Email, Is.EqualTo("test@example.com"));
        Assert.That(createdUser.Phone, Is.Empty);
        Assert.That(createdUser.Name, Is.EqualTo("Test"));
        Assert.That(createdUser.IsActive, Is.True);
    }

    [Test]
    public async Task UpdateAsync_WhenUserExists_ShouldUpdateAndReturnUserDto()
    {
        // Arrange
        var existingUser = new UserModel { Id = "1", Name = "Old Name", Info = "Old Info", Avatar = "Old Avatar" };
        var updateDto = new UserUpdateDto { Name = "New Name", Info = "New Info", Avatar = "New Avatar" };
        
        _mockUserRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existingUser);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<UserModel>())).Returns(Task.CompletedTask);

        // Act
        var result = await _userService.UpdateAsync("1", updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("New Name"));
        Assert.That(result.Info, Is.EqualTo("New Info"));
        Assert.That(result.Avatar, Is.EqualTo("New Avatar"));
        Assert.That(existingUser.Name, Is.EqualTo("New Name"));
    }

    [Test]
    public async Task DeleteAsync_ShouldCallDeleteAsyncOnRepo()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.DeleteAsync("1")).Returns(Task.CompletedTask);

        // Act
        await _userService.DeleteAsync("1");

        // Assert
        _mockUserRepo.Verify(r => r.DeleteAsync("1"), Times.Once);
    }
}