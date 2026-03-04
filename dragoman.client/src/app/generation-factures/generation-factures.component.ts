import { Component, OnInit } from '@angular/core';
import {
  FacturesGenService,
  GenererFacturesRequest,
  GenererFacturesResult,
  FactureListItem,
  UpdateStatutResult,
  TransmettreResult
} from '../services/factures.service';

@Component({
  selector: 'app-generation-factures',
  templateUrl: './generation-factures.component.html',
  styleUrls: ['./generation-factures.component.css']
})
export class GenerationFacturesComponent implements OnInit {
  tab: 'generer' | 'enregistrer' | 'historique' = 'enregistrer';

  // PO modifiable
  poNumber = '4501133577';
  editingPo = false;

  // === Génération ===
  genMode: 'mois' | 'periode' = 'mois';
  selectedMonth = this.currentMonth();
  dateDebut = '';
  dateFin = '';
  generating = false;
  genError?: string;
  genResult?: GenererFacturesResult;

  // === Enregistrer ===
  enrMonth = this.currentMonth();
  enrFactures: FactureListItem[] = [];
  enrLoading = false;
  enrError?: string;
  enrSuccess?: string;
  downloading = false;
  sendingEmail = new Set<number>();
  transmittingId = new Set<number>();

  // === Historique ===
  factures: FactureListItem[] = [];
  histLoading = false;
  histError?: string;
  filterMonth = this.currentMonth();
  filterStatut = '';
  filterTolkcode = '';
  statuts = ['GENEREE', 'APPROUVEE', 'TRANSMISE', 'ANNULEE', 'NOTE DE CREDIT', 'CREDIT VALIDE'];

  constructor(private api: FacturesGenService) { }

  ngOnInit(): void {
    this.loadEnregistrer();
  }

  switchTab(t: 'generer' | 'enregistrer' | 'historique'): void {
    this.tab = t;
    this.genError = undefined;
    this.genResult = undefined;
    this.enrError = undefined;
    this.enrSuccess = undefined;
    this.histError = undefined;
    if (t === 'enregistrer') this.loadEnregistrer();
    if (t === 'historique') this.loadHistorique();
  }

  private currentMonth(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
  }

  // ====== Onglet 1 : Générer ======

  generer(): void {
    this.genError = undefined;
    this.genResult = undefined;

    let req: GenererFacturesRequest;

    if (this.genMode === 'periode') {
      if (!this.dateDebut || !this.dateFin) {
        this.genError = 'Veuillez renseigner la date de début et la date de fin.';
        return;
      }
      if (this.dateDebut >= this.dateFin) {
        this.genError = 'La date de début doit être antérieure à la date de fin.';
        return;
      }
      req = { annee: 0, mois: 0, dateDebut: this.dateDebut, dateFin: this.dateFin };
    } else {
      const [a, m] = this.selectedMonth.split('-');
      const annee = parseInt(a, 10);
      const mois = parseInt(m, 10);
      if (!annee || !mois || mois < 1 || mois > 12) {
        this.genError = 'Mois invalide';
        return;
      }
      req = { annee, mois };
    }

    this.generating = true;
    this.api.generer(req).subscribe({
      next: (res: GenererFacturesResult) => {
        this.generating = false;
        this.genResult = res;
      },
      error: (err: any) => {
        this.generating = false;
        this.genError = err?.error?.message ?? err?.error ?? 'Erreur inconnue';
      }
    });
  }

  // ====== Onglet 2 : Enregistrer ======

  loadEnregistrer(): void {
    this.enrLoading = true;
    this.enrError = undefined;
    this.enrSuccess = undefined;
    this.api.list({ month: this.enrMonth }).subscribe({
      next: (res: FactureListItem[]) => {
        this.enrLoading = false;
        this.enrFactures = res;
      },
      error: (err: any) => {
        this.enrLoading = false;
        this.enrError = err?.error?.message ?? 'Erreur inconnue';
      }
    });
  }

  approuver(f: FactureListItem): void {
    this.enrError = undefined;
    this.enrSuccess = undefined;
    this.api.updateStatut(f.idFacture, 'APPROUVEE').subscribe({
      next: (res: UpdateStatutResult) => {
        f.statutFacture = res.statutFacture;
        f.dateValidationFedcom = res.dateValidationFedcom;
        this.enrSuccess = `Facture ${res.reference} approuvée.`;
      },
      error: (err: any) => {
        const msg = typeof err?.error === 'string' ? err.error : err?.error?.message ?? err?.message ?? 'Erreur inconnue';
        this.enrError = `Erreur sur la facture ${f.reference} : ${msg}`;
      }
    });
  }

  annuler(f: FactureListItem): void {
    this.enrError = undefined;
    this.enrSuccess = undefined;
    if (!confirm(`Annuler la facture ${f.reference} ? Une note de crédit sera créée.`)) return;
    this.api.updateStatut(f.idFacture, 'ANNULEE').subscribe({
      next: (res: UpdateStatutResult) => {
        this.enrSuccess = `Facture ${res.reference} annulée. Note de crédit créée.`;
        this.loadEnregistrer();
      },
      error: (err: any) => {
        const msg = typeof err?.error === 'string' ? err.error : err?.error?.message ?? err?.message ?? 'Erreur inconnue';
        this.enrError = `Erreur sur la facture ${f.reference} : ${msg}`;
      }
    });
  }

  downloadPdf(): void {
    this.downloading = true;
    this.enrError = undefined;

    this.api.downloadPdf(this.enrMonth, this.poNumber).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Factures_${this.enrMonth}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.downloading = false;
      },
      error: () => {
        this.enrError = 'Aucune facture à télécharger ou erreur PDF.';
        this.downloading = false;
      }
    });
  }

  downloadEml(f: FactureListItem): void {
    if (this.sendingEmail.has(f.idFacture)) return;
    this.sendingEmail.add(f.idFacture);
    this.enrError = undefined;

    const isCredit = f.statutFacture === 'NOTE DE CREDIT' || f.statutFacture === 'CREDIT VALIDE';

    this.api.downloadEml(f.idFacture, this.poNumber).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        const prefix = isCredit ? 'NoteDeCredit' : 'Facture';
        a.download = `${prefix}_${f.reference.replace('/', '-')}.eml`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.sendingEmail.delete(f.idFacture);
      },
      error: (err: any) => {
        this.sendingEmail.delete(f.idFacture);
        const msg = typeof err?.error === 'string' ? err.error : err?.error?.message ?? err?.message ?? 'Erreur inconnue';
        this.enrError = `Erreur ${f.reference} : ${msg}`;
      }
    });
  }

  confirmTransmission(f: FactureListItem): void {
    if (f.statutFacture === 'TRANSMISE' || this.transmittingId.has(f.idFacture)) return;

    const isCredit = f.statutFacture === 'NOTE DE CREDIT' || f.statutFacture === 'CREDIT VALIDE';
    const label = isCredit ? 'note de crédit' : 'facture';

    if (!confirm(`Confirmer la transmission de la ${label} ${f.reference} ?\n\nLe statut passera à TRANSMISE.`)) {
      return;
    }

    this.transmittingId.add(f.idFacture);
    this.enrError = undefined;
    this.enrSuccess = undefined;

    this.api.transmettre(f.idFacture).subscribe({
      next: (res: TransmettreResult) => {
        this.transmittingId.delete(f.idFacture);
        f.statutFacture = res.statutFacture;
        f.dateTransmission = res.dateTransmission;
        const labelDone = isCredit ? 'Note de crédit' : 'Facture';
        this.enrSuccess = `${labelDone} ${res.reference} marquée comme transmise.`;
      },
      error: (err: any) => {
        this.transmittingId.delete(f.idFacture);
        const msg = typeof err?.error === 'string' ? err.error : err?.error?.message ?? err?.message ?? 'Erreur inconnue';
        this.enrError = `Erreur lors du marquage de ${f.reference} : ${msg}`;
      }
    });
  }

  // ====== Onglet 3 : Historique ======

  loadHistorique(): void {
    this.histLoading = true;
    this.histError = undefined;
    this.api.list({
      month: this.filterMonth || undefined,
      statut: this.filterStatut || undefined,
      tolkcode: this.filterTolkcode?.trim() || undefined
    }).subscribe({
      next: (res: FactureListItem[]) => {
        this.histLoading = false;
        this.factures = res;
      },
      error: (err: any) => {
        this.histLoading = false;
        this.histError = err?.error?.message ?? 'Erreur inconnue';
      }
    });
  }

  clearFilters(): void {
    this.filterMonth = '';
    this.filterStatut = '';
    this.filterTolkcode = '';
    this.loadHistorique();
  }

  get totalTtcFiltered(): number {
    return this.factures.reduce((s, f) => s + f.totalTtc, 0);
  }
}
