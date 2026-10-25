"use client";

import { useRouter } from "next/navigation";
import { RiotIdSearchForm } from "@/src/riot-id-search-form";
import type { homeCopy } from "./home-copy";

export function PlayerSearchForm({
  platformRegion,
  copy,
}: {
  platformRegion: string;
  copy: typeof homeCopy.form;
}) {
  const router = useRouter();
  return (
    <RiotIdSearchForm
      platformRegion={platformRegion}
      copy={copy}
      onNavigate={(href) => router.push(href)}
    />
  );
}
