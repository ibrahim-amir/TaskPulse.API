import { Component, OnInit } from '@angular/core';
import { DashboardSummary } from '../../core/models/dashboard.model';
import { DashboardService } from '../../core/services/dashboard.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  summary: DashboardSummary | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(private dashboardService: DashboardService, public auth: AuthService) {}

  ngOnInit(): void {
    // §16/§17 — every number and both required charts come straight from
    // GET /api/dashboard/summary. Nothing here is hardcoded.
    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary = summary;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Unable to load dashboard statistics.';
        this.isLoading = false;
      },
    });
  }

  // Widest slice in a chart sets the 100% bar width for the rest — keeps the
  // two charts readable without pulling in a charting library yet.
  barWidth(count: number, slices: { count: number }[]): number {
    const max = Math.max(...slices.map((s) => s.count), 1);
    return Math.round((count / max) * 100);
  }
}
