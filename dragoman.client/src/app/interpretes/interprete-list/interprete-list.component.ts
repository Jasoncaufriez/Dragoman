import { Component } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { InterpretesService } from '../../services/interpretes.service';
import { LanguesService } from '../../services/langues.service';

function phoneValidator(ctrl: AbstractControl) {
  const v: string = (ctrl.value || '').trim().replace(/[ .\-\/]/g, '');
  if (!v) return null;
  return /^(\+32|0)[1-9]\d{7,9}$/.test(v) ? null : { phone: true };
}

function tvaValidator(ctrl: AbstractControl) {
  const v: string = (ctrl.value || '').trim().replace(/[ .]/g, '').toUpperCase();
  if (!v) return null;
  return /^BE\d{10}$/.test(v) ? null : { tva: true };
}

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
  createForm: FormGroup;

  langues: { id: number; libelle: string }[] = [];
  rows: InterpreteMatch[] = [];
  quickRows: { tolkcode: string; nom?: string; prenom?: string; languesDestination: string[]; languesSource: string[] }[] = [];
  quickQuery = '';
  showQuickResults = false;
  showCreateForm = false;
  creating = false;
  createError?: string;
  createSuccess?: string;
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

    this.createForm = fb.group({
      nom:         ['', Validators.required],
      prenom:      [''],
      email:       ['', [Validators.email]],
      tel:         ['', [phoneValidator]],
      telbis:      ['', [phoneValidator]],
      gsm:         ['', [phoneValidator]],
      tva:         ['', [tvaValidator]],
      iban:        [''],
      bankrekening:[''],
      taalrol:     [null],
      beedigd:     [0],
      genre:       ['']
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

  runQuickSearch() {
    if (!this.quickQuery || !this.quickQuery.trim()) return;
    const q = this.quickQuery.trim();
    const isNumber = /^\d+$/.test(q);
    const mode = isNumber ? 'tolkcode' as const : 'nom' as const;

    this.loading = true;
    this.error = undefined;
    this.showQuickResults = true;
    this.rows = [];

    this.api.search(mode, q).subscribe({
      next: (r) => { this.quickRows = r; this.loading = false; },
      error: () => { this.error = 'Erreur de recherche.'; this.loading = false; }
    });
  }

  runAdvanced() {
    const v = this.advForm.value;
    if (!v.langSrc || !v.langDst || !v.date) {
      this.error = 'Langue source, langue destination et date sont requis.';
      return;
    }
    this.loading = true; this.error = undefined;
    this.showQuickResults = false;
    this.quickRows = [];

    this.api.match({
      langSrc: v.langSrc,
      langDst: v.langDst,
      date: v.date
    }).subscribe({
      next: (r) => { this.rows = r; this.loading = false; },
      error: (_err) => { this.error = 'Erreur de recherche.'; this.loading = false; }
    });
  }

  openCreateForm() {
    this.showCreateForm = !this.showCreateForm;
    this.createError = undefined;
    this.createSuccess = undefined;
    if (this.showCreateForm) this.createForm.reset({ beedigd: 0, taalrol: null, genre: '' });
  }

  submitCreate() {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.creating = true;
    this.createError = undefined;
    this.createSuccess = undefined;

    const v = this.createForm.value;
    this.api.create({
      nom:         v.nom?.trim().toUpperCase(),
      prenom:      v.prenom?.trim() || undefined,
      email:       v.email?.trim() || undefined,
      tel:         v.tel?.trim() || undefined,
      telbis:      v.telbis?.trim() || undefined,
      gsm:         v.gsm?.trim() || undefined,
      tva:         v.tva?.trim().replace(/[ .]/g, '').toUpperCase() || undefined,
      iban:        v.iban?.trim() || undefined,
      bankrekening:v.bankrekening?.trim() || undefined,
      taalrol:     v.taalrol ?? undefined,
      beedigd:     v.beedigd ?? 0,
      genre:       v.genre?.trim() || undefined
    }).subscribe({
      next: (res) => {
        this.creating = false;
        this.createSuccess = `Interprète créé avec le tolkcode ${res.tolkcode} — ${res.nom} ${res.prenom ?? ''}`;
        this.showCreateForm = false;
        this.createForm.reset({ beedigd: 0, taalrol: null, genre: '' });
      },
      error: (err) => {
        this.creating = false;
        const errs = err?.error?.errors as string[];
        this.createError = errs?.length ? errs.join(' | ') : (err?.error ?? 'Erreur inconnue');
      }
    });
  }

  openDetail(tolkcode: number | string) {
    this.router.navigate(['/interpretes', tolkcode, 'audiences']);
  }
}
