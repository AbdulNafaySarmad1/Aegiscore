import { AppShell } from "@/components/layout/AppShell";

interface RouteLayoutProps {
  children: React.ReactNode;
}

export default function RouteLayout({ children }: RouteLayoutProps) {
  return <AppShell>{children}</AppShell>;
}
