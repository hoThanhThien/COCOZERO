export type DataOriginTag = "Demo" | "Synthetic" | "UserDeclared" | "AiExtracted" | "Verified";

export type CoconutVariety = "TallDuaTa" | "ShortXiemXanh" | "OtherUnsupported";

export interface UserRoleDto {
  roleCode: string;
  approvalStatus: string;
  scopeType: string;
  scopeId?: string | null;
}

export interface UserProfileDto {
  id: string;
  firebaseUid: string;
  email: string;
  displayName: string;
  phone?: string | null;
  status: string;
  roles: UserRoleDto[];
  consents: {
    consentType: string;
    granted: boolean;
    policyVersion: string;
    recordedAt: string;
  }[];
}

export interface FarmDto {
  id: string;
  ownerUserId: string;
  ownerName?: string;
  cooperativeId?: string | null;
  cooperativeName?: string | null;
  name: string;
  province: string;
  commune: string;
  historicalRegion: string;
  areaHa: number;
  treeCount: number;
  variety: CoconutVariety;
  ageYears: number;
  latitude?: number | null;
  longitude?: number | null;
  status: string;
  dataOriginTag: DataOriginTag;
  hasLocationConsent: boolean;
  hasPhotoConsent: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ResearchFactorDto {
  id: string;
  code: string;
  variety: CoconutVariety;
  varietyDisplayName: string;
  treeAgeYears: number;
  valueTCO2PerHa: number;
  unit: string;
  version: string;
  validFrom: string;
  approvedBy: string;
  applicabilityNotes: string;
  source?: {
    code: string;
    title: string;
    authors: string;
    year: number;
    publication: string;
    limitations: string;
  } | null;
}

export interface CarbonEstimateResponse {
  calculationId: string;
  farmId?: string | null;
  resultLabel: string;
  estimatedCO2EquivalentT: number;
  unit: string;
  factorUsed: {
    id: string;
    code: string;
    varietyDisplayName: string;
    treeAgeYears: number;
    valueTCO2PerHa: number;
    version: string;
  };
  observedTreeDensityPerHa: number;
  isDensityWithinBenTre2024SurveyRange: boolean;
  densityContextNote: string;
  optionalArithmeticAllocationKgPerTree?: number | null;
  arithmeticAllocationWarning?: string | null;
  formulaVersion: string;
  sourceCitation: string;
  limitations: string;
  disclaimer: string;
  createdAt: string;
}

export interface AiAnalysisDetailDto {
  analysisId: string;
  fileId?: string | null;
  fileName?: string | null;
  farmId?: string | null;
  projectId?: string | null;
  provider: string;
  modelName: string;
  promptVersion: string;
  status: string;
  isFallback?: boolean;
  isFallbackResult?: boolean;
  provenance?: string;
  verificationNotice?: string;
  extractedFields?: { field: string; value?: string | null; confidence: number; provenance: string }[];
  findings?: { code: string; severity: string; category: string; message: string; evidenceRef?: string | null }[];
  missingFields?: string[];
  duplicates?: { duplicateType: string; matchedId?: string | null; reason: string }[];
  structuredOutput?: {
    extractedFields: { field: string; value?: string | null; confidence: number; provenance: string }[];
    findings: { code: string; severity: string; category: string; message: string; evidenceRef?: string | null }[];
    missingFields: string[];
    duplicates: { duplicateType: string; matchedId?: string | null; reason: string }[];
    confidenceNotes: string[];
    fraudRiskScore?: number;
    fraudRiskLevel?: string;
    fraudSignals?: { code: string; severity: string; evidenceSnippet: string; explanation: string }[];
  };
  fraudRiskScore?: number;
  fraudRiskLevel?: string;
  fraudSignals?: { code: string; severity: string; evidenceSnippet: string; explanation: string }[];
  evidence: {
    fileId: string;
    pageNumber?: number | null;
    snippet: string;
    extractedValue?: string | null;
    confidence: number;
    reason: string;
  }[];
  readinessRubric?: {
    label: string;
    score: number;
    disclaimer: string;
    items: { criterion: string; status: string; notes: string }[];
  } | null;
  reviewerRequired: boolean;
  reviewStatus: string;
  reviewerDecisions?: {
    id: string;
    reviewerUserId: string;
    decision: string;
    comment: string;
    editedOutputJson?: string | null;
    createdAt: string;
  }[];
  createdAt: string;
}

export interface ProjectDto {
  id: string;
  code: string;
  name: string;
  projectType: string;
  province: string;
  historicalRegion: string;
  declaredTotalAreaHa: number;
  intendedMethodology: string;
  readinessScore: number;
  readinessLabel: string;
  verificationStatus: string;
  projectStatus: string;
  dataOriginTag: DataOriginTag;
  disclaimer: string;
  cooperativeName?: string | null;
  listingsCount: number;
  updatedAt: string;
}

export interface RfqDto {
  id: string;
  projectId: string;
  projectName: string;
  buyerUserId: string;
  sellerUserId: string;
  buyerOrganization: string;
  inquiryScope: string;
  message: string;
  status: string;
  sellerResponse?: string | null;
  createdAt: string;
}
