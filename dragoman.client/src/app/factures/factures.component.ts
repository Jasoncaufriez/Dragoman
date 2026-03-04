import { Component, OnInit } from '@angular/core';
import { PaiementsService, PaiementMoisInterpreteRowDto, PaiementMoisDetailDto, PaiementMoisDetailRowDto } from '../services/paiements.service';

@Component({
  selector: 'app-factures',
  templateUrl: './factures.component.html',
  styleUrls: ['./factures.component.css']
})
export class FacturesComponent implements OnInit {
  month = this.currentMonth();
  loading = false;
  error?: string;

  interpretes: PaiementMoisInterpreteRowDto[] = [];
  selected?: PaiementMoisInterpreteRowDto;

  detailLoading = false;
  detail?: PaiementMoisDetailDto;

  constructor(private api: PaiementsService) { }

  ngOnInit(): void {
    this.loadMonth();
  }

  currentMonth(): string {
    const d = new Date();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    return `${d.getFullYear()}-${m}`;
  }

  loadMonth(): void {
    this.loading = true;
    this.error = undefined;
    this.selected = undefined;
    this.detail = undefined;

    this.api.listInterpretes(this.month).subscribe({
      next: r => {
        this.interpretes = r;
        this.loading = false;
      },
      error: () => {
        this.error = 'Impossible de charger les paiements du mois.';
        this.loading = false;
      }
    });
  }

  pick(row: PaiementMoisInterpreteRowDto): void {
    this.selected = row;
    this.detail = undefined;
    this.detailLoading = true;

    this.api.detail(this.month, row.tolkcode).subscribe({
      next: d => {
        this.detail = d;
        this.detailLoading = false;
      },
      error: () => {
        this.error = 'Impossible de charger le détail.';
        this.detailLoading = false;
      }
    });
  }

  trackByTolkcode(_: number, r: PaiementMoisInterpreteRowDto): string {
    return r.tolkcode;
  }

  deleting = false;

  deletePaiement(row: PaiementMoisDetailRowDto): void {
    if (row.idFacture != null) return;
    if (!confirm('Supprimer ce paiement et la prestation associée ?')) return;

    this.deleting = true;
    this.api.deletePaiement(row.idPaiement).subscribe({
      next: () => {
        this.deleting = false;
        // Refresh detail
        if (this.selected) {
          this.pick(this.selected);
        }
        // Refresh list
        this.loadMonth();
      },
      error: () => {
        this.error = 'Impossible de supprimer le paiement.';
        this.deleting = false;
      }
    });
  }

  downloading = false;

  downloadPdfMonth() {
    this.downloading = true;
    this.api.downloadMonthPdf(this.month).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Factures_${this.month}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.downloading = false;
      },
      error: () => {
        this.error = 'Impossible de générer le PDF.';
        this.downloading = false;
      }
    });
  }

}
