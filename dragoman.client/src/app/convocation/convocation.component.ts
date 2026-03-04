import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { InterpretesService } from '../services/interpretes.service';
import { AudienceDto } from '../dtos/interprete-dto.model';

export interface AudienceDisplayRow extends AudienceDto {
  groupedIds: number[];
}

@Component({
  selector: 'app-convocation',
  templateUrl: './convocation.component.html',
  styleUrls: ['./convocation.component.css']
})
export class ConvocationComponent implements OnInit {
  tolkcode = '';
  private rawValidated: AudienceDto[] = [];
  private rawAvailable: AudienceDto[] = [];
  selectedIds = new Set<number>();
  loading = false;
  error?: string;
  info?: string;

  interpretePrenom = '';
  interpreteEmail = '';
  taalrol: number | null = null; // 1=NL, 2=FR

  showPreview = false;
  previewHtml = '';
  sourceFilter: 'ALL' | 'VRM' | 'ANN' = 'ALL';

  /** Distinct validated rows for display */
  get validatedRows(): AudienceDisplayRow[] {
    return this.toDistinct(this.rawValidated);
  }

  /** Distinct available rows, filtered by source */
  get filteredAvailableRows(): AudienceDisplayRow[] {
    const src = this.sourceFilter === 'ALL'
      ? this.rawAvailable
      : this.rawAvailable.filter(r => r.source === this.sourceFilter);
    return this.toDistinct(src);
  }

  private toDistinct(rows: AudienceDto[]): AudienceDisplayRow[] {
    const map = new Map<string, AudienceDisplayRow>();
    for (const r of rows) {
      const key = [
        r.dateAudience,
        r.heureAudience,
        r.nom,
        r.salleAudience,
        r.langueRequete,
        r.source ?? ''
      ].join('|');
      const existing = map.get(key);
      if (existing) {
        if (!existing.groupedIds.includes(r.idAffAudience)) {
          existing.groupedIds.push(r.idAffAudience);
        }
      } else {
        map.set(key, { ...r, groupedIds: [r.idAffAudience] });
      }
    }
    return Array.from(map.values());
  }

  constructor(
    private route: ActivatedRoute,
    private api: InterpretesService
  ) {}

  ngOnInit(): void {
    this.tolkcode = this.route.snapshot.paramMap.get('tolkcode') ?? '';
    this.load();
  }

  load() {
    if (!this.tolkcode) { this.error = 'Tolkcode manquant'; return; }
    this.loading = true;
    this.error = undefined;
    this.info = undefined;
    this.selectedIds.clear();
    this.showPreview = false;

    this.api.getIdentite(Number(this.tolkcode)).subscribe({
      next: (data: any) => {
        this.interpretePrenom = data.prenom ?? data.Prenom ?? '';
        this.interpreteEmail = data.email ?? data.Email ?? '';
        this.taalrol = data.taalrol ?? data.Taalrol ?? null;
        this.loadConvocations();
      },
      error: () => { this.error = 'Erreur de chargement de l\'interprète.'; this.loading = false; }
    });
  }

  private loadConvocations() {
    this.api.convocations(this.tolkcode).subscribe({
      next: r => {
        this.rawValidated = r;
        this.loadAvailable();
      },
      error: () => { this.error = 'Erreur de chargement des convocations.'; this.loading = false; }
    });
  }

  private loadAvailable() {
    this.api.audiencesExact(this.tolkcode).subscribe({
      next: r => { this.rawAvailable = r; this.loading = false; },
      error: () => { this.error = 'Erreur de chargement des audiences disponibles.'; this.loading = false; }
    });
  }

  toggleGroupSelection(row: AudienceDisplayRow) {
    const allSelected = row.groupedIds.every(id => this.selectedIds.has(id));
    for (const id of row.groupedIds) {
      if (allSelected) {
        this.selectedIds.delete(id);
      } else {
        this.selectedIds.add(id);
      }
    }
  }

  isGroupSelected(row: AudienceDisplayRow): boolean {
    return row.groupedIds.every(id => this.selectedIds.has(id));
  }

  get isNL(): boolean { return this.taalrol === 1; }

  private fmtDate(iso: string): string {
    if (!iso) return '';
    const d = new Date(iso);
    if (isNaN(d.getTime())) return iso;
    return `${d.getDate()}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
  }

  private buildTableHtml(rows: AudienceDto[], headerLabel: string, headerColor: string, statusLabel: string): string {
    if (rows.length === 0) return '';
    const nl = this.isNL;
    const hDate = nl ? 'Datum' : 'Date';
    const hNom = nl ? 'Rechter' : 'Magistrat';
    const hHeure = nl ? 'Uur' : 'Heure';
    const hLangue = nl ? 'Taal' : 'Langue';
    const hStatut = nl ? 'Status' : 'Statut';

    // En‑tête stylée : fond coloré + texte blanc
    let html = `<p style="margin:12px 0 8px;font-weight:bold;background:${headerColor};color:#fff;padding:6px 10px;border-radius:4px;display:inline-block;font-size:14px;">${headerLabel}</p>`;
    html += `<table border="1" cellpadding="6" cellspacing="0" style="border-collapse:collapse;font-family:Calibri,Arial,sans-serif;font-size:12px;min-width:500px;">`;
    html += `<thead><tr style="background:${headerColor};color:#fff;">`;
    html += `<th style="text-align:left;padding:6px 10px;">${hDate}</th>`;
    html += `<th style="text-align:left;padding:6px 10px;">${hNom}</th>`;
    html += `<th style="text-align:left;padding:6px 10px;">${hHeure}</th>`;
    html += `<th style="text-align:left;padding:6px 10px;">${hLangue}</th>`;
    html += `<th style="text-align:left;padding:6px 10px;">${hStatut}</th>`;
    html += `</tr></thead><tbody>`;
    for (let i = 0; i < rows.length; i++) {
      const r = rows[i];
      const bg = i % 2 === 0 ? '#ffffff' : '#f4f7fb';
      html += `<tr style="background:${bg};">`;
      html += `<td style="padding:5px 10px;">${this.fmtDate(r.dateAudience)}</td>`;
      html += `<td style="padding:5px 10px;">${r.nom}</td>`;
      html += `<td style="padding:5px 10px;">${r.heureAudience}</td>`;
      html += `<td style="padding:5px 10px;">${r.langueRequete}</td>`;
      html += `<td style="padding:5px 10px;font-weight:bold;">${statusLabel}</td>`;
      html += `</tr>`;
    }
    html += `</tbody></table>`;
    return html;
  }

  private buildMailHtml(): string {
    const nl = this.isNL;
    const prenom = this.interpretePrenom || (nl ? 'Beste' : 'Madame/Monsieur');
    const allAvailDistinct = this.toDistinct(this.rawAvailable);
    const selectedRows = allAvailDistinct.filter(a => a.groupedIds.some(id => this.selectedIds.has(id)));

    const greeting = nl ? `Beste ,` : `Bonjour ,`;
    const intro = nl
      ? 'Hieronder vindt u een overzicht van de reeds bevestigde alsook nieuwe datums.<br>Gelieve uw beschikbaarheid te bevestigen aub.'
      : 'Vous trouverez ci-dessous un aperçu des dates déjà confirmées ainsi que les nouvelles dates.<br>Veuillez confirmer votre disponibilité svp.';

    const confirmedLabel = nl ? 'Bevestigd' : 'Confirmé';
    const newLabel = nl ? 'Te bevestigen' : 'À confirmer';
    const confirmedHeader = nl ? '✅ Bevestigde zittingen' : '✅ Audiences confirmées';
    const newHeader = nl ? '📋 Nieuwe zittingen — te plannen' : '📋 Nouvelles audiences — à planifier';

    // confirmed header color changed to red #9E3039, new header remains green (#059669)
    const tableConfirmed = this.buildTableHtml(this.validatedRows, confirmedHeader, '#9E3039', confirmedLabel);
    const tableNew = this.buildTableHtml(selectedRows, newHeader, '#059669', newLabel);

    const footer = nl
      ? 'We willen benadrukken dat het absoluut noodzakelijk is om 15 minuten voor het aangegeven tijdstip aanwezig te zijn om het goede verloop van de zittingen te garanderen.<br><br>Alvast bedankt<br><br>Met vriendelijke groeten,'
      : 'Nous tenons à souligner qu\'il est absolument nécessaire d\'être présent 15 minutes avant l\'heure indiquée afin de garantir le bon déroulement des audiences.<br><br>Merci d\'avance<br><br>Cordialement,';

    return `<div style="font-family:Calibri,Arial,sans-serif;font-size:13px;color:#1f2937;">`
      + `<p>${greeting}</p>`
      + `<p>${intro}</p>`
      + `${tableConfirmed}`
      + `${tableNew}`
      + `<br><p>${footer}</p>`
      + `</div>`;
  }

  generatePreview() {
    if (!this.interpreteEmail) {
      this.error = this.isNL
        ? 'Geen e-mailadres gevonden voor deze tolk.'
        : 'Aucune adresse e-mail trouvée pour cet interprète.';
      return;
    }
    this.previewHtml = this.buildMailHtml();
    this.showPreview = true;
  }

  async copyAndOpenMail() {
    const html = this.buildMailHtml();
    let copied = false;

    // 1) Tenter l'API Clipboard moderne (HTTPS / localhost uniquement)
    if (window.isSecureContext && navigator.clipboard && typeof ClipboardItem !== 'undefined') {
      try {
        const blob = new Blob([html], { type: 'text/html' });
        await navigator.clipboard.write([
          new ClipboardItem({ 'text/html': blob })
        ]);
        copied = true;
      } catch { /* fallback ci-dessous */ }
    }

    // 2) Fallback : copie via execCommand (fonctionne en HTTP)
    if (!copied) {
      try {
        const tempDiv = document.createElement('div');
        tempDiv.contentEditable = 'true';
        tempDiv.innerHTML = html;
        tempDiv.style.position = 'fixed';
        tempDiv.style.left = '-9999px';
        tempDiv.style.opacity = '0';
        document.body.appendChild(tempDiv);

        const range = document.createRange();
        range.selectNodeContents(tempDiv);
        const sel = window.getSelection();
        sel?.removeAllRanges();
        sel?.addRange(range);

        copied = document.execCommand('copy');
        sel?.removeAllRanges();
        document.body.removeChild(tempDiv);
      } catch { /* échec total */ }
    }

    this.info = copied
      ? (this.isNL
          ? 'E-mail inhoud gekopieerd! Plak (Ctrl+V) in het Outlook-venster.'
          : 'Contenu copié ! Collez (Ctrl+V) dans la fenêtre Outlook.')
      : (this.isNL
          ? 'Kopiëren mislukt — selecteer en kopieer handmatig uit de preview.'
          : 'Copie échouée — sélectionnez et copiez manuellement depuis l\'aperçu.');

    const nl = this.isNL;
    const subject = encodeURIComponent(nl
      ? `RVV-CCE Convocatie tolk #${this.tolkcode}`
      : `RVV-CCE Convocation interprète #${this.tolkcode}`);

    window.location.href = `mailto:${this.interpreteEmail}?subject=${subject}`;
  }
}
