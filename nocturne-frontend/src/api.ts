import * as signalR from "@microsoft/signalr";
import { AppDispatch, receiveNotification } from "./store";

const baseUrl = import.meta.env.VITE_API_URL ?? "";

export async function apiGet<T>(path: string): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, { credentials: "include" });
  if (!response.ok) throw new Error(`API ${response.status}`);
  return response.json() as Promise<T>;
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  if (!response.ok) throw new Error(`API ${response.status}`);
  return response.json() as Promise<T>;
}

export function connectNotifications(dispatch: AppDispatch) {
  const connection = new signalR.HubConnectionBuilder().withUrl(`${baseUrl}/hubs/notifications`).withAutomaticReconnect().build();
  connection.on("ReceiveNotification", () => dispatch(receiveNotification()));
  connection.start().catch(() => undefined);
  return connection;
}
