using MarketOurs.Data.DTOs;
using MarketOurs.DataAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketOurs.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PostTagController(IPostTagService postTagService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ApiResponse<List<PostTagDto>>> GetActive()
    {
        return ApiResponse<List<PostTagDto>>.Success(await postTagService.GetActiveAsync(), "获取成功");
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<List<PostTagDto>>> GetAll()
    {
        return ApiResponse<List<PostTagDto>>.Success(await postTagService.GetAllAsync(), "获取成功");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PostTagDto>> Create([FromBody] PostTagCreateDto request)
    {
        return ApiResponse<PostTagDto>.Success(await postTagService.CreateAsync(request), "创建成功");
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PostTagDto>> Update(string id, [FromBody] PostTagUpdateDto request)
    {
        return ApiResponse<PostTagDto>.Success(await postTagService.UpdateAsync(id, request), "更新成功");
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<PostTagDto>> Deactivate(string id)
    {
        return ApiResponse<PostTagDto>.Success(await postTagService.DeactivateAsync(id), "停用成功");
    }
}
