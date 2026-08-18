import { config } from "zod";

// Must run before any Zod schema is constructed anywhere in the app (form validators, DTO
// parsers, etc.) — those constructions happen as a side effect of importing the modules that
// define them, so this needs to be the very first import in main.tsx, ahead of "./App". See
// main.tsx for the full rationale (CSP script-src has no 'unsafe-eval'; Zod's eval-availability
// probe is otherwise reported as a securitypolicyviolation even though it's self-caught).
config({ jitless: true });
