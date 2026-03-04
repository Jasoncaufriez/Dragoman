import { Component, OnInit } from '@angular/core';
import { AdStatusService, AdUserNormalStatusDto } from '../ad-status.service';
import { AdUserStatus } from '../../models/ad-status.model';

type CategoryKey =
  | 'pwdExpired'
  | 'pwdExpiringSoon'
  | 'pwdNeverExpires'
  | 'inactive90Plus'
  | 'inactiveSoon'
  | 'all';

type SaveStateValue = 'idle' | 'saving' | 'saved' | 'error';

interface SaveState {
  [samAccountName: string]: SaveStateValue;
}

@Component({
  selector: 'app-ad-status-dashboard',
  templateUrl: './ad-status-dashboard.component.html',
  styleUrls: ['./ad-status-dashboard.component.css']
})
export class AdStatusDashboardComponent implements OnInit {

  allUsers: AdUserStatus[] = [];
  filteredUsers: AdUserStatus[] = [];

  counts = {
    pwdExpired: 0,
    pwdExpiringSoon: 0,
    pwdNeverExpires: 0,
    inactive90Plus: 0,
    inactiveSoon: 0
  };

  selectedCategory: CategoryKey | null = 'pwdExpired';
  isLoading = false;
  errorMessage = '';
  saveState: SaveState = {};

  sortKey: keyof AdUserStatus = 'samAccountName';
  sortDirection: 'asc' | 'desc' = 'asc';
  showNormal: boolean = false; // Par défaut, on masque les situations normales

  constructor(private adStatusService: AdStatusService) { }

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.adStatusService.getAll().subscribe({
      next: data => {
        this.allUsers = this.processUsers(data);
        this.calculateCounts();

        this.applyFiltersAndSort();
        this.isLoading = false;
      },
      error: err => {
        console.error(err);
        this.errorMessage = 'Erreur lors du chargement des données AD.';
        this.isLoading = false;
      }
    });
  }

  private processUsers(users: AdUserStatus[]): AdUserStatus[] {
    return users.map(u => {
      // Logique pour colorer les expirations imminentes
      if (u.passwordStatus !== 'Expired' && u.passwordStatus !== 'NeverExpires' && u.daysUntilExpiration !== null) {
        if (u.daysUntilExpiration <= 0) {
          u.passwordStatus = 'Expired';
        } else if (u.daysUntilExpiration <= 1) {
          u.passwordStatus = 'Expired_Red';
        } else if (u.daysUntilExpiration <= 7) {
          u.passwordStatus = 'Expired_Orange';
        } else if (u.daysUntilExpiration <= 15) {
          u.passwordStatus = 'Expired_Yellow';
        }
      }
      return u;
    });
  }

  private calculateCounts(): void {
    this.counts.pwdExpired = this.allUsers.filter(u => u.passwordStatus === 'Expired').length;

    this.counts.pwdExpiringSoon = this.allUsers.filter(u =>
      ['Expired_Red', 'Expired_Orange', 'Expired_Yellow'].includes(u.passwordStatus)
    ).length;

    this.counts.pwdNeverExpires = this.allUsers.filter(u => u.passwordStatus === 'NeverExpires').length;

    this.counts.inactive90Plus = this.allUsers.filter(u => u.inactivityStatus === 'Inactive90Plus').length;
    this.counts.inactiveSoon = this.allUsers.filter(u => u.inactivityStatus === 'InactiveSoon').length;
  }

  setCategory(category: CategoryKey): void {
    this.selectedCategory = category;
    this.applyFiltersAndSort();
  }

  toggleShowNormal(): void {
    this.applyFiltersAndSort();
  }

  sortUsers(key: keyof AdUserStatus): void {
    if (this.sortKey === key) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = key;
      this.sortDirection = 'asc';
    }
    this.applyFiltersAndSort();
  }

  applyFiltersAndSort(): void {
    let temp = [...this.allUsers];

    // 1. Filtre "IsNormal"
    if (!this.showNormal) {
      temp = temp.filter(u => !u.isNormal);
    }

    // 2. Filtre Catégorie
    if (this.selectedCategory && this.selectedCategory !== 'all') {
      switch (this.selectedCategory) {
        case 'pwdExpired':
          temp = temp.filter(u => u.passwordStatus === 'Expired');
          break;
        case 'pwdExpiringSoon':
          temp = temp.filter(u => ['Expired_Red', 'Expired_Orange', 'Expired_Yellow'].includes(u.passwordStatus));
          break;
        case 'pwdNeverExpires':
          temp = temp.filter(u => u.passwordStatus === 'NeverExpires');
          break;
        case 'inactive90Plus':
          temp = temp.filter(u => u.inactivityStatus === 'Inactive90Plus');
          break;
        case 'inactiveSoon':
          temp = temp.filter(u => u.inactivityStatus === 'InactiveSoon');
          break;
      }
    }

    // 3. Tri
    temp.sort((a, b) => {
      let valA: any = a[this.sortKey];
      let valB: any = b[this.sortKey];

      if (valA === null || valA === undefined) valA = this.sortDirection === 'asc' ? '' : 'zzzzz';
      if (valB === null || valB === undefined) valB = this.sortDirection === 'asc' ? '' : 'zzzzz';

      let comparison = 0;

      // Gestion du tri booléen pour 'IsNormal'
      if (typeof valA === 'boolean' && typeof valB === 'boolean') {
        // false avant true en asc
        comparison = (valA === valB) ? 0 : (valA ? 1 : -1);
      } else if (typeof valA === 'number' && typeof valB === 'number') {
        comparison = valA - valB;
      } else {
        comparison = valA.toString().toLowerCase().localeCompare(valB.toString().toLowerCase());
      }

      return this.sortDirection === 'asc' ? comparison : -comparison;
    });

    this.filteredUsers = temp;
  }

  // Sauvegarde du statut normal
  toggleNormal(user: AdUserStatus): void {
    const sam = user.samAccountName;
    if (!sam) return;

    // Inverse l'état local immédiatement pour une meilleure UX
    // user.isNormal est déjà mis à jour par [(ngModel)]

    const dto: AdUserNormalStatusDto = {
      samAccountName: sam,
      isNormal: user.isNormal
    };

    this.saveState[sam] = 'saving';

    this.adStatusService.saveNormalStatus(dto).subscribe({
      next: () => {
        this.saveState[sam] = 'saved';
        setTimeout(() => { if (this.saveState[sam] === 'saved') this.saveState[sam] = 'idle'; }, 2000);
        this.applyFiltersAndSort(); // Re-filtrer pour masquer/afficher si le filtre 'showNormal' est actif
      },
      error: err => {
        console.error(err);
        this.saveState[sam] = 'error';
      }
    });
  }

  saveComment(user: AdUserStatus): void {
    const sam = user.samAccountName;
    if (!sam) return;

    this.saveState[sam] = 'saving';
    this.adStatusService.saveComment({
      samAccountName: sam,
      comment: user.comment ?? ''
    }).subscribe({
      next: () => {
        this.saveState[sam] = 'saved';
        setTimeout(() => { if (this.saveState[sam] === 'saved') this.saveState[sam] = 'idle'; }, 2000);
      },
      error: err => {
        console.error(err);
        this.saveState[sam] = 'error';
      }
    });
  }

  // --- Fonctions utilitaires existantes ---

  getCategoryLabel(): string {
    switch (this.selectedCategory) {
      case 'pwdExpired': return 'Mots de passe EXPIRÉS';
      case 'pwdExpiringSoon': return 'Mots de passe expirent bientôt (15j / 7j / 24h)';
      case 'pwdNeverExpires': return 'Mots de passe "Never Expires"';
      case 'inactive90Plus': return 'Inactifs (> 90 jours)';
      case 'inactiveSoon': return 'Bientôt inactifs';
      default: return 'Tous les utilisateurs';
    }
  }

  openTeamsChat(user: AdUserStatus): void {
    const upn = `${user.samAccountName}@ton-domaine.be`; // Adapter 'ton-domaine.be' si nécessaire
    const url = `https://teams.microsoft.com/l/chat/0/0?users=${encodeURIComponent(upn)}`;
    window.open(url, '_blank');
  }

  getPasswordStatusLabel(status: string): string {
    switch (status) {
      case 'Expired': return 'Expiré';
      case 'Expired_Red': return '< 24h';
      case 'Expired_Orange': return '< 7 jours';
      case 'Expired_Yellow': return '< 15 jours ';
      case 'NeverExpires': return 'Jamais';
      default: return 'OK';
    }
  }
}
