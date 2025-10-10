import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { LanguesService, LangueRef, LangueSource, LangueDestination } from '../services/langues.service';

@Component({
  selector: 'app-langues',
  templateUrl: './langues.component.html',
  styleUrls: ['./langues.component.css']
})
export class LanguesComponent implements OnInit {
  tolkcode = 0;
  loading = false;
  error?: string;

  refForSource: LangueRef[] = [];
  refForDestination: LangueRef[] = [];
  sources: LangueSource[] = [];
  dests: LangueDestination[] = [];

  addSrc: number | null = null;
  addDst: number | null = null;

  constructor(
    private route: ActivatedRoute,
    private languesService: LanguesService
  ) { }

  ngOnInit(): void {
    const raw = this.route.snapshot.paramMap.get('tolkcode');
    this.tolkcode = raw ? Number(raw) : NaN;
    if (Number.isNaN(this.tolkcode)) {
      this.error = 'Paramètre tolkcode manquant dans l’URL.';
      return;
    }
    this.loadAll();
  }

  private loadAll() {
    this.loading = true;
    this.error = undefined;

    // Charge en parallèle :
    // - ref source = toutes les langues
    // - ref destination = seulement langues avec IslangueDestination != null (API: ?destOnly=true)
    // - listes de l’interprète
    forkJoin({
      refSrc: this.languesService.listRef(false),
      refDst: this.languesService.listRef(true),
      sources: this.languesService.listSources(this.tolkcode),
      dests: this.languesService.listDestinations(this.tolkcode)
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ refSrc, refDst, sources, dests }) => {
          this.refForSource = refSrc;
          this.refForDestination = refDst;
          this.sources = sources;
          this.dests = dests;
        },
        error: () => {
          this.error = 'Erreur lors du chargement des langues.';
        }
      });
  }

  addSource() {
    if (this.addSrc == null) return; // évite le 0/undefined/null
    this.languesService.addSource(this.tolkcode, this.addSrc).subscribe({
      next: () => {
        this.addSrc = null;
        this.loadAll();
      },
      error: () => (this.error = 'Impossible d’ajouter la langue source')
    });
  }

  removeSource(id: number) {
    this.languesService.removeSource(this.tolkcode, id).subscribe({
      next: () => this.loadAll(),
      error: () => (this.error = 'Impossible de supprimer la langue source')
    });
  }

  addDestination() {
    if (this.addDst == null) return;
    this.languesService.addDestination(this.tolkcode, this.addDst).subscribe({
      next: () => {
        this.addDst = null;
        this.loadAll();
      },
      error: () => (this.error = 'Impossible d’ajouter la langue destination')
    });
  }

  removeDestination(id: number) {
    this.languesService.removeDestination(this.tolkcode, id).subscribe({
      next: () => this.loadAll(),
      error: () => (this.error = 'Impossible de supprimer la langue destination')
    });
  }
}
