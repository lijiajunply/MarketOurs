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

export interface VerifyRegistrationRequest {
  registrationToken: string;
  code: string;
}

export interface VerifyCodeRequest {
  code: string;
}

export interface SendCodeRequest {
  account: string;
}

export interface LoginByCodeRequest {
  account: string;
  code: string;
  deviceType?: string;
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
  oursId?: string;
  githubId?: string;
  googleId?: string;
  weixinId?: string;
}

export interface PublicUserProfileDto {
  id: string;
  name: string;
  role: string;
  avatar: string;
  info: string;
  createdAt: string;
}

export interface UserUpdateDto {
  name?: string;
  avatar?: string;
  info?: string;
  email?: string;
  phone?: string;
  oursId?: string;
  githubId?: string;
  googleId?: string;
  weixinId?: string;
}

export interface UserSimpleDto {
  id: string;
  name: string;
  avatar: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
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
  author?: UserSimpleDto | null;
  likes: number;
  dislikes: number;
  watch: number;
  isReview: boolean;
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
  isReview?: boolean;
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
  isReview: boolean;
  createdAt: string;
  updatedAt: string;
  userId: string;
  author?: UserSimpleDto | null;
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
 * Admin Models
 */
export interface LogEntry {
  timestamp: string;
  level: string;
  exception: string | null;
  properties: unknown | null;
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

export interface AdminTrendPointDto {
  date: string;
  posts: number;
  users: number;
}

export interface AdminRecentActivityDto {
  id: string;
  type: string;
  title: string;
  description: string;
  timestamp: string;
}

export interface AdminOverviewDto {
  totalUsers: number;
  activeUsers: number;
  totalPosts: number;
  postsCreatedInLast7Days: number;
  totalLogs: number;
  errorLogs: number;
  blacklistHits: number;
  cacheHits: number;
  postTrend: AdminTrendPointDto[];
  recentActivities: AdminRecentActivityDto[];
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}

export interface UpdatePostReviewRequest {
  isReview: boolean;
}

/**
 * Notification Models
 */
export const NotificationType = {
  CommentReply: 0,
  PostReply: 1,
  HotList: 2,
  System: 3,
} as const;

export type NotificationType = typeof NotificationType[keyof typeof NotificationType];

export interface NotificationDto {
  id: string;
  userId: string;
  title: string;
  content: string;
  type: NotificationType;
  targetId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface PushSettingsDto {
  enableEmailNotifications: boolean;
  enableHotListPush: boolean;
  enableCommentReplyPush: boolean;
}
