import { Component } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup } from '@angular/forms';
import { InterpretesService } from '../../services/interpretes.service';
import { LanguesService } from '../../services/langues.service';

interface InterpreteMatch {
  tolkcode: number;
  nom?: string;
  prenom?: string;
  tel?: string;
  telbis?: string;
  gsm?: string;
  languesDestination: string[];
  distanceKm?: number | null;
}

@Component({
  selector: 'app-interprete-list',
  templateUrl: './interprete-list.component.html'
})
export class InterpreteListComponent {
  advForm: FormGroup;

  langues: { id: number; libelle: string }[] = [];
  rows: InterpreteMatch[] = [];
  loading = false;
  error?: string;

  constructor(
    private api: InterpretesService,
    private languesSvc: LanguesService,
    private router: Router,
    private route: ActivatedRoute,
    fb: FormBuilder
  ) {
    const todayISO = new Date().toISOString().slice(0, 10);
    this.advForm = fb.group({
      langSrc: [null],
      langDst: [null],
      date: [todayISO]
    });

    this.loadLangues();
  }

  private loadLangues() {
    // Récupère le référentiel des langues depuis /api/langues
    this.languesSvc.listRef(/* destOnly */ false).subscribe({
      next: (ls) => {
        this.langues = (ls || []).map(l => ({
          id: l.idlangue,
          libelle: l.libelleFr ?? l.libelleNl ?? l.codeIso ?? `#${l.idlangue}`
        }));
        // Après chargement des langues, applique le pré-remplissage éventuel
        this.applyPrefillFromQuery();
      },
      error: () => {
        this.langues = [];
        this.applyPrefillFromQuery(); // tente quand même (date au moins)
      }
    });
  }

  /** Pré-remplir depuis les query params (date, langSrcLbl, langDstLbl) */
  private applyPrefillFromQuery() {
    const q = this.route.snapshot.queryParamMap;
    const date = q.get('date') || '';
    const srcLbl = (q.get('langSrcLbl') || '').toLowerCase();
    const dstLbl = (q.get('langDstLbl') || '').toLowerCase();

    const findIdByLabel = (lbl: string | null): number | null => {
      if (!lbl) return null;
      const x = this.langues.find(l => l.libelle.toLowerCase() === lbl.toLowerCase());
      return x ? x.id : null;
    };

    const langSrcId = findIdByLabel(srcLbl);
    const langDstId = findIdByLabel(dstLbl);

    const patch: any = {};
    if (date) patch.date = date;
    if (langSrcId != null) patch.langSrc = langSrcId;
    if (langDstId != null) patch.langDst = langDstId;

    if (Object.keys(patch).length) {
      this.advForm.patchValue(patch, { emitEvent: true });
    }
  }

  resetAdvanced() {
    this.error = undefined;
    this.advForm.reset({ langSrc: null, langDst: null, date: new Date().toISOString().slice(0, 10) });
  }

  runAdvanced() {
    const v = this.advForm.value;
    if (!v.langSrc || !v.langDst || !v.date) {
      this.error = 'Langue source, langue destination et date sont requis.';
      return;
    }
    this.loading = true; this.error = undefined;

    this.api.match({
      langSrc: v.langSrc,
      langDst: v.langDst,
      date: v.date
    }).subscribe({
      next: (r) => { this.rows = r; this.loading = false; },
      error: (_err) => { this.error = 'Erreur de recherche.'; this.loading = false; }
    });
  }

  openDetail(tolkcode: number) {
    this.router.navigate(['/interpretes', tolkcode, 'audiences']);
  }
}
