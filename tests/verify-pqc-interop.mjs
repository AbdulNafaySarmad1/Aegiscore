import { readFile } from "node:fs/promises";
import { ml_dsa65 } from "../AegisCore.Portal/node_modules/@noble/post-quantum/ml-dsa.js";

if (process.argv.length !== 3) {
  throw new Error("Usage: node tests/verify-pqc-interop.mjs <fixture.json>");
}

const fixture = JSON.parse(await readFile(process.argv[2], "utf8"));
const bytes = (value) => Uint8Array.from(Buffer.from(value, "base64"));
if (fixture.Algorithm !== "ML-DSA-65" || !ml_dsa65.verify(bytes(fixture.Signature), bytes(fixture.Payload), bytes(fixture.PublicKey))) {
  throw new Error("Bouncy Castle ML-DSA-65 proof failed Noble verification.");
}
console.log("ML-DSA-65 backend/frontend interoperability verified.");
