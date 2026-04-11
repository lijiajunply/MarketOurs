# MarketOurs WebAPI

MarketOurs 是一个基于 **.NET Core** 构建的全面且健壮的后端 Web API。它为市场/社区平台提供基础支持，包含用户管理、帖子发布、评论、实时通知和内容审核等核心功能。系统在设计时充分考虑了高并发处理能力和安全性，采用了现代架构模式与业界最佳实践。

## ✨ 核心特性

- **认证与授权 (Authentication & Authorization)**: 
  - 基于 RSA 密钥对的 JWT 身份验证。
  - 支持第三方 OAuth2 登录（GitHub、Google、微信、以及自定义的校园/企业 OAuth 授权）。
  - 基于角色的访问控制 (RBAC)。
- **核心业务逻辑 (Core Business Logic)**: 
  - 完善的帖子与评论管理机制。
  - 针对高并发场景优化的“点赞”系统，结合分布式锁与后台异步同步策略保障数据一致性。
- **内容审核 (Content Moderation)**: 
  - 内置敏感词过滤系统，用于维护健康的社区环境。
- **通知系统 (Notifications)**: 
  - 支持多渠道的通知分发机制（站内推送、电子邮件、基于 UniSms 的短信）。
- **安全与可靠性 (Security & Reliability)**: 
  - 接口限流中间件 (Rate Limiting)，防止恶意请求和接口滥用。
  - 数据脱敏中间件 (Data Masking)，保护用户隐私和敏感信息。
  - 全局异常处理，以及健壮的安全响应头（CSP、XSS 防护机制等）。
- **性能与可观测性 (Performance & Observability)**: 
  - 支持响应数据压缩（Brotli、Gzip）。
  - 多级缓存架构设计（Redis 分布式缓存 + 内存缓存）。
  - 接入 Serilog 实现全面的结构化日志记录（支持输出至控制台、文件、SQLite）。
  - 集成 Prometheus 监控指标采集与 Health Check 健康检查端点。

## 🛠️ 技术栈

- **框架**: .NET (ASP.NET Core Web API)
- **数据库**: PostgreSQL (生产环境推荐) / SQLite (开发环境) via Entity Framework Core
- **缓存**: Redis & IMemoryCache
- **数据验证**: FluentValidation
- **API 接口文档**: OpenAPI & Scalar API Reference
- **监控指标**: Prometheus-net
- **日志**: Serilog
- **测试**: xUnit, Moq (涵盖单元测试、集成测试、压力测试和高并发测试)

## 📁 项目结构

该解决方案遵循清晰的关注点分离架构理念：

- `MarketOurs.WebAPI`: 表现层，包含控制器 (Controllers)、中间件 (Middlewares)、过滤器 (Filters) 以及应用程序的主入口和配置。
- `MarketOurs.DataAPI`: 业务逻辑层，包含各种服务 (Services)、数据仓储 (Repositories)、应用配置 (Configs) 以及自定义业务异常 (Exceptions)。
- `MarketOurs.Data`: 数据访问层，包含 Entity Framework 上下文 (Context)、数据库迁移脚本 (Migrations)、数据实体模型 (DataModels) 及数据传输对象 (DTOs)。
- `MarketOurs.Test`: 综合测试工程，包含全面的集成测试、并发测试、压力测试与单元测试。

## 🚀 快速入门

### 环境要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (或更高版本)
- [PostgreSQL](https://www.postgresql.org/) (可选，未配置将默认使用本地 SQLite)
- [Redis](https://redis.io/) (可选，但强烈建议配置以开启完整功能与提升性能)

### 项目配置

1. 克隆本代码仓库。
2. 进入 `MarketOurs.WebAPI` 目录，将 `.env.example` 文件重命名为 `.env`，并根据你的本地环境填写相关配置信息：

```env
# 数据库连接字符串（留空则自动在本地生成 SQLite 数据库）
SQL=Host=localhost;Database=marketours;Username=postgres;Password=yourpassword

# Redis 连接配置
REDIS=localhost:6379

# 初始管理员账号凭证 (用户名,密码 - 如果为空则默认生成)
USER=admin,your_secure_password

# 第三方 OAuth 登录提供商配置 (可选)
GITHUB_CLIENTID=
GITHUB_CLIENTSECRET=
GOOGLE_CLIENTID=
GOOGLE_CLIENTSECRET=
WEIXIN_CLIENTID=
WEIXIN_CLIENTSECRET=
```

### 运行应用

在终端中导航至 `MarketOurs.WebAPI` 项目目录，运行以下命令启动服务：

```bash
cd MarketOurs.WebAPI
dotnet run
```

应用启动时将自动检测并执行任何挂起的数据库迁移操作，并根据环境配置自动初始化管理员用户数据。

## 📚 API 文档与监控

当应用程序在开发环境 (Development) 下成功运行后，你可以通过浏览器访问以下端点：

- **API 接口调试文档 (Scalar)**: `http://localhost:<port>/scalar/v1`
- **系统健康检查 (Health Checks)**: `http://localhost:<port>/api/health`
- **Prometheus 监控指标 (Metrics)**: `http://localhost:<port>/api/metrics`

## 🧪 自动化测试

项目内包含丰富的自动化测试用例，用以保障系统质量。如需运行所有测试，请在终端执行：

```bash
cd MarketOurs.Test
dotnet test
```

测试包含以下几大类：
- **集成测试 (Integration Tests)**: 用于验证各个 API 端点间的端到端工作流。
- **并发测试 (Concurrency Tests)**: 用于验证分布式锁、点赞等高并发操作的数据正确性。
- **压力测试 (Stress Tests)**: 模拟高负载访问场景，验证缓存雪崩防御和系统稳定性。
- **单元测试 (Unit Tests)**: 针对独立的服务组件与核心逻辑进行的细粒度测试。
