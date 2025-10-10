import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { AdressesService, Tolkadresse } from '../services/adresses.service';

@Component({
  selector: 'app-adresses',
  templateUrl: './adresses.component.html',
  styleUrls: ['./adresses.component.css']
})
export class AdressesComponent implements OnInit {
  tolkcode = 0;
  loading = false;
  saving = false;
  error?: string;
  rows: Tolkadresse[] = [];
  onlyActive = true;

  editingId: number | null = null;
  formVisible = false; // <-- pilotage d’affichage du formulaire

  f = this.fb.nonNullable.group({
    land: ['', [Validators.required, Validators.maxLength(50)]],
    cp: ['', [Validators.required, Validators.maxLength(10)]],
    commune: ['', [Validators.required, Validators.maxLength(100)]],
    rue: [''],
    numero: [''],
    boite: [''],
    km: [null as number | null],
    startdate: ['', Validators.required], // yyyy-MM-dd
    enddate: ['']
  });

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private svc: AdressesService
  ) { }

  ngOnInit(): void {
    const raw = this.route.snapshot.paramMap.get('tolkcode');
    this.tolkcode = raw ? Number(raw) : 0;
    this.resetForm();
    this.refresh();
  }

  refresh() {
    this.loading = true;
    this.error = undefined;
    this.svc.list(this.tolkcode, this.onlyActive).subscribe({
      next: (data) => { this.rows = data; this.loading = false; },
      error: (err: any) => { this.error = 'Erreur de chargement des adresses.'; console.error(err); this.loading = false; }
    });
  }

  toggleActive() {
    this.onlyActive = !this.onlyActive;
    this.refresh();
  }

  todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  resetForm() {
    this.editingId = null;
    this.f.reset({
      land: '',
      cp: '',
      commune: '',
      rue: '',
      numero: '',
      boite: '',
      km: null,
      startdate: this.todayIso(),
      enddate: ''
    });
  }

  create() {
    this.editingId = null;
    this.formVisible = true; // <-- affiche le form
    this.f.patchValue({ startdate: this.todayIso(), enddate: '' });
  }

  edit(row: Tolkadresse) {
    this.editingId = row.idAdresse;
    this.formVisible = true; // <-- affiche le form
    this.f.patchValue({
      land: row.land ?? '',
      cp: row.cp ?? '',
      commune: row.commune ?? '',
      rue: row.rue ?? '',
      numero: row.numero ?? '',
      boite: row.boite ?? '',
      km: row.km ?? null,
      startdate: toIso(row.startdate) ?? this.todayIso(),
      enddate: toIso(row.enddate) ?? ''
    });
  }

  cancel() {
    this.formVisible = false; // <-- cache le form
    this.resetForm();
  }

  submit() {
    if (this.f.invalid) { this.f.markAllAsTouched(); return; }
    this.saving = true;

    const body: Partial<Tolkadresse> = {
      land: this.f.value.land!, cp: this.f.value.cp!, commune: this.f.value.commune!,
      rue: this.f.value.rue || null, numero: this.f.value.numero || null, boite: this.f.value.boite || null,
      km: this.f.value.km ?? null,
      startdate: this.f.value.startdate!, // yyyy-MM-dd
      enddate: this.f.value.enddate ? this.f.value.enddate : null
    };

    const onDone = () => { this.saving = false; this.formVisible = false; this.resetForm(); this.refresh(); };
    const onErr = (err: any) => { this.saving = false; this.error = 'Échec de l’enregistrement.'; console.error(err); };

    if (this.editingId) {
      // Edition simple de la ligne existante
      this.svc.update(this.editingId, body).subscribe({ next: onDone, error: onErr });
    } else {
      // Nouvelle adresse = logique métier "replace" (clôture l’active + crée la nouvelle)
      this.svc.replace(this.tolkcode, body).subscribe({ next: onDone, error: onErr });
    }
  }

  remove(row: Tolkadresse) {
    if (!confirm('Supprimer cette adresse ?')) return;
    this.svc.remove(row.idAdresse).subscribe({
      next: () => this.refresh(),
      error: (err: any) => { this.error = 'Échec de la suppression.'; console.error(err); }
    });
  }
}

function toIso(v: any): string | null {
  if (!v) return null;
  if (typeof v === 'string' && /^\d{4}-\d{2}-\d{2}/.test(v)) return v.slice(0, 10);
  const d = new Date(v);
  return isNaN(d.getTime()) ? null : d.toISOString().slice(0, 10);
}
