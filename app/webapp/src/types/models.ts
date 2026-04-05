/**
 * Auth Models
 */
export interface LoginRequest {
  account: string;
  password: string;
  deviceType?: string;
}

export interface UserCreateDto {
  account: string;
  password: string;
  name: string;
  avatar?: string;
  role?: string;
}

export interface RefreshRequest {
  refreshToken: string;
  deviceType?: string;
}

export interface TokenDto {
  accessToken: string;
  refreshToken: string;
}

export interface ForgotPasswordRequest {
  account: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface VerifyCodeRequest {
  code: string;
}

/**
 * User Models
 */
export interface UserDto {
  id: string;
  email: string;
  phone: string;
  name: string;
  role: string;
  avatar: string;
  info: string;
  createdAt: string;
  lastLoginAt: string;
  isActive: boolean;
  isEmailVerified: boolean;
  isPhoneVerified: boolean;
}

export interface UserUpdateDto {
  name?: string;
  avatar?: string;
  info?: string;
}

/**
 * Post Models
 */
export interface PostDto {
  id: string;
  title: string;
  content: string;
  images: string[];
  createdAt: string;
  updatedAt: string;
  userId: string;
  likes: number;
  dislikes: number;
  watch: number;
}

export interface PostCreateDto {
  title: string;
  content: string;
  images?: string[];
  userId: string;
}

export interface PostUpdateDto {
  title: string;
  content: string;
  images?: string[];
}

/**
 * Comment Models
 */
export interface CommentDto {
  id: string;
  content: string;
  images: string[];
  likes: number;
  dislikes: number;
  createdAt: string;
  updatedAt: string;
  userId: string;
  postId: string;
  parentCommentId: string | null;
  repliedComments?: CommentDto[];
}

export interface CommentCreateDto {
  content: string;
  images?: string[];
  userId: string;
  postId: string;
  parentCommentId?: string | null;
}

export interface CommentUpdateDto {
  content: string;
  images?: string[];
}

/**
 * Admin & Logs Models
 */
export interface LogEntry {
  timestamp: string;
  level: string;
  exception: string | null;
  properties: any | null;
  message: string | null;
}

export interface LogStatistics {
  totalCount: number;
  levelCounts: Record<string, number>;
}

export interface LogDistribution {
  timePoint: string;
  totalCount: number;
  errorCount: number;
  infoCount: number;
  warningCount: number;
}

export interface BlacklistStats {
  totalIps: number;
  totalCidrRanges: number;
  cacheHits: number;
  cacheMisses: number;
  blacklistHits: number;
  lastRefreshTime: string;
}

export interface AddIpRequest {
  ip: string;
  reason?: string | null;
}

export interface RemoveIpRequest {
  ip: string;
  reason?: string | null;
}

export interface IpCheckResult {
  ip: string;
  isBlacklisted: boolean;
  checkTime: string;
}
