"use client";

import { useState } from "react";
import { LockKeyhole } from "lucide-react";

type Mode = "demo" | "paid";
type WizardData = Record<string, unknown>;

const GATEWAYS = [
  "Raast RTP", "1Link", "JazzCash Merchant", "EasyPaisa",
  "HBL Pay", "MCB Corporate", "UBL Omni", "Stripe",
  "Network International", "Telr", "GoCardless", "Plaid ACH",
  "Billplz FPX", "DuitNow QR", "SWIFT MT103"
];

export default function Step2_5PaymentGateways({
  licenseMode = "demo",
  value = {},
  onNext,
  onUpdate
}: {
  licenseMode?: Mode;
  value?: WizardData;
  onNext?: () => void;
  onUpdate?: (patch: WizardData) => void;
}) {
  const enabled = (value.enabledGateways as string[] | undefined) ?? [];
  const configured = (value.configuredGateways as string[] | undefined) ?? [];

  const toggleGateway = (name: string) => {
    if (licenseMode === "demo") return;
    const nextEnabled = enabled.includes(name)
      ? enabled.filter(item => item !== name)
      : [...enabled, name];
    onUpdate?.({ enabledGateways: nextEnabled });
  };

  const handleSkip = () => {
    onUpdate?.({ skippedInDemo: true });
    onNext?.();
  };

  return (
    <div className="w-full max-w-4xl mx-auto p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Payment Gateways</h1>
        <p className="text-slate-600 text-sm mt-1">
          Connect your institution&apos;s payment accounts. Each credential belongs to your university — AegisCore never pools or shares payment accounts.
        </p>
      </div>

      {licenseMode === "demo" ? (
        <div className="bg-amber-50 border border-amber-300 rounded-xl p-4 text-amber-800 text-sm space-y-3">
          <div className="flex items-center gap-2 font-bold">
            <span>🧪 SANDBOX MODE ACTIVE</span>
          </div>
          <p>
            Payment gateway credentials are not required in Demo. All payment operations will be simulated automatically. Configure real credentials after upgrading to Licensed.
          </p>
          <button
            type="button"
            onClick={handleSkip}
            className="px-4 py-2 bg-amber-200 hover:bg-amber-300 text-amber-900 font-semibold text-xs rounded-lg transition"
          >
            Skip — Configure After Upgrade
          </button>
        </div>
      ) : (
        <p className="text-sm font-medium text-slate-700">
          At least one payment gateway must be configured to proceed.
        </p>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {GATEWAYS.map(name => {
          const isConfigured = configured.includes(name);
          const isEnabled = enabled.includes(name);
          const isDemo = licenseMode === "demo";

          return (
            <div
              key={name}
              className={`p-4 border rounded-xl bg-white shadow-sm flex items-center justify-between transition ${
                isDemo ? "opacity-60 bg-slate-50" : ""
              }`}
            >
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  {isDemo && <LockKeyhole className="w-4 h-4 text-slate-400" />}
                  <span className="font-semibold text-slate-800">{name}</span>
                </div>
                <div>
                  <span
                    className={`inline-block text-[10px] font-bold px-2 py-0.5 rounded ${
                      isConfigured
                        ? "bg-emerald-100 text-emerald-800"
                        : "bg-slate-100 text-slate-600"
                    }`}
                  >
                    {isConfigured ? "CONFIGURED ✓" : "NOT CONFIGURED"}
                  </span>
                </div>
              </div>

              <div className="flex items-center gap-3">
                <button
                  type="button"
                  disabled={isDemo}
                  onClick={() => toggleGateway(name)}
                  className={`w-12 h-6 flex items-center rounded-full p-1 transition ${
                    isEnabled ? "bg-indigo-600 justify-end" : "bg-slate-300 justify-start"
                  } ${isDemo ? "cursor-not-allowed opacity-50" : "cursor-pointer"}`}
                >
                  <span className="bg-white w-4 h-4 rounded-full shadow-md" />
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
