namespace MarketOurs.WebAPI.Controllers;

/// <summary>
    /// 统一API响应模型
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// 请求状态码（200表示成功，其他表示失败）
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 业务错误码（0表示成功，其他表示具体错误类型）
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// 详细描述
        /// </summary>
        public string? Detail { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public T? Data { get; set; }
        
        /// <summary>
        /// 请求ID，用于追踪请求
        /// </summary>
        public string? RequestId { get; set; }
        
        /// <summary>
        /// 响应时间戳（UTC时间）
        /// </summary>
        public string? Timestamp { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ApiResponse()
        {
            Code = 200;
            ErrorCode = 0;
            Message = "Success";
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        /// <summary>
        /// 成功响应
        /// </summary>
        /// <param name="data">响应数据</param>
        /// <param name="message">响应消息</param>
        /// <param name="detail">详细描述</param>
        /// <param name="requestId">请求ID</param>
        /// <returns>成功响应模型</returns>
        public static ApiResponse<T> Success(T data, string message = "Success", string? detail = null, string? requestId = null)
        {
            return new ApiResponse<T>
            {
                Code = 200,
                ErrorCode = 0,
                Message = message,
                Detail = detail,
                Data = data,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        /// <summary>
        /// 失败响应
        /// </summary>
        /// <param name="errorCode">业务错误码</param>
        /// <param name="message">错误消息</param>
        /// <param name="detail">详细描述</param>
        /// <param name="requestId">请求ID</param>
        /// <returns>失败响应模型</returns>
        public static ApiResponse<T> Fail(int errorCode, string message, string? detail = null, string? requestId = null)
        {
            return new ApiResponse<T>
            {
                Code = GetHttpStatusCodeFromErrorCode(errorCode),
                ErrorCode = errorCode,
                Message = message,
                Detail = detail,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
        
        /// <summary>
        /// 失败响应
        /// </summary>
        /// <param name="httpStatusCode">HTTP状态码</param>
        /// <param name="errorCode">业务错误码</param>
        /// <param name="message">错误消息</param>
        /// <param name="detail">详细描述</param>
        /// <param name="requestId">请求ID</param>
        /// <returns>失败响应模型</returns>
        public static ApiResponse<T> Fail(int httpStatusCode, int errorCode, string message, string? detail = null, string? requestId = null)
        {
            return new ApiResponse<T>
            {
                Code = httpStatusCode,
                ErrorCode = errorCode,
                Message = message,
                Detail = detail,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

    /// <summary>
    /// 根据业务错误码获取对应的HTTP状态码
    /// </summary>
    /// <param name="errorCode">业务错误码</param>
    /// <returns>HTTP状态码</returns>
    protected static int GetHttpStatusCodeFromErrorCode(int errorCode)
    {
        if (errorCode is >= 1000 and < 2000) // 参数错误
            return 400;
        if (errorCode is >= 2000 and < 3000) // 业务逻辑错误
            return 400;
        if (errorCode is >= 3000 and < 4000) // 权限错误
            return 403;
        if (errorCode is >= 4000 and < 5000) // 资源错误
            return 404;
        if (errorCode is >= 5000 and < 6000) // 系统错误
            return 500;
        if (errorCode is >= 6000 and < 7000) // 外部服务错误
            return 500;

        return 400; // 默认返回400
    }
}

/// <summary>
    /// 无数据的API响应模型
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        /// <summary>
        /// 成功响应
        /// </summary>
        /// <param name="message">响应消息</param>
        /// <param name="detail">详细描述</param>
        /// <param name="requestId">请求ID</param>
        /// <returns>成功响应模型</returns>
        public static ApiResponse Success(string message = "Success", string? detail = null, string? requestId = null)
        {
            return new ApiResponse
            {
                Code = 200,
                ErrorCode = 0,
                Message = message,
                Detail = detail,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        /// <summary>
        /// 失败响应
        /// </summary>
        /// <param name="errorCode">业务错误码</param>
        /// <param name="message">错误消息</param>
        /// <param name="detail">详细描述</param>
        /// <param name="requestId">请求ID</param>
        /// <returns>失败响应模型</returns>
        public new static ApiResponse Fail(int errorCode, string message, string? detail = null, string? requestId = null)
        {
            return new ApiResponse
            {
                Code = GetHttpStatusCodeFromErrorCode(errorCode),
                ErrorCode = errorCode,
                Message = message,
                Detail = detail,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
        
        /// <summary>
        /// 失败响应
        /// </summary>
        /// <param name="httpStatusCode">HTTP状态码</param>
        /// <param name="errorCode">业务错误码</param>
        /// <param name="message">错误消息</param>
        /// <param name="detail">详细描述</param>
        /// <param name="requestId">请求ID</param>
        /// <returns>失败响应模型</returns>
        public new static ApiResponse Fail(int httpStatusCode, int errorCode, string message, string? detail = null, string? requestId = null)
        {
            return new ApiResponse
            {
                Code = httpStatusCode,
                ErrorCode = errorCode,
                Message = message,
                Detail = detail,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
    }