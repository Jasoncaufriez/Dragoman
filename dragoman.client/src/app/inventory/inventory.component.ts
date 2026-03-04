import { Component, OnInit } from '@angular/core';
import { InventoryService } from '../services/inventory.service';
import { MachineRecord } from '../models/machine-record';

@Component({
  selector: 'app-inventory',
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.css']
})
export class InventoryComponent implements OnInit {

  machines: MachineRecord[] = [];
  loading = false;
  selectedFile: File | null = null;
  errorMessage = '';

  // Filtres
  filterText = '';
  filterLocalisation = 'all';   // all | bureau | tt | injoignable | autre
  filterVerified = 'all';       // all | yes | no

  // Filtre version précise
  versionOptions: string[] = []; // ex: ["6.3.3", "6.2.8"]
  filterVersion = 'all';         // all | none | "6.3.3" | ...

  constructor(private inventoryService: InventoryService) { }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.inventoryService.getAll().subscribe({
      next: (data: MachineRecord[]) => {
        this.machines = data;
        this.updateVersionOptions();
        this.loading = false;
      },
      error: (err: any) => {
        this.errorMessage = 'Erreur lors du chargement de la liste.';
        console.error(err);
        this.loading = false;
      }
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile = input.files[0];
      this.import();      // lance directement l'import
      input.value = '';   // permet de réimporter le même fichier ensuite
    } else {
      this.selectedFile = null;
    }
  }

  import(): void {
    if (!this.selectedFile) {
      this.errorMessage = 'Veuillez sélectionner un fichier CSV.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.inventoryService.import(this.selectedFile).subscribe({
      next: (data: MachineRecord[]) => {
        this.machines = data;
        this.updateVersionOptions();
        this.loading = false;
      },
      error: (err: any) => {
        this.errorMessage = 'Erreur lors de l\'import du CSV.';
        console.error(err);
        this.loading = false;
      }
    });
  }

  isOffice(m: MachineRecord): boolean {
    return m.lastEnLigne &&
      (m.lastLocalisation === 'Bureau' || m.lastLocalisation === 'Bureau (autre site)');
  }

  onToggleVerified(m: MachineRecord): void {
    this.inventoryService.updateMachine(m.computerName, {
      verifiedByTeam: !!m.verifiedByTeam,
      remark: m.remark ?? null
    }).subscribe({
      next: () => { /* ok */ },
      error: (err: any) => {
        console.error(err);
        this.errorMessage = 'Erreur lors de la mise à jour "vérifié".';
      }
    });
  }

  onRemarkBlur(m: MachineRecord): void {
    this.inventoryService.updateMachine(m.computerName, {
      verifiedByTeam: !!m.verifiedByTeam,
      remark: m.remark ?? null
    }).subscribe({
      next: () => { /* ok */ },
      error: (err: any) => {
        console.error(err);
        this.errorMessage = 'Erreur lors de la mise à jour de la remarque.';
      }
    });
  }

  // ---------- Résumé / stats (basé sur la liste filtrée) ----------

  get filteredMachines(): MachineRecord[] {
    return this.machines.filter(m => {

      // Filtre texte global
      if (this.filterText && this.filterText.trim().length > 0) {
        const t = this.filterText.toLowerCase();
        const haystack = [
          m.computerName,
          m.description ?? '',
          m.lastIPAddress ?? '',
          m.globalProtectVersion ?? '',
          m.lastLocalisation ?? ''
        ].join(' ').toLowerCase();

        if (!haystack.includes(t)) {
          return false;
        }
      }

      // Filtre localisation
      if (this.filterLocalisation !== 'all') {
        const loc = (m.lastLocalisation ?? '').toLowerCase();

        if (this.filterLocalisation === 'bureau') {
          if (!(loc === 'bureau' || loc === 'bureau (autre site)')) return false;
        } else if (this.filterLocalisation === 'tt') {
          if (!loc.startsWith('télétravail')) return false;
        } else if (this.filterLocalisation === 'injoignable') {
          if (loc !== 'injoignable') return false;
        } else if (this.filterLocalisation === 'autre') {
          if (
            loc === 'bureau' ||
            loc === 'bureau (autre site)' ||
            loc.startsWith('télétravail') ||
            loc === 'injoignable'
          ) return false;
        }
      }

      // Filtre version
      const version = (m.globalProtectVersion ?? '').trim();
      const hasVersion = version.length > 0;

      if (this.filterVersion === 'none') {
        if (hasVersion) return false;
      } else if (this.filterVersion !== 'all') {
        if (version !== this.filterVersion) return false;
      }

      // Filtre vérifié / non vérifié
      const verified = !!m.verifiedByTeam;
      if (this.filterVerified === 'yes' && !verified) return false;
      if (this.filterVerified === 'no' && verified) return false;

      return true;
    });
  }

  get totalFiltered(): number {
    return this.filteredMachines.length;
  }

  get filteredWithGP(): number {
    return this.filteredMachines.filter(m => (m.globalProtectVersion ?? '').trim().length > 0).length;
  }

  get filteredWithoutGP(): number {
    return this.filteredMachines.filter(m => (m.globalProtectVersion ?? '').trim().length === 0).length;
  }

  get filteredOffice(): number {
    return this.filteredMachines.filter(m =>
      m.lastLocalisation === 'Bureau' || m.lastLocalisation === 'Bureau (autre site)'
    ).length;
  }

  get filteredTT(): number {
    return this.filteredMachines.filter(m =>
      (m.lastLocalisation ?? '').toLowerCase().startsWith('télétravail')
    ).length;
  }

  get filteredInjoignable(): number {
    return this.filteredMachines.filter(m =>
      (m.lastLocalisation ?? '').toLowerCase() === 'injoignable'
    ).length;
  }

  get latestVersion(): string | null {
    return this.versionOptions.length > 0 ? this.versionOptions[0] : null;
  }

  get filteredUpToDateCount(): number {
    const latest = this.latestVersion;
    if (!latest) return 0;

    return this.filteredMachines.filter(m =>
      (m.globalProtectVersion ?? '').trim() === latest
    ).length;
  }

  get filteredUpToDatePercent(): number {
    if (this.totalFiltered === 0) return 0;
    return Math.round(this.filteredUpToDateCount * 1000 / this.totalFiltered) / 10; // 1 décimale
  }

  // ---------- Options de versions disponibles (pour le filtre) ----------

  private updateVersionOptions(): void {
    const set = new Set<string>();

    for (const m of this.machines) {
      const v = (m.globalProtectVersion ?? '').trim();
      if (v) {
        set.add(v);
      }
    }

    const arr = Array.from(set);
    this.versionOptions = this.sortVersionsDescending(arr);

    if (
      this.filterVersion !== 'all' &&
      this.filterVersion !== 'none' &&
      !this.versionOptions.includes(this.filterVersion)
    ) {
      this.filterVersion = 'all';
    }
  }

  private sortVersionsDescending(versions: string[]): string[] {
    return versions.sort((a, b) => {
      const pa = a.split('.').map(x => parseInt(x, 10) || 0);
      const pb = b.split('.').map(x => parseInt(x, 10) || 0);
      const len = Math.max(pa.length, pb.length);

      for (let i = 0; i < len; i++) {
        const va = pa[i] ?? 0;
        const vb = pb[i] ?? 0;
        if (va < vb) return 1;
        if (va > vb) return -1;
      }
      return 0;
    });
  }

  // Appelé à chaque changement de filtre
  onFiltersChanged(): void {
    // plus de graphique à mettre à jour, la liste et le résumé utilisent directement les getters
  }

  // ---------- Export CSV (Excel) de la liste filtrée ----------

  exportCsv(): void {
    const rows = this.filteredMachines;

    if (!rows || rows.length === 0) {
      this.errorMessage = 'Aucune donnée à exporter.';
      return;
    }

    const headers = [
      'ComputerName',
      'Description',
      'DNSHostName',
      'OperatingSystem',
      'IPAddress',
      'Localisation',
      'GlobalProtectVersion',
      'GlobalProtectStatus',
      'EnLigne',
      'LastScanDateUtc',
      'VerifiedByTeam',
      'Remark'
    ];

    const sep = ';';

    const lines: string[] = [];
    lines.push(headers.join(sep));

    for (const m of rows) {
      const line = [
        m.computerName ?? '',
        m.description ?? '',
        m.dnsHostName ?? '',
        m.operatingSystem ?? '',
        m.lastIPAddress ?? '',
        m.lastLocalisation ?? '',
        m.globalProtectVersion ?? '',
        m.globalProtectStatus ?? '',
        m.lastEnLigne ? 'True' : 'False',
        m.lastScanDateUtc ?? '',
        m.verifiedByTeam ? 'True' : 'False',
        (m.remark ?? '').replace(/[\r\n]/g, ' ')
      ]
        .map(v => `"${(v ?? '').replace(/"/g, '""')}"`)
        .join(sep);

      lines.push(line);
    }

    const csvContent = '\uFEFF' + lines.join('\r\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });

    const now = new Date();
    const stamp = now.toISOString().replace(/[:\-T]/g, '').slice(0, 12);
    const fileName = `Inventaire_GlobalProtect_${stamp}.csv`;

    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.setAttribute('download', fileName);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  // ---------- Ouverture d’un chat Teams sur le nom ----------

  openTeamsChat(m: MachineRecord): void {
    // Adapter ces propriétés à ton modèle réel (email / UPN dans le CSV)
    const anyMachine = m as any;
    const email =
      anyMachine.userPrincipalName ||
      anyMachine.email ||
      null;

    if (!email) {
      console.warn('Aucun email/UPN pour cet enregistrement, impossible d’ouvrir Teams.', m);
      return;
    }

    const url = `https://teams.microsoft.com/l/chat/0/0?users=${encodeURIComponent(email)}`;
    window.open(url, '_blank');
  }
}
