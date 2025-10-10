import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { InterpretesService } from '../../services/interpretes.service';

type SectionKey = 'identite' | 'contact' | 'langueStatut' | 'banqueTva' | 'entreprise' | 'divers';

@Component({
  selector: 'app-interprete-detail',
  templateUrl: './interprete-detail.component.html',
  styleUrls: ['./interprete-detail.component.css']
})
export class InterpreteDetailComponent implements OnInit {
  public tolkcode = 0;
  public fIdent!: FormGroup;
  public loading = false;
  public saving = false;
  public error?: string;
  public saved = false;

  // Ouverture des sections (par défaut seules les 3 premières)
  public open: Record<SectionKey, boolean> = {
    identite: true,
    contact: true,
    langueStatut: true,
    banqueTva: false,
    entreprise: false,
    divers: false,
  };

  public showAdvanced = false; // bascule globale pour sections avancées

  private origPhones = { gsm: '', tel: '', telbis: '' };

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private service: InterpretesService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(pm => {
      const raw = pm.get('tolkcode');
      this.tolkcode = raw ? Number(raw) : NaN;
      if (Number.isNaN(this.tolkcode)) {
        this.error = 'Paramètre tolkcode manquant dans l’URL.';
        return;
      }
      this.buildForm();
      this.loadIdentite();
    });
  }

  private buildForm() {
    this.fIdent = this.fb.group({
      tolkcode: [{ value: null, disabled: true }],

      // --- Identité (essentiel)
      nom: ['', [Validators.required, Validators.maxLength(50)]],
      prenom: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.email, Validators.maxLength(80)]],
      dateNaissance: [null],
      genre: ['', [Validators.maxLength(1)]],

      // --- Contact
      telephone1: ['', [Validators.maxLength(40)]],
      telephone2: ['', [Validators.maxLength(40)]],
      fax: ['', [Validators.maxLength(40)]],

      // --- Langue & Statut
      // taalrol: 1=Français, 2=Néerlandais
      taalrol: [null, []],
      // beedigd: 1=assermenté, 0=non
      beedigd: [0, []],
      nationaliteit: ['', [Validators.maxLength(50)]],
      rijksregisternr: ['', [Validators.maxLength(50)]],
      herkomst: ['', [Validators.maxLength(20)]],
      beroepscode: [null],

      // --- Banque & TVA
      iban: ['', [Validators.maxLength(34)]],
      bankrekening: ['', [Validators.maxLength(34)]],
      tva: ['', [Validators.maxLength(20)]],
      btwNr: [null],

      // --- Entreprise (avancé)
      fedcom: [null],
      ondernemingsnummer: [null],
      vestigingsnummer: ['', [Validators.maxLength(10)]],
      fedcomnummer: [null],

      // --- Divers (avancé)
      remarque: ['', [Validators.maxLength(250)]],
      evaluatiecode: [null],
      ba: ['', [Validators.maxLength(11)]],
      iscce: ['', [Validators.maxLength(1)]],
    });
  }

  private loadIdentite() {
    this.loading = true;
    this.error = undefined;
    this.service.getIdentite(this.tolkcode).subscribe({
      next: (data: any) => {
        // Téléphones depuis l’API/DB
        const gsm = data.gsm ?? data.Gsm ?? data.GSM ?? '';
        const tel = data.tel ?? data.Tel ?? data.TEL ?? '';
        const telbis = data.telbis ?? data.Telbis ?? data.TELBIS ?? '';
        this.origPhones = { gsm, tel, telbis };
        const candidates = uniq([gsm, tel, telbis]);
        const telephone1 = candidates[0] ?? '';
        const telephone2 = candidates[1] ?? '';

        this.fIdent.patchValue({
          tolkcode: this.tolkcode,

          // Identité
          nom: data.nom ?? data.Nom ?? '',
          prenom: data.prenom ?? data.Prenom ?? '',
          email: data.email ?? data.Email ?? '',
          dateNaissance: toIsoDate(data.dateNaissance ?? data.DateNaissance ?? data.DATE_NAISSANCE),
          genre: data.genre ?? data.Genre ?? data.GENRE ?? '',

          // Contact
          fax: data.fax ?? data.Fax ?? '',
          telephone1,
          telephone2,

          // Langue & Statut
          taalrol: to12(data.taalrol ?? data.Taalrol),
          beedigd: to01(data.beedigd ?? data.Beedigd),
          nationaliteit: data.nationaliteit ?? data.Nationaliteit ?? data.NATIONALITEIT ?? '',
          rijksregisternr: data.rijksregisternr ?? data.Rijksregisternr ?? data.RIJKSREGISTERNR ?? '',
          herkomst: data.herkomst ?? data.Herkomst ?? data.HERKOMST ?? '',
          beroepscode: toNum(data.beroepscode ?? data.Beroepscode ?? data.BEROEPSCODE),

          // Banque & TVA
          iban: data.iban ?? data.Iban ?? data.IBAN ?? '',
          bankrekening: data.bankrekening ?? data.Bankrekening ?? data.BANKREKENING ?? '',
          tva: data.tva ?? data.Tva ?? data.TVA ?? '',
          btwNr: toNum(data.btwNr ?? data.BtwNr ?? data.BTW_NR),

          // Entreprise
          fedcom: toNum(data.fedcom ?? data.Fedcom ?? data.FEDCOM),
          ondernemingsnummer: toNum(data.ondernemingsnummer ?? data.Ondernemingsnummer ?? data.ONDERNEMINGSNUMMER),
          vestigingsnummer: data.vestigingsnummer ?? data.Vestigingsnummer ?? data.VESTIGINGSNUMMER ?? '',
          fedcomnummer: toNum(data.fedcomnummer ?? data.Fedcomnummer ?? data.FEDCOMNUMMER),

          // Divers
          remarque: data.remarque ?? data.Remarque ?? data.REMARQUE ?? '',
          evaluatiecode: toNum(data.evaluatiecode ?? data.Evaluatiecode ?? data.EVALUATIECODE),
          ba: data.ba ?? data.Ba ?? data.BA ?? '',
          iscce: data.iscce ?? data.Iscce ?? data.ISCCE ?? '',
        });

        // Ouvrir automatiquement une section si elle contient des erreurs
        this.openSectionsWithErrors();

        this.loading = false;
      },
      error: (err) => {
        this.error = 'Erreur lors du chargement de l’identité.';
        console.error(err);
        this.loading = false;
      }
    });
  }

  saveIdent() {
    if (this.fIdent.invalid) {
      this.fIdent.markAllAsTouched();
      this.openSectionsWithErrors();
      return;
    }
    this.saving = true;
    this.saved = false;
    this.error = undefined;

    const raw = this.fIdent.getRawValue() as any;

    // Recompose gsm/tel/telbis à partir de telephone1/2
    const p1 = sanitizePhone(raw.telephone1);
    const p2 = sanitizePhone(raw.telephone2);
    const numbers = uniq([p1, p2]).filter(v => v && v.length > 0);

    let gsm: string | null = null, tel: string | null = null, telbis: string | null = null;
    const mobiles = numbers.filter(isBelgianMobile);
    const land = numbers.filter(n => !isBelgianMobile(n));
    if (mobiles[0]) gsm = mobiles[0];
    if (land[0]) tel = land[0];
    const used = uniq([gsm ?? '', tel ?? '']).filter(Boolean);
    const rest = numbers.filter(n => !used.includes(n));
    if (rest[0]) telbis = rest[0];

    const payload: any = {
      ...raw,
      tolkcode: this.tolkcode,
      beedigd: to01(raw.beedigd),
      taalrol: to12(raw.taalrol),
      dateNaissance: raw.dateNaissance ?? null,
      gsm: numbers.length ? gsm : null,
      tel: numbers.length ? tel : null,
      telbis: numbers.length ? telbis : null
    };

    delete payload.telephone1;
    delete payload.telephone2;

    this.service.saveIdentite(this.tolkcode, payload).subscribe({
      next: () => {
        this.saving = false;
        this.saved = true;
        setTimeout(() => (this.saved = false), 2500);
      },
      error: (err) => {
        this.saving = false;
        this.error = 'Échec de l’enregistrement.';
        console.error(err);
      }
    });
  }

  // ---- UI helpers ----
  toggleSection(key: SectionKey) { this.open[key] = !this.open[key]; }
  toggleAdvanced() {
    this.showAdvanced = !this.showAdvanced;
    this.open.entreprise = this.showAdvanced;
    this.open.divers = this.showAdvanced;
  }

  invalidCount(ctrlNames: string[]): number {
    let n = 0;
    for (const name of ctrlNames) {
      const c = this.fIdent.get(name);
      if (c && c.invalid && (c.touched || c.dirty)) n++;
    }
    return n;
  }

  private openSectionsWithErrors() {
    // Ouvre les sections qui contiennent des erreurs
    const map: Record<SectionKey, string[]> = {
      identite: ['nom', 'prenom', 'email', 'dateNaissance', 'genre'],
      contact: ['telephone1', 'telephone2', 'fax'],
      langueStatut: ['taalrol', 'beedigd', 'nationaliteit', 'rijksregisternr', 'herkomst', 'beroepscode'],
      banqueTva: ['iban', 'bankrekening', 'tva', 'btwNr'],
      entreprise: ['fedcom', 'ondernemingsnummer', 'vestigingsnummer', 'fedcomnummer'],
      divers: ['remarque', 'evaluatiecode', 'ba', 'iscce'],
    };
    (Object.keys(map) as SectionKey[]).forEach(k => {
      const hasErr = this.invalidCount(map[k]) > 0;
      if (hasErr) this.open[k] = true;
    });
  }
}

/* ---------- helpers ---------- */
function sanitizePhone(raw: string | null | undefined): string {
  return (raw ?? '').replace(/[^\d+]/g, '').replace(/\s+/g, '').trim();
}
function isBelgianMobile(p: string): boolean {
  const x = sanitizePhone(p);
  return x.startsWith('04') || x.startsWith('+324') || x.startsWith('324');
}
function uniq<T>(arr: T[]): T[] { return Array.from(new Set(arr.filter(Boolean))); }
function toNum(v: any): number | null { const n = Number(v); return Number.isFinite(n) ? n : null; }
function to01(v: any): 0 | 1 { return Number(v) === 1 ? 1 : 0; }
function to12(v: any): 1 | 2 | null {
  const n = Number(v);
  if (n === 1) return 1;
  if (n === 2) return 2;
  return null;
}
function toIsoDate(v: any): string | null {
  if (!v) return null;
  if (typeof v === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(v)) return v;
  const d = new Date(v);
  return isNaN(d.getTime()) ? null : d.toISOString().slice(0, 10);
}
