import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // ✅ nécessaire pour routerLink / routerLinkActive
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthentificationService } from '../../services/authentification.service';
import { HDPrestationsService } from '../hd-prestations.service';
import { HDPrestationJour, HDTicket, HDAutreTache, HDSaveResultat } from '../hd-prestations.model';


function hdIsoDate(d: Date): string {
  const p = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}
function hdSemaineISO(d: Date): string {
  const utc = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
  const day = (utc.getUTCDay() + 6) % 7; // Mon=0..Sun=6
  utc.setUTCDate(utc.getUTCDate() - day + 3);
  const jan4 = new Date(Date.UTC(utc.getUTCFullYear(), 0, 4));
  const diff = (utc.getTime() - jan4.getTime()) / 86400000;
  const week = 1 + Math.floor(diff / 7);
  return `${utc.getUTCFullYear()}-W${week.toString().padStart(2, '0')}`;
}

@Component({
  selector: 'app-hd-prestation-jour',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule], // ✅ RouterModule ajouté
  templateUrl: './hd-prestation-jour.component.html',
  styleUrls: ['./hd-prestation-jour.component.css']
})
export class HDPrestationJourComponent implements OnInit {
  user = '';
  formulaire!: FormGroup;
  enregistrementEnCours = false;
  messageSauvegarde = '';
  aujourdHui = new Date();

  constructor(
    private fb: FormBuilder,
    private authService: AuthentificationService,
    private hdSvc: HDPrestationsService
  ) { }

  ngOnInit(): void {
    this.construireFormulaire();

    // Récupération utilisateur + premier chargement
    this.authService.getLogin().subscribe((name: string) => {
      this.user = name || 'anonymous';
      this.chargerJourCourant();
    });

    // Quand on change de date : réaligne les dates internes et recharge depuis le backend
    this.formulaire.get('hdDate')!.valueChanges.subscribe((dateISO: string) => {
      for (const g of this.tickets.controls) g.get('date')!.setValue(dateISO, { emitEvent: false });
      for (const g of this.autresTaches.controls) g.get('date')!.setValue(dateISO, { emitEvent: false });
      this.chargerJourCourant();
    });
  }

  // ========================
  // Formulaire
  // ========================
  private construireFormulaire(): void {
    this.formulaire = this.fb.group({
      hdDate: new FormControl<string>(hdIsoDate(this.aujourdHui), { nonNullable: true }),
      hdRegimeTravail: new FormControl<'mi-temps' | 'temps plein' | 'autre'>('temps plein', { nonNullable: true }),
      hdEnGarde: [false], // ✅ seulement un checkbox pour la garde (semaine)
      hdTickets: this.fb.array([]),
      hdAutresTaches: this.fb.array([]),
      hdRemarquesCollaborateur: ['']
    });

    // Lignes par défaut “propres”
    for (let i = 0; i < 1; i++) this.ajouterTicket();
    this.ajouterAutreTache();
  }

  get tickets(): FormArray<FormGroup> {
    return this.formulaire.get('hdTickets') as FormArray<FormGroup>;
  }
  get autresTaches(): FormArray<FormGroup> {
    return this.formulaire.get('hdAutresTaches') as FormArray<FormGroup>;
  }

  // ========================
  // Tickets
  // ========================
  ajouterTicket(): void {
    const d = this.formulaire?.value?.hdDate || hdIsoDate(this.aujourdHui);
    this.tickets.push(
      this.fb.group({
        date: [d],
        heure: [''],
        numero: [''],
        type: [''],
        dureeMin: [0, [Validators.min(0)]]
      })
    );
  }
  supprimerTicket(i: number): void {
    if (i >= 0 && i < this.tickets.length) this.tickets.removeAt(i);
  }

  // ========================
  // Autres tâches
  // ========================
  ajouterAutreTache(): void {
    const d = this.formulaire?.value?.hdDate || hdIsoDate(this.aujourdHui);
    this.autresTaches.push(
      this.fb.group({
        denomination: [''],
        date: [d],
        dureeMin: [0, [Validators.min(0)]]
      })
    );
  }
  supprimerAutreTache(i: number): void {
    if (i >= 0 && i < this.autresTaches.length) this.autresTaches.removeAt(i);
  }

  // ========================
  // Chargement / Réinjection
  // ========================
  private parseGardeVersForm(hdGarde?: string | null) {
    this.formulaire.patchValue(
      { hdEnGarde: !!(hdGarde && hdGarde.trim().toLowerCase().startsWith('oui')) },
      { emitEvent: false }
    );
  }

  private remettreTableauxVides(): void {
    const d = this.formulaire.get('hdDate')!.value as string;
    this.tickets.clear();
    this.autresTaches.clear();
    for (let i = 0; i < 2; i++) this.ajouterTicket();
    this.autresTaches.push(this.fb.group({ denomination: [''], date: [d], dureeMin: [0, [Validators.min(0)]] }));
    this.formulaire.patchValue({ hdRemarquesCollaborateur: '', hdEnGarde: false }, { emitEvent: false });
  }

  private reinjecterJour(dto: HDPrestationJour) {
    this.formulaire.patchValue(
      {
        hdDate: dto.hdDate,
        hdRegimeTravail: (dto.hdRegimeTravail as any) || 'temps plein',
        hdRemarquesCollaborateur: dto.hdRemarquesCollaborateur || ''
      },
      { emitEvent: false }
    );
    this.parseGardeVersForm(dto.hdGarde);

    const d = dto.hdDate;
    this.tickets.clear();
    (dto.hdTickets || []).forEach((t) =>
      this.tickets.push(
        this.fb.group({
          date: [d],
          heure: [t.heure || ''],
          numero: [t.numero || ''],
          type: [t.type || ''],
          dureeMin: [t.dureeMin ?? 0, [Validators.min(0)]]
        })
      )
    );
    if (this.tickets.length === 0) this.ajouterTicket();

    this.autresTaches.clear();
    (dto.hdAutresTaches || []).forEach((a) =>
      this.autresTaches.push(
        this.fb.group({
          denomination: [a.denomination || ''],
          date: [d],
          dureeMin: [a.dureeMin ?? 0, [Validators.min(0)]]
        })
      )
    );
    if (this.autresTaches.length === 0) this.ajouterAutreTache();
  }

  private chargerJourCourant(): void {
    if (!this.user) return;
    const hdDate = this.formulaire.get('hdDate')!.value as string;

    this.hdSvc.lireJour(this.user, hdDate).subscribe({
      next: (j) => {
        if (j && j.hdDate === hdDate) {
          this.reinjecterJour(j);
          this.messageSauvegarde = '📖 Données du jour rechargées.';
        } else {
          this.remettreTableauxVides();
          this.messageSauvegarde = 'ℹ️ Aucune donnée pour ce jour — tableaux vidés.';
        }
      },
      error: () => {
        this.remettreTableauxVides();
        this.messageSauvegarde = '⚠️ Impossible de recharger la journée.';
      }
    });
  }

  // ========================
  // Construction DTO / Sauvegarde
  // ========================
  private construireChampGarde(): string | undefined {
    return this.formulaire.value.hdEnGarde ? 'oui' : undefined; // garde = bool semaine
  }

  private nettoyerTicketsVides(): void {
    for (let i = this.tickets.length - 1; i >= 0; i--) {
      const r = this.tickets.at(i).value as HDTicket;
      const vide = !r.heure && !r.numero && !r.type && (!r.dureeMin || r.dureeMin === 0);
      if (vide) this.tickets.removeAt(i);
    }
  }

  private nettoyerAutresVides(): void {
    for (let i = this.autresTaches.length - 1; i >= 0; i--) {
      const r = this.autresTaches.at(i).value as HDAutreTache;
      const vide = !r.denomination && (!r.dureeMin || r.dureeMin === 0);
      if (vide) this.autresTaches.removeAt(i);
    }
  }

  private construireDto(): HDPrestationJour {
    const d = new Date(this.formulaire.value.hdDate as string);
    const dayISO = hdIsoDate(d);

    for (const g of this.tickets.controls) g.get('date')!.setValue(dayISO, { emitEvent: false });
    for (const g of this.autresTaches.controls) g.get('date')!.setValue(dayISO, { emitEvent: false });

    return {
      hdUser: this.user,
      hdDate: dayISO,
      hdSemaineISO: hdSemaineISO(d),
      hdRegimeTravail: this.formulaire.value.hdRegimeTravail as string,
      hdGarde: this.construireChampGarde(),
      hdTickets: (this.formulaire.value.hdTickets as HDTicket[]) || [],
      hdAutresTaches: (this.formulaire.value.hdAutresTaches as HDAutreTache[]) || [],
      hdRemarquesCollaborateur: (this.formulaire.value.hdRemarquesCollaborateur as string) || undefined
    };
  }
  private normaliserChampsAvantSave(): void {
    // Tickets : si 'type' est vide, on met "Ticket {numero}" ou "Ticket"
    this.tickets.controls.forEach(g => {
      const numero = (g.get('numero')?.value as string || '').trim();
      const type = (g.get('type')?.value as string || '').trim();
      if (!type) {
        g.get('type')!.setValue(numero ? `Ticket ${numero}` : 'Ticket', { emitEvent: false });
      }
    });

    // Autres tâches : si 'denomination' est vide, on met "Autre tâche"
    this.autresTaches.controls.forEach(g => {
      const denom = (g.get('denomination')?.value as string || '').trim();
      if (!denom) {
        g.get('denomination')!.setValue('Autre tâche', { emitEvent: false });
      }
    });
  }

  enregistrerJour(): void {
    const snapshot = { t: this.tickets.length, a: this.autresTaches.length };
    this.nettoyerTicketsVides();
    this.nettoyerAutresVides();
    this.normaliserChampsAvantSave();
    const dto = this.construireDto();

    this.enregistrementEnCours = true;
    this.messageSauvegarde = 'Enregistrement...';

    this.hdSvc.enregistrerJour(dto).subscribe({
      next: (res: HDSaveResultat) => {
        this.messageSauvegarde = res.ok ? '✅ Enregistré' : `⚠️ ${res.message || 'Échec enregistrement'}`;
        this.enregistrementEnCours = false;
        this.chargerJourCourant(); // relecture immédiate
        while (this.tickets.length < Math.max(1, snapshot.t)) this.ajouterTicket();
        while (this.autresTaches.length < Math.max(1, snapshot.a)) this.ajouterAutreTache();
      },
      error: (err: unknown) => {
        this.messageSauvegarde = `❌ Erreur: ${err instanceof Error ? err.message : String(err)}`;
        this.enregistrementEnCours = false;
      }
    });
  }

  exporterJsonLocal(): void {
    const dto = this.construireDto();
    const blob = new Blob([JSON.stringify(dto, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${dto.hdUser}_${dto.hdSemaineISO}_${dto.hdDate}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
