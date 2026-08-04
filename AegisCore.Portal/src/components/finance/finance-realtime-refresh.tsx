"use client";

import { HubConnectionBuilder, HttpTransportType } from "@microsoft/signalr";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

const refreshEvents = new Set(["voucher-updated", "voucher-posted", "payment-settled"]);

export function FinanceRealtimeRefresh() {
  const router = useRouter();

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl("/hubs/finance", { transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling, withCredentials: true })
      .withAutomaticReconnect()
      .build();
    connection.on("FinanceEvent", (event: { eventName?: string }) => {
      if (event.eventName && refreshEvents.has(event.eventName)) router.refresh();
    });
    void connection.start().catch(() => undefined);
    return () => { void connection.stop(); };
  }, [router]);

  return null;
}
