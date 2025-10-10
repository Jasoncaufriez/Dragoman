// src/app/reports/presence-interpretes/presence-interpretes.component.ts
import { Component, OnInit } from '@angular/core';
import { ReportsService } from '../services/reports.service';

export type AudienceItem = { heure?: string; salle?: string; langue?: string };
export type InterpretePresence = {
  tolkcode: number;
  nom: string;
  prenom: string;
  telephone?: string;
  audiences: AudienceItem[];
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

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
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
}
