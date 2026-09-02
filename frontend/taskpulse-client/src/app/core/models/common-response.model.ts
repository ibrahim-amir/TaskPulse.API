// Every TaskPulse.API endpoint returns this envelope
// (TaskPulse.API/ApiResponse/CommonResponse.cs) — services unwrap .data,
// components never see this shape directly.
export interface CommonResponse<T> {
  success: boolean;
  messages: string[];
  errors: string[];
  data: T | null;
  statusCode: string;
  errorCode: string | null;
  traceId: string | null;
}
