import '../models/api_response.dart';
import '../models/paged_result.dart';
import '../models/post.dart';
import '../models/comment.dart';
import 'api_service.dart';

class PostService {
  final _api = ApiService().dio;

  Future<ApiResponse<PagedResult<PostDto>>> getPosts({
    int pageIndex = 1,
    int pageSize = 10,
    String? keyword,
  }) async {
    final response = await _api.get('/Post', queryParameters: {
      'PageIndex': pageIndex,
      'PageSize': pageSize,
      if (keyword != null) 'Keyword': keyword,
    });
    return ApiResponse<PagedResult<PostDto>>.fromJson(
      response.data,
      (json) => PagedResult<PostDto>.fromJson(
        json as Map<String, dynamic>,
        (item) => PostDto.fromJson(item as Map<String, dynamic>),
      ),
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

  Future<ApiResponse<PostDto>> updatePost(String id, PostUpdateDto request) async {
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

  Future<ApiResponse<List<CommentDto>>> getPostComments(String id, String type) async {
    final response = await _api.get('/Post/$id/comments/$type');
    return ApiResponse<List<CommentDto>>.fromJson(
      response.data,
      (json) => (json as List).map((i) => CommentDto.fromJson(i as Map<String, dynamic>)).toList(),
    );
  }
}
