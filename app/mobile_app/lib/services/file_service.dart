import 'package:dio/dio.dart';
import 'package:image_picker/image_picker.dart';

import '../models/api_response.dart';
import 'api_service.dart';

class FileService {
  final _api = ApiService().dio;

  Future<ApiResponse<Map<String, dynamic>>> getUploadKey() async {
    final response = await _api.post('/File/upload/key');
    return ApiResponse<Map<String, dynamic>>.fromJson(
      response.data,
      (json) => json as Map<String, dynamic>,
    );
  }

  Future<ApiResponse<String>> uploadImage(XFile file, {String? key}) async {
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(file.path, filename: file.name),
    });

    final queryParams = key != null ? {'key': key} : null;
    final response = await _api.post(
      '/File/upload/image',
      data: formData,
      queryParameters: queryParams,
      options: ApiService.uploadOptions(),
    );
    return ApiResponse<String>.fromJson(
      response.data,
      (json) => json as String,
    );
  }

  Future<ApiResponse<String>> uploadAvatar(XFile file) async {
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(file.path, filename: file.name),
    });

    final response = await _api.post(
      '/File/upload/avatar',
      data: formData,
      options: ApiService.uploadOptions(anonymous: true),
    );
    return ApiResponse<String>.fromJson(
      response.data,
      (json) => json as String,
    );
  }

  Future<ApiResponse<List<String>>> uploadImages(
    List<XFile> files, {
    String? key,
  }) async {
    // Read all files from disk in parallel instead of serially
    final payload = await Future.wait(
      files.map((f) => MultipartFile.fromFile(f.path, filename: f.name)),
    );

    final formData = FormData.fromMap({'files': payload});

    final queryParams = key != null ? {'key': key} : null;
    final response = await _api.post(
      '/File/upload/images',
      data: formData,
      queryParameters: queryParams,
      options: ApiService.uploadOptions(),
    );
    return ApiResponse<List<String>>.fromJson(
      response.data,
      (json) => (json as List<Object?>).cast<String>(),
    );
  }
}
