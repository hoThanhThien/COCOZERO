import { LoginClient } from "./LoginClient";

export const dynamic = "force-dynamic";

export default function LoginPage() {
  const runtimeFirebaseConfig = {
    apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY || "",
    authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN || "",
    projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID || "",
    appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID || "",
  };
  const showDemoMode = process.env.NEXT_PUBLIC_SHOW_DEMO_MODE !== "false";

  return (
    <LoginClient
      runtimeFirebaseConfig={runtimeFirebaseConfig}
      showDemoMode={showDemoMode}
    />
  );
}
