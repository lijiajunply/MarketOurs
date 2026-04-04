using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using MarketOurs.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        _mockUserService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo(200));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Count, Is.EqualTo(2));
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
    public async Task GetUserById_WhenUserDoesNotExist_ShouldReturn404()
    {
        // Arrange
        _mockUserService.Setup(s => s.GetByIdAsync("2")).ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetUserById("2");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(404));
        Assert.That(result.Message, Is.EqualTo("用户不存在"));
    }

    [Test]
    public async Task CreateUser_WithExistingAccount_ShouldReturn400()
    {
        // Arrange
        var createDto = new UserCreateDto { Account = "existing@test.com" };
        _mockUserService.Setup(s => s.GetByAccountAsync("existing@test.com"))
            .ReturnsAsync(new UserDto { Id = "1" });

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(400));
        Assert.That(result.Message, Is.EqualTo("该账号已被注册"));
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
    public async Task DeleteUser_WhenDeletingSelf_ShouldReturn400()
    {
        // Arrange (current user is "1" based on Setup)

        // Act
        var result = await _controller.DeleteUser("1");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(400));
        Assert.That(result.Message, Is.EqualTo("不能删除当前登录的管理员账号"));
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