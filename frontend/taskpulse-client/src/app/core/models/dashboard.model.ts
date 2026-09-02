// Matches TaskPulse.API/DTO/DashboardDto/ChartSliceDto.cs — one bar/slice
// label + count, reused for both required charts (§17).
export interface ChartSlice {
  label: string;
  count: number;
}

// Matches TaskPulse.API/DTO/DashboardDto/DashboardSummaryDto.cs
// (GET /api/dashboard/summary).
export interface DashboardSummary {
  projectCount: number;
  taskCount: number;
  completedTaskCount: number;
  overdueTaskCount: number;
  myOpenTaskCount: number;
  unreadNotificationCount: number;
  pendingApprovalCount: number;
  tasksByStatus: ChartSlice[];
  tasksByPriority: ChartSlice[];
}
