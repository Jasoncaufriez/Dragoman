export interface AdUserStatus {
  samAccountName: string;
  displayName: string;
  ou: string;

  passwordLastSet?: string | null;
  passwordExpiresOn?: string | null;
  passwordStatus: string;

  lastLogonDate?: string | null;
  inactivityStatus: string;

  // PROPRIÉTÉS PERSISTÉES ET CALCULÉES
  isNormal: boolean;
  daysUntilExpiration: number | null;

  // commentaire interne
  comment?: string | null;
}
