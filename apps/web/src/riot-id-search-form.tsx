"use client";

import { FormEvent, useRef, useState } from "react";

export function RiotIdSearchForm({
  platformRegion,
  onNavigate,
}: {
  platformRegion: string;
  onNavigate: (href: string) => void;
}) {
  const gameNameInput = useRef<HTMLInputElement>(null);
  const tagLineInput = useRef<HTMLInputElement>(null);
  const [gameName, setGameName] = useState("");
  const [tagLine, setTagLine] = useState("");
  const [errors, setErrors] = useState({ gameName: false, tagLine: false });
  const [isNavigating, setIsNavigating] = useState(false);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedGameName = gameName.trim();
    const normalizedTagLine = tagLine.trim();
    const nextErrors = {
      gameName: !normalizedGameName,
      tagLine: !normalizedTagLine,
    };
    setErrors(nextErrors);
    if (nextErrors.gameName || nextErrors.tagLine) {
      if (nextErrors.gameName) gameNameInput.current?.focus();
      else tagLineInput.current?.focus();
      return;
    }

    setIsNavigating(true);
    onNavigate(
      `/player/${encodeURIComponent(normalizedGameName)}/${encodeURIComponent(normalizedTagLine)}?platformRegion=${encodeURIComponent(platformRegion)}`,
    );
  }

  return (
    <form className="search-form" onSubmit={submit} noValidate>
      <div className="riot-id-fields">
        <label htmlFor="game-name">
          <span>Game Name</span>
          <input
            aria-describedby={errors.gameName ? "game-name-error" : "riot-id-help"}
            aria-invalid={errors.gameName}
            autoComplete="off"
            id="game-name"
            maxLength={64}
            name="gameName"
            onChange={(event) => {
              setGameName(event.target.value);
              setErrors((current) => ({ ...current, gameName: false }));
            }}
            placeholder="Eagly"
            ref={gameNameInput}
            value={gameName}
          />
        </label>
        <span className="hash" aria-hidden="true">#</span>
        <label className="tag-field" htmlFor="tag-line">
          <span>Tag</span>
          <input
            aria-describedby={errors.tagLine ? "tag-line-error" : "riot-id-help"}
            aria-invalid={errors.tagLine}
            autoCapitalize="characters"
            autoComplete="off"
            id="tag-line"
            maxLength={16}
            name="tagLine"
            onChange={(event) => {
              setTagLine(event.target.value);
              setErrors((current) => ({ ...current, tagLine: false }));
            }}
            placeholder="LAN"
            ref={tagLineInput}
            value={tagLine}
          />
        </label>
      </div>
      <p className="field-help" id="riot-id-help">Escribe tu Game Name y tu Tag de Riot ID.</p>
      {errors.gameName ? <p className="field-error" id="game-name-error">Escribe el Game Name.</p> : null}
      {errors.tagLine ? <p className="field-error" id="tag-line-error">Escribe el Tag.</p> : null}
      <button className="primary-button" disabled={isNavigating} type="submit">
        {isNavigating ? "Abriendo perfil…" : "Buscar jugador"}
        <span aria-hidden="true">→</span>
      </button>
      {isNavigating ? <span className="sr-only" role="status">Abriendo el perfil del jugador.</span> : null}
    </form>
  );
}
