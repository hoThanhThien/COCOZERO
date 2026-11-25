"use client";

import { initializeApp, getApps, type FirebaseApp } from "firebase/app";
import {
  getAuth,
  GoogleAuthProvider,
  signInWithEmailAndPassword,
  signInWithPopup,
  signOut,
  type Auth,
} from "firebase/auth";

export interface FirebasePublicConfig {
  apiKey: string;
  authDomain: string;
  projectId: string;
  appId: string;
}

const buildTimeConfig: FirebasePublicConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY || "",
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN || "",
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID || "",
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID || "",
};

let runtimeOverride: Partial<FirebasePublicConfig> = {};

export function setFirebaseRuntimeConfig(cfg?: Partial<FirebasePublicConfig>): void {
  if (!cfg) return;
  runtimeOverride = { ...runtimeOverride, ...cfg };
}

function getEffectiveConfig(override?: Partial<FirebasePublicConfig>): FirebasePublicConfig {
  return {
    apiKey: override?.apiKey || runtimeOverride.apiKey || buildTimeConfig.apiKey || "",
    authDomain: override?.authDomain || runtimeOverride.authDomain || buildTimeConfig.authDomain || "",
    projectId: override?.projectId || runtimeOverride.projectId || buildTimeConfig.projectId || "",
    appId: override?.appId || runtimeOverride.appId || buildTimeConfig.appId || "",
  };
}

export function isFirebaseWebConfigured(override?: Partial<FirebasePublicConfig>): boolean {
  const cfg = getEffectiveConfig(override);
  return Boolean(
    cfg.apiKey &&
      !cfg.apiKey.startsWith("CHANGE_ME") &&
      cfg.projectId &&
      !cfg.projectId.startsWith("CHANGE_ME")
  );
}

function getFirebaseAuth(override?: Partial<FirebasePublicConfig>): Auth {
  const cfg = getEffectiveConfig(override);
  const app: FirebaseApp = getApps().length > 0 ? getApps()[0] : initializeApp(cfg);
  return getAuth(app);
}

export async function signInWithGoogleFirebase(
  override?: Partial<FirebasePublicConfig>
): Promise<{ idToken: string; email: string; displayName: string }> {
  const auth = getFirebaseAuth(override);
  const provider = new GoogleAuthProvider();
  const result = await signInWithPopup(auth, provider);
  const idToken = await result.user.getIdToken();
  return {
    idToken,
    email: result.user.email ?? "",
    displayName: result.user.displayName ?? result.user.email ?? "Google User",
  };
}

export async function signInWithEmailPasswordFirebase(
  email: string,
  password: string,
  override?: Partial<FirebasePublicConfig>
): Promise<{ idToken: string; email: string; displayName: string }> {
  const auth = getFirebaseAuth(override);
  const result = await signInWithEmailAndPassword(auth, email.trim(), password);
  const idToken = await result.user.getIdToken();
  return {
    idToken,
    email: result.user.email ?? email.trim(),
    displayName: result.user.displayName ?? result.user.email ?? email.trim(),
  };
}

export async function signOutFirebaseIfActive(): Promise<void> {
  if (typeof window !== "undefined") {
    localStorage.removeItem("cococarbon_token");
    localStorage.removeItem("cococarbon_auth_mode");
  }
  if (isFirebaseWebConfigured()) {
    try {
      const auth = getFirebaseAuth();
      await signOut(auth);
    } catch {
      // ignore if firebase was not initialized
    }
  }
}
