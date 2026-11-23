"use client";

import { initializeApp, getApps, type FirebaseApp } from "firebase/app";
import { getAuth, GoogleAuthProvider, signInWithPopup, signOut, type Auth } from "firebase/auth";

const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY || "",
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN || "",
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID || "",
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID || "",
};

export function isFirebaseWebConfigured(): boolean {
  return Boolean(
    firebaseConfig.apiKey &&
      !firebaseConfig.apiKey.startsWith("CHANGE_ME") &&
      firebaseConfig.projectId &&
      !firebaseConfig.projectId.startsWith("CHANGE_ME")
  );
}

function getFirebaseAuth(): Auth {
  const app: FirebaseApp = getApps().length > 0 ? getApps()[0] : initializeApp(firebaseConfig);
  return getAuth(app);
}

export async function signInWithGoogleFirebase(): Promise<{ idToken: string; email: string; displayName: string }> {
  const auth = getFirebaseAuth();
  const provider = new GoogleAuthProvider();
  const result = await signInWithPopup(auth, provider);
  const idToken = await result.user.getIdToken();
  return {
    idToken,
    email: result.user.email ?? "",
    displayName: result.user.displayName ?? result.user.email ?? "Google User",
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
