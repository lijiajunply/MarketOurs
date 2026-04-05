export interface ApiResponse<T = any> {
  code: number;
  errorCode: number;
  message: string;
  detail: string | null;
  data: T;
  requestId: string | null;
  timestamp: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}
