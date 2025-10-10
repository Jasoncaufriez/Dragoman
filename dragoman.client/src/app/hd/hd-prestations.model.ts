// ====== Modèles HD ======
export interface HDTicket {
  date: string;      // YYYY-MM-DD
  heure?: string;    // HH:mm
  numero?: string;   // numéro ticket
  type?: string;     // type de souci
  dureeMin?: number; // minutes
}

export interface HDAutreTache {
  denomination: string;
  date: string;      // YYYY-MM-DD
  dureeMin?: number; // minutes
}

export interface HDPrestationJour {
  hdUser: string;
  hdDate: string;            // jour
  hdSemaineISO: string;      // ex: 2025-W41
  hdRegimeTravail?: string;  // mi-temps / temps plein
  hdGarde?: string;          // ex "oui — soir 18–22h"
  hdTickets: HDTicket[];
  hdAutresTaches: HDAutreTache[];
  hdRemarquesCollaborateur?: string;
}

export interface HDPrestationSemaine {
  hdUser: string;
  hdSemaineISO: string;
  jours: HDPrestationJour[];
}

export interface HDSaveResultat {
  ok: boolean;
  chemin?: string;   // dossier/zip généré côté serveur
  message?: string;
}
