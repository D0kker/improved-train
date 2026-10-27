"use client";

import { useCallback, useEffect, useRef } from "react";
import { RequestLifetime } from "./request-lifetime";

export function useRequestLifetime() {
  const lifetime = useRef<RequestLifetime | null>(null);
  if (lifetime.current === null) lifetime.current = new RequestLifetime();

  useEffect(() => () => lifetime.current?.cancel(), []);

  return useCallback(
    (parentSignal?: AbortSignal) => lifetime.current!.begin(parentSignal),
    [],
  );
}
