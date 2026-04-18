import '../models/api_response.dart';
import '../models/paged_result.dart';
import '../models/post.dart';
import '../models/comment.dart';
import 'api_service.dart';

class PostService {
  final _api = ApiService().dio;

  ApiResponse<PagedResult<PostDto>> _parsePagedPosts(dynamic data) {
    return ApiResponse<PagedResult<PostDto>>.fromJson(
      data as Map<String, dynamic>,
      (json) => PagedResult<PostDto>.fromJson(
        json as Map<String, dynamic>,
        (item) => PostDto.fromJson(item as Map<String, dynamic>),
      ),
    );
  }

  Future<ApiResponse<PagedResult<PostDto>>> getPosts({
    int pageIndex = 1,
    int pageSize = 10,
    String? keyword,
  }) async {
    final response = await _api.get(
      '/Post',
      queryParameters: {
        'PageIndex': pageIndex,
        'PageSize': pageSize,
        'Keyword': keyword,
      }..removeWhere((_, value) => value == null),
    );
    return _parsePagedPosts(response.data);
  }

  Future<ApiResponse<PagedResult<PostDto>>> searchPosts({
    int pageIndex = 1,
    int pageSize = 10,
    String? keyword,
  }) async {
    final response = await _api.get(
      '/Post/search',
      queryParameters: {
        'PageIndex': pageIndex,
        'PageSize': pageSize,
        'Keyword': keyword,
      }..removeWhere((_, value) => value == null),
    );
    return _parsePagedPosts(response.data);
  }

  Future<ApiResponse<PagedResult<PostDto>>> getUserPosts(
    String userId, {
    int pageIndex = 1,
    int pageSize = 10,
    String? keyword,
  }) async {
    final response = await _api.get(
      '/Post/user/$userId',
      queryParameters: {
        'PageIndex': pageIndex,
        'PageSize': pageSize,
        'Keyword': keyword,
      }..removeWhere((_, value) => value == null),
    );
    return _parsePagedPosts(response.data);
  }

  Future<ApiResponse<List<PostDto>>> getHotPosts({int count = 10}) async {
    final response = await _api.get(
      '/Post/hot',
      queryParameters: {'count': count},
    );
    return ApiResponse<List<PostDto>>.fromJson(
      response.data as Map<String, dynamic>,
      (json) => (json as List)
          .map((item) => PostDto.fromJson(item as Map<String, dynamic>))
          .toList(),
    );
  }

  Future<ApiResponse<PostDto>> createPost(PostCreateDto request) async {
    final response = await _api.post('/Post', data: request.toJson());
    return ApiResponse<PostDto>.fromJson(
      response.data,
      (json) => PostDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<PostDto>> getPost(String id) async {
    final response = await _api.get('/Post/$id');
    return ApiResponse<PostDto>.fromJson(
      response.data,
      (json) => PostDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<PostDto>> updatePost(
    String id,
    PostUpdateDto request,
  ) async {
    final response = await _api.put('/Post/$id', data: request.toJson());
    return ApiResponse<PostDto>.fromJson(
      response.data,
      (json) => PostDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> deletePost(String id) async {
    final response = await _api.delete('/Post/$id');
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> likePost(String id) async {
    final response = await _api.post('/Post/$id/like');
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> dislikePost(String id) async {
    final response = await _api.post('/Post/$id/dislike');
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse<List<CommentDto>>> getPostComments(
    String id,
    String type,
  ) async {
    final response = await _api.get('/Post/$id/comments/$type');
    return ApiResponse<List<CommentDto>>.fromJson(
      response.data as Map<String, dynamic>,
      (json) => (json as List)
          .map((i) => CommentDto.fromJson(i as Map<String, dynamic>))
          .toList(),
    );
  }
}
