using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Exceptions;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Controllers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MarketOurs.Test.Controllers;

[TestFixture]
public class UserControllerTests : ControllerTestBase
{
    private Mock<IUserService> _mockUserService;
    private Mock<ILogger<UserController>> _mockLogger;
    private UserController _controller;

    [SetUp]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<UserController>>();
        
        _controller = new UserController(_mockUserService.Object, _mockLogger.Object);
        
        // Use the base class method to setup user
        SetupUser(_controller, "1", "Admin");
    }

    [Test]
    public async Task GetAllUsers_AsAdmin_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new UserDto { Id = "1", Name = "User 1" },
            new UserDto { Id = "2", Name = "User 2" }
        };
        var pagedResult = PagedResultDto<UserDto>.Success(users, 2, 1, 10);
        _mockUserService.Setup(s => s.GetAllAsync(It.IsAny<PaginationParams>())).ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAllUsers(new PaginationParams());

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Items.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetUserById_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var user = new UserDto { Id = "2", Name = "User 2" };
        _mockUserService.Setup(s => s.GetByIdAsync("2")).ReturnsAsync(user);

        // Act
        var result = await _controller.GetUserById("2");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data!.Name, Is.EqualTo("User 2"));
    }

    [Test]
    public void GetUserById_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _mockUserService.Setup(s => s.GetByIdAsync("2")).ReturnsAsync((UserDto?)null);

        // Act
        var ex = Assert.ThrowsAsync<ResourceAccessException>(async () => await _controller.GetUserById("2"));

        // Assert
        Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.UserNotFound));
        Assert.That(ex.HttpStatusCode, Is.EqualTo(404));
    }

    [Test]
    public void CreateUser_WithExistingAccount_ShouldThrowConflictException()
    {
        // Arrange
        var createDto = new UserCreateDto { Account = "existing@test.com" };
        _mockUserService.Setup(s => s.GetByAccountAsync("existing@test.com"))
            .ReturnsAsync(new UserDto { Id = "1" });

        // Act
        var ex = Assert.ThrowsAsync<BusinessException>(async () => await _controller.CreateUser(createDto));

        // Assert
        Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.AccountAlreadyExists));
        Assert.That(ex.HttpStatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task CreateUser_WithNewAccount_ShouldCreateAndReturnUser()
    {
        // Arrange
        var createDto = new UserCreateDto { Account = "new@test.com" };
        var createdUser = new UserDto { Id = "2", Email = "new@test.com" };
        
        _mockUserService.Setup(s => s.GetByAccountAsync("new@test.com"))
            .ReturnsAsync((UserDto?)null);
        _mockUserService.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data!.Id, Is.EqualTo("2"));
    }

    [Test]
    public void DeleteUser_WhenDeletingSelf_ShouldThrowBusinessException()
    {
        // Arrange (current user is "1" based on Setup)

        // Act
        var ex = Assert.ThrowsAsync<BusinessException>(async () => await _controller.DeleteUser("1"));

        // Assert
        Assert.That(ex!.ErrorCode, Is.EqualTo(ErrorCode.OperationFailed));
        Assert.That(ex.HttpStatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task GetPublicProfileById_WhenUserExists_ShouldReturnPublicProfile()
    {
        var profile = new PublicUserProfileDto { Id = "2", Name = "Public User" };
        _mockUserService.Setup(s => s.GetPublicProfileByIdAsync("2")).ReturnsAsync(profile);

        var result = await _controller.GetPublicProfileById("2");

        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Name, Is.EqualTo("Public User"));
    }

    [Test]
    public async Task GetMyProfile_WhenUserIsLoggedIn_ShouldReturnProfile()
    {
        // Arrange
        var user = new UserDto { Id = "1", Name = "Current User" };
        _mockUserService.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(user);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data!.Name, Is.EqualTo("Current User"));
    }

    [Test]
    public async Task UpdateMyProfile_ShouldUpdateAndReturnProfile()
    {
        // Arrange
        var updateDto = new UserUpdateDto { Name = "Updated Name" };
        var updatedUser = new UserDto { Id = "1", Name = "Updated Name" };
        
        _mockUserService.Setup(s => s.UpdateAsync("1", updateDto)).ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateMyProfile(updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data!.Name, Is.EqualTo("Updated Name"));
    }
}
