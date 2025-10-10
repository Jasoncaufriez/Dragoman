import { Component, OnInit } from '@angular/core';
import { DashboardService, LangueToday } from '../services/dashboard.service';
import { UserService } from '../services/user.service';

interface Interprete {
  tolkcode: number;
  nom: string;
  prenom: string;
  gsm?: string;
  tel?: string;
  telbis?: string;
}

interface AudienceVM {
  key: string;
  heureAudience: string;
  salleAudience: string;
  langueRequete: string;
  interpretes: Interprete[];
}

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
  audiencesToday: AudienceVM[] = [];
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

    // 2) Compteurs + langues
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
        this.nbLangues = data.length;
      },
      error: () => { this.languesToday = []; this.nbLangues = 0; }
    });

    // 3) Audiences du jour (détails avec interprètes)
    this.dashboardService.getAudiencesDetailToday().subscribe({
      next: rows => {
        const map = new Map<string, AudienceVM>();

        for (const r of rows) {
          const key = `${r.heureAudience}|${r.salleAudience}|${r.langueRequete}`;
          if (!map.has(key)) {
            map.set(key, {
              key,
              heureAudience: r.heureAudience,
              salleAudience: r.salleAudience,
              langueRequete: r.langueRequete,
              interpretes: []
            });
          }
          if (r.nom || r.prenom) {
            map.get(key)!.interpretes.push({
              tolkcode: r.tolkcode,
              nom: r.nom,
              prenom: r.prenom,
              gsm: r.gsm,
              tel: r.tel,
              telbis: r.telbis
            });
          }
        }

        this.audiencesToday = Array.from(map.values())
          .sort((a, b) =>
            (a.heureAudience ?? '').localeCompare(b.heureAudience ?? '') ||
            (a.salleAudience ?? '').localeCompare(b.salleAudience ?? '')
          );
      },
      error: () => this.audiencesToday = []
    });

    this.dashboardService.getAudiencesSupprimeesToday().subscribe({
      next: d => this.audiencesSupprimees = d,
      error: () => this.audiencesSupprimees = []
    });

    this.loading = false;
  }
}
