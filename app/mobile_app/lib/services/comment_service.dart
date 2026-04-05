import '../models/api_response.dart';
import '../models/comment.dart';
import '../models/paged_result.dart';
import 'api_service.dart';

class CommentService {
  final _api = ApiService().dio;

  Future<ApiResponse<PagedResult<CommentDto>>> getComments({
    int pageIndex = 1,
    int pageSize = 10,
    String? keyword,
  }) async {
    final response = await _api.get('/Comment', queryParameters: {
      'PageIndex': pageIndex,
      'PageSize': pageSize,
      if (keyword != null) 'Keyword': keyword,
    });
    return ApiResponse<PagedResult<CommentDto>>.fromJson(
      response.data,
      (json) => PagedResult<CommentDto>.fromJson(
        json as Map<String, dynamic>,
        (item) => CommentDto.fromJson(item as Map<String, dynamic>),
      ),
    );
  }

  Future<ApiResponse<CommentDto>> createComment(CommentCreateDto request) async {
    final response = await _api.post('/Comment', data: request.toJson());
    return ApiResponse<CommentDto>.fromJson(
      response.data,
      (json) => CommentDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<CommentDto>> getComment(String id) async {
    final response = await _api.get('/Comment/$id');
    return ApiResponse<CommentDto>.fromJson(
      response.data,
      (json) => CommentDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<CommentDto>> updateComment(String id, CommentUpdateDto request) async {
    final response = await _api.put('/Comment/$id', data: request.toJson());
    return ApiResponse<CommentDto>.fromJson(
      response.data,
      (json) => CommentDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> deleteComment(String id) async {
    final response = await _api.delete('/Comment/$id');
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse<CommentDto>> replyToComment(String id, CommentCreateDto request) async {
    final response = await _api.post('/Comment/$id/reply', data: request.toJson());
    return ApiResponse<CommentDto>.fromJson(
      response.data,
      (json) => CommentDto.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse> likeComment(String id) async {
    final response = await _api.post('/Comment/$id/like');
    return ApiResponse.fromJson(response.data, (json) => json);
  }

  Future<ApiResponse> dislikeComment(String id) async {
    final response = await _api.post('/Comment/$id/dislike');
    return ApiResponse.fromJson(response.data, (json) => json);
  }
}
