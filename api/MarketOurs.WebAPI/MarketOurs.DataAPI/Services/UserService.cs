using MarketOurs.Data.DataModels;
using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Repos;

namespace MarketOurs.DataAPI.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(string id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<UserDto> CreateAsync(UserCreateDto createDto);
    Task<UserDto?> UpdateAsync(string id, UserUpdateDto updateDto);
    Task DeleteAsync(string id);
}

public class UserService(IUserRepo userRepo) : IUserService
{
    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await userRepo.GetAllAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await userRepo.GetByIdAsync(id);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await userRepo.GetByEmailAsync(email);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<UserDto> CreateAsync(UserCreateDto createDto)
    {
        var user = new UserModel
        {
            Email = createDto.Email,
            Password = createDto.Password,
            Name = createDto.Name,
            Role = createDto.Role,
            CreatedAt = DateTime.Now,
            LastLoginAt = DateTime.Now,
            IsActive = true
        };
        await userRepo.CreateAsync(user);
        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(string id, UserUpdateDto updateDto)
    {
        var user = await userRepo.GetByIdAsync(id);
        if (user == null) return null;

        user.Name = updateDto.Name;
        user.Avatar = updateDto.Avatar;
        user.Info = updateDto.Info;

        await userRepo.UpdateAsync(user);
        return MapToDto(user);
    }

    public async Task DeleteAsync(string id)
    {
        await userRepo.DeleteAsync(id);
    }

    public static UserDto MapToDto(UserModel user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            Avatar = user.Avatar,
            Info = user.Info,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        };
    }
}
