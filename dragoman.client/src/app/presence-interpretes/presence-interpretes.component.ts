// src/app/reports/presence-interpretes/presence-interpretes.component.ts
import { Component, OnInit } from '@angular/core';
import { ReportsService } from '../services/reports.service';

export type AudienceItem = { heure?: string; salle?: string; langue?: string; magistrat?: string; nbAffaires?: number };
export type InterpretePresence = {
  tolkcode: number;
  nom: string;
  prenom: string;
  telephones?: string[];
  frNl?: string;
  nbAffaires?: number;
  audiences: AudienceItem[];
};
export type FlatRow = {
  interprete: InterpretePresence;
  audience: AudienceItem | null;
};

@Component({
  selector: 'app-presence-interpretes',
  templateUrl: './presence-interpretes.component.html',
  styleUrls: ['./presence-interpretes.component.css'],
})
export class PresenceInterpretesComponent implements OnInit {
  date = new Date().toISOString().slice(0, 10); // yyyy-MM-dd
  data: InterpretePresence[] = [];
  loading = false;

  constructor(private reports: ReportsService) { }

  ngOnInit(): void {
    this.reload();
  }
  trackByTolkcode(index: number, item: InterpretePresence): number {
    return item?.tolkcode ?? index;
  }

  get flatRows(): FlatRow[] {
    const rows: FlatRow[] = [];
    for (const i of this.data) {
      if (i.audiences && i.audiences.length > 0) {
        for (const a of i.audiences) {
          rows.push({ interprete: i, audience: a });
        }
      } else {
        rows.push({ interprete: i, audience: null });
      }
    }
    return rows;
  }

  trackByRow(index: number, item: FlatRow): string {
    return `${item.interprete?.tolkcode ?? index}-${item.audience?.heure ?? ''}-${item.audience?.salle ?? ''}`;
  }
  reload(): void {
    this.loading = true;
    this.reports.getInterpretes(this.date).subscribe({
      next: (d: InterpretePresence[]) => {
        this.data = Array.isArray(d) ? d : [];
        this.loading = false;
      },
      error: (_err: unknown) => {
        this.data = [];
        this.loading = false;
      },
    });
  }

  exportExcel(): void {
    this.reports.downloadExcel(this.date).subscribe((blob: Blob) => {
      this.saveBlob(blob, `Presence_Interpretes_${this.date}.xlsx`);
    });
  }

  exportWord(): void {
    this.reports.downloadWord(this.date).subscribe((blob: Blob) => {
      this.saveBlob(blob, `Presence_Interpretes_${this.date}.docx`);
    });
  }

  exportPdf(): void {
    this.reports.downloadPdf(this.date).subscribe((blob: Blob) => {
      this.saveBlob(blob, `Presence_Interpretes_${this.date}.pdf`);
    });
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
  }

  /**
   * Normalise un numéro de téléphone pour le protocole tel: (utilisé par Teams).
   * Supprime les espaces, points, tirets et slashes.
   * Convertit un numéro belge commençant par 0 en format international +32.
   */
  formatPhone(raw: string): string {
    let cleaned = raw.replace(/[\s.\-\/()]/g, '');
    if (cleaned.startsWith('00')) {
      cleaned = '+' + cleaned.substring(2);
    } else if (cleaned.startsWith('0')) {
      cleaned = '+32' + cleaned.substring(1);
    } else if (!cleaned.startsWith('+')) {
      cleaned = '+32' + cleaned;
    }
    return cleaned;
  }

  // ==== Helpers pour le template (remplacent map() avec => dans le HTML) ====
  joinHeures(auds?: AudienceItem[]): string {
    return (auds ?? []).map(a => a.heure).filter(Boolean).join(', ');
  }
  joinSalles(auds?: AudienceItem[]): string {
    return (auds ?? []).map(a => a.salle).filter(Boolean).join(', ');
  }
  joinLangues(auds?: AudienceItem[]): string {
    return (auds ?? []).map(a => a.langue).filter(Boolean).join(', ');
  }
  joinMagistrats(auds?: AudienceItem[]): string {
    const names = (auds ?? []).map(a => a.magistrat).filter(Boolean);
    return [...new Set(names)].join(', ');
  }
}
