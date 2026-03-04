import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { InterpretesService } from '../services/interpretes.service';
import { AudienceDto } from '../dtos/interprete-dto.model';
import { TolklinkService } from '../services/tolklink.service';

@Component({
  selector: 'app-interprete-audiences',
  templateUrl: './interprete-audiences.component.html',
  styleUrls: ['./interprete-audiences.component.css']
})
export class InterpreteAudiencesComponent implements OnInit {
tolkcode = '';
rows: AudienceDto[] = [];
loading = false; error?: string; info?: string; saving = false;
sourceFilter: 'ALL' | 'VRM' | 'ANN' = 'ALL';

get filteredRows(): AudienceDto[] {
  if (this.sourceFilter === 'ALL') return this.rows;
  return this.rows.filter(r => r.source === this.sourceFilter);
}

  // Modal state
  modal: 'confirm' | 'bulk' | null = null;
  pendingAudience?: AudienceDto;
  pendingSiblings: number[] = [];

  constructor(
    private route: ActivatedRoute,
    private api: InterpretesService,
    private links: TolklinkService
  ) { }

  ngOnInit(): void {
    this.tolkcode = this.route.snapshot.paramMap.get('tolkcode') ?? '';
    this.load();
  }

  load() {
    if (!this.tolkcode) { this.error = 'Tolkcode manquant'; return; }
    this.loading = true; this.error = undefined; this.info = undefined;
    this.api.audiencesExact(this.tolkcode).subscribe({
      next: r => { this.rows = r; this.loading = false; },
      error: (_: any) => { this.error = 'Erreur de chargement.'; this.loading = false; }
    });
  }

  siblingCountOf(a: AudienceDto): number {
    return this.siblingsOf(a).length;
  }

  private siblingsOf(a: AudienceDto): number[] {
    const keyOf = (x: AudienceDto) => {
      const d = typeof x.dateAudience === 'string'
        ? x.dateAudience
        : new Date(x.dateAudience as any).toISOString().slice(0, 10);
      return `${d}|${x.heureAudience}|${x.langueRequete}`;
    };
    const key = keyOf(a);
    return this.rows.filter(x => keyOf(x) === key).map(x => x.idAffAudience);
  }

  add(a: AudienceDto) {
    if (this.saving) return;
    this.pendingAudience = a;
    this.pendingSiblings = this.siblingsOf(a);
    this.modal = 'confirm';
    this.error = undefined;
    this.info = undefined;
  }

  confirmSingle() {
    if (!this.pendingAudience) return;
    const a = this.pendingAudience;
    const sibs = this.pendingSiblings;

    if (sibs.length > 1) {
      this.modal = 'bulk';
      return;
    }

    this.saving = true;
    this.modal = null;
    const tk = Number(this.tolkcode);
    this.links.addOne(tk, a.idAffAudience).subscribe({
      next: () => {
        this.saving = false;
        this.info = 'Interprete assigne avec succes.';
        this.load();
      },
      error: (err: any) => {
        this.saving = false;
        this.error = err?.error ?? 'Erreur lors de l\'enregistrement.';
      }
    });
  }

  confirmBulk(all: boolean) {
    if (!this.pendingAudience) return;
    this.saving = true;
    this.modal = null;
    const tk = Number(this.tolkcode);

    if (all) {
      this.links.addBulk(tk, this.pendingSiblings).subscribe({
        next: (res) => {
          this.saving = false;
          this.info = `${res.inserted} audience(s) assignee(s), ${res.skipped} deja liee(s).`;
          this.load();
        },
        error: (err: any) => {
          this.saving = false;
          this.error = err?.error ?? 'Erreur lors de l\'enregistrement.';
        }
      });
    } else {
      this.links.addOne(tk, this.pendingAudience.idAffAudience).subscribe({
        next: () => {
          this.saving = false;
          this.info = 'Interprete assigne avec succes.';
          this.load();
        },
        error: (err: any) => {
          this.saving = false;
          this.error = err?.error ?? 'Erreur lors de l\'enregistrement.';
        }
      });
    }
  }

  cancelModal() {
    this.modal = null;
    this.pendingAudience = undefined;
    this.pendingSiblings = [];
  }

  dismissInfo() { this.info = undefined; }
  dismissError() { this.error = undefined; }
}
