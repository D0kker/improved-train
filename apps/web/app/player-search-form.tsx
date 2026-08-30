"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";

export function PlayerSearchForm() {
  const router = useRouter();
  const [gameName, setGameName] = useState("");
  const [tagLine, setTagLine] = useState("");
  const [error, setError] = useState<string | null>(null);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedGameName = gameName.trim();
    const normalizedTagLine = tagLine.trim();

    if (!normalizedGameName || !normalizedTagLine) {
      setError("Escribe el Game Name y el Tag.");
      return;
    }

    setError(null);
    router.push(
      `/player/${encodeURIComponent(normalizedGameName)}/${encodeURIComponent(normalizedTagLine)}`,
    );
  }

  return (
    <form className="search-form" onSubmit={submit} noValidate>
      <div className="riot-id-fields">
        <label>
          <span>Game Name</span>
          <input
            autoComplete="off"
            maxLength={64}
            name="gameName"
            onChange={(event) => setGameName(event.target.value)}
            placeholder="Eagly"
            value={gameName}
          />
        </label>
        <span className="hash" aria-hidden="true">
          #
        </span>
        <label className="tag-field">
          <span>Tag</span>
          <input
            autoCapitalize="characters"
            autoComplete="off"
            maxLength={16}
            name="tagLine"
            onChange={(event) => setTagLine(event.target.value)}
            placeholder="LAN"
            value={tagLine}
          />
        </label>
      </div>
      {error ? (
        <p className="field-error" role="alert">
          {error}
        </p>
      ) : null}
      <button className="primary-button" type="submit">
        Analizar jugador
        <span aria-hidden="true">→</span>
      </button>
    </form>
  );
}
