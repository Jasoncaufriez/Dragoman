export interface MachineRecord {
  computerName: string;
  description?: string;
  dnsHostName?: string;
  operatingSystem?: string;

  lastIPAddress?: string;
  lastLocalisation?: string;

  globalProtectVersion?: string;
  globalProtectStatus?: string;

  lastEnLigne: boolean;
  lastScanDateUtc: string;

  verifiedByTeam?: boolean;
  remark?: string | null;
}
