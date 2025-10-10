import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // ✅ nécessaire pour routerLink / routerLinkActive
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AuthentificationService } from '../../services/authentification.service';
import { HDPrestationsService } from '../hd-prestations.service';
import { HDPrestationJour } from '../hd-prestations.model';

// "YYYY-Www" pour la semaine ISO du jour
function isoWeekToday(): string {
  const d = new Date();
  const dt = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  const j = (dt.getUTCDay() + 6) % 7; dt.setUTCDate(dt.getUTCDate() - j + 3);
  const j1 = new Date(Date.UTC(dt.getUTCFullYear(), 0, 4));
  const diff = (dt.getTime() - j1.getTime()) / 86400000;
  const w = 1 + Math.floor(diff / 7);
  return `${dt.getUTCFullYear()}-W${w.toString().padStart(2, '0')}`;
}
function minutes(v?: number) { return v && v > 0 ? v : 0; }

// Lundi local à partir d'une semaine ISO "YYYY-Www"
function mondayFromISOWeek(week: string): Date {
  const y = parseInt(week.slice(0, 4), 10);
  const w = parseInt(week.slice(6), 10);
  const jan4 = new Date(Date.UTC(y, 0, 4));
  const day = (jan4.getUTCDay() + 6) % 7; // Mon=0..Sun=6
  const monW1 = new Date(jan4); monW1.setUTCDate(jan4.getUTCDate() - day);
  const mon = new Date(monW1); mon.setUTCDate(monW1.getUTCDate() + (w - 1) * 7);
  return new Date(mon.getUTCFullYear(), mon.getUTCMonth(), mon.getUTCDate());
}
function hdIsoDate(d: Date): string {
  const p = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

type JourVue = {
  nom: string;
  dateISO: string; // YYYY-MM-DD
  data?: HDPrestationJour;
  ouvert: boolean;
};

@Component({
  selector: 'app-hd-recap-semaine',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule], // ✅ RouterModule ajouté
  templateUrl: './hd-recap-semaine.component.html',
  styleUrls: ['./hd-recap-semaine.component.css']
})
export class HDRecapSemaineComponent implements OnInit {
  user = '';
  formulaire!: FormGroup;
  semaineISO = '';
  donnees: HDPrestationJour[] = [];
  jours: JourVue[] = [];
  chargement = false;
  message = '';

  constructor(
    private fb: FormBuilder,
    private auth: AuthentificationService,
    private svc: HDPrestationsService
  ) { }

  ngOnInit(): void {
    this.auth.getLogin().subscribe((u: string) => {
      this.user = u || 'anonymous';
      const currentWeek = isoWeekToday();
      this.semaineISO = currentWeek;

      this.formulaire = this.fb.group({
        // contrôle de type "week" => valeur "YYYY-Www"
        hdSemaine: new FormControl<string>(currentWeek, { nonNullable: true })
      });

      this.onSemaineChange();
      this.formulaire.get('hdSemaine')!.valueChanges.subscribe(() => this.onSemaineChange());
    });
  }

  private onSemaineChange(): void {
    this.message = '';
    const w = this.formulaire.get('hdSemaine')!.value as string;
    this.semaineISO = w || isoWeekToday();
    this.initialiserJours(this.semaineISO);
    this.chargerSemaine();
  }

  private initialiserJours(semISO: string): void {
    const lundi = mondayFromISOWeek(semISO);
    const noms = ['Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi'];
    this.jours = noms.map((nom, i) => {
      const d = new Date(lundi); d.setDate(lundi.getDate() + i);
      return { nom, dateISO: hdIsoDate(d), data: undefined, ouvert: i === 0 }; // lundi ouvert par défaut
    });
  }

  private chargerSemaine(): void {
    this.chargement = true;
    this.svc.lireSemaine(this.user, this.semaineISO).subscribe({
      next: (rows: HDPrestationJour[]) => {
        this.donnees = (rows || []).sort((a, b) => a.hdDate.localeCompare(b.hdDate));
        const map = new Map(this.donnees.map(j => [j.hdDate, j] as const));
        this.jours.forEach(j => j.data = map.get(j.dateISO));
        this.chargement = false;
      },
      error: (err: unknown) => {
        this.message = `Erreur: ${err instanceof Error ? err.message : String(err)}`;
        this.chargement = false;
      }
    });
  }

  // ===== Calculs pour le template
  totalJourMinutes(j: JourVue): number {
    const t = (j.data?.hdTickets || []).reduce((s, x) => s + minutes(x.dureeMin), 0);
    const a = (j.data?.hdAutresTaches || []).reduce((s, x) => s + minutes(x.dureeMin), 0);
    return t + a;
  }
  totalMinutesTickets(): number {
    return this.jours.reduce((s, j) =>
      s + (j.data?.hdTickets || []).reduce((x, t) => x + minutes(t.dureeMin), 0), 0);
  }
  totalMinutesAutres(): number {
    return this.jours.reduce((s, j) =>
      s + (j.data?.hdAutresTaches || []).reduce((x, a) => x + minutes(a.dureeMin), 0), 0);
  }
  totalMinutes(): number { return this.totalMinutesTickets() + this.totalMinutesAutres(); }

  toggleAll(plie: boolean): void { this.jours.forEach(j => j.ouvert = !plie); }

  telechargerWord(): void {
    this.svc.telechargerSemaineWord(this.user, this.semaineISO).subscribe((blob: Blob) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `FicheHelpdesk_${this.user}_${this.semaineISO}.docx`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }
}
