import 'package:dio/dio.dart';
import 'package:image_picker/image_picker.dart';

import '../models/api_response.dart';
import 'api_service.dart';

class FileService {
  final _api = ApiService().dio;

  Future<ApiResponse<String>> uploadImage(XFile file) async {
    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(file.path, filename: file.name),
    });

    final response = await _api.post('/File/upload/image', data: formData);
    return ApiResponse<String>.fromJson(
      response.data,
      (json) => json as String,
    );
  }

  Future<ApiResponse<List<String>>> uploadImages(List<XFile> files) async {
    final payload = <MultipartFile>[];
    for (final file in files) {
      payload.add(await MultipartFile.fromFile(file.path, filename: file.name));
    }

    final formData = FormData.fromMap({'files': payload});

    final response = await _api.post('/File/upload/images', data: formData);
    return ApiResponse<List<String>>.fromJson(
      response.data,
      (json) => (json as List<Object?>).cast<String>(),
    );
  }
}
