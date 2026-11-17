export interface ApiError {
  message: string;
  errorCode: string;
  field?: string | null;
}

export interface ApiResponse<T> {
  isSuccess: boolean;
  result: T | null;
  message: string;
  statusCode: number;
  errors: ApiError[];
}
