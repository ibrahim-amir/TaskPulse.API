// Matches TaskPulse.API/Entities/EnumForEntities/Enums.cs UserRole.
export type UserRole = 'Admin' | 'Manager' | 'Member';

// Matches TaskPulse.API/DTO/UserDto/UserDTO.cs.
export interface User {
  id: number;
  name: string;
  email: string;
  profileImageUrl: string | null;
  createdAt: string;
  isDisabled: boolean;
  role: UserRole;
}

// Matches TaskPulse.API/DTO/UserDto/LoginDto.cs.
export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

// Matches TaskPulse.API/DTO/UserDto/AuthResponseDto.cs — login/refresh return
// only tokens, never the user profile. AuthService fetches /auth/me for that.
export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}
