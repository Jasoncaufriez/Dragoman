import { Component, OnInit } from '@angular/core';
import { DashboardService, LangueToday } from '../services/dashboard.service';
import { UserService } from '../services/user.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  username = '...';

  // Compteurs
  nbAudiences = 0;
  nbInterpretes = 0;
  nbLangues = 0;

  // Listes
  audiencesToday: any[] = [];
  languesToday: LangueToday[] = [];
  audiencesSupprimees: any[] = [];

  loading = true;
  error?: string;

  constructor(
    private dashboardService: DashboardService,
    private userService: UserService
  ) { }

  ngOnInit(): void {
    // 1) Utilisateur courant
    this.userService.getCurrentUser().subscribe({
      next: (u) => this.username = u?.username ?? 'Invité',
      error: () => this.username = 'Invité'
    });

    // 2) Compteurs + listes du jour
    this.dashboardService.getAudienceCountToday().subscribe({
      next: r => this.nbAudiences = r.nbAudiences,
      error: () => this.nbAudiences = 0
    });

    this.dashboardService.getInterpretesCountToday().subscribe({
      next: r => this.nbInterpretes = r.nbInterpretes,
      error: () => this.nbInterpretes = 0
    });

    this.dashboardService.getLanguesToday().subscribe({
      next: data => {
        this.languesToday = data;
        this.nbLangues = data.length; // nombre de langues distinctes
      },
      error: () => { this.languesToday = []; this.nbLangues = 0; }
    });

    this.dashboardService.getAudiencesToday().subscribe({
      next: d => this.audiencesToday = d,
      error: () => this.audiencesToday = []
    });

    this.dashboardService.getAudiencesSupprimeesToday().subscribe({
      next: d => this.audiencesSupprimees = d,
      error: () => this.audiencesSupprimees = []
    });

    this.loading = false;
  }
}
