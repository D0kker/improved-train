"use client";

import { useRouter } from "next/navigation";
import { RiotIdSearchForm } from "@/src/riot-id-search-form";

export function PlayerSearchForm({ platformRegion }: { platformRegion: string }) {
  const router = useRouter();
  return (
    <RiotIdSearchForm
      platformRegion={platformRegion}
      onNavigate={(href) => router.push(href)}
    />
  );
}
