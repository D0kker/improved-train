"use client";

import { FormEvent, useRef, useState } from "react";
import { Button } from "@/src/design-system";

export function RiotIdSearchForm({
  platformRegion,
  onNavigate,
  copy = {
    gameNameLabel: "Nombre de jugador",
    tagLineLabel: "Tag",
    riotIdHelp: "Escribe el nombre y el Tag que aparecen en tu Riot ID.",
    gameNameError: "Escribe el nombre de jugador.",
    tagLineError: "Escribe el Tag.",
    submit: "Buscar jugador",
    navigating: "Abriendo perfil…",
    navigatingAnnouncement: "Abriendo el perfil del jugador.",
  },
}: {
  platformRegion: string;
  onNavigate: (href: string) => void;
  copy?: {
    gameNameLabel: string;
    tagLineLabel: string;
    riotIdHelp: string;
    gameNameError: string;
    tagLineError: string;
    submit: string;
    navigating: string;
    navigatingAnnouncement: string;
  };
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
    <form className="search-form" id="player-search-form" onSubmit={submit} noValidate>
      <div className="riot-id-fields">
        <label htmlFor="game-name">
          <span>{copy.gameNameLabel}</span>
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
          <span>{copy.tagLineLabel}</span>
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
      <p className="field-help" id="riot-id-help">{copy.riotIdHelp}</p>
      {errors.gameName ? <p className="field-error" id="game-name-error">{copy.gameNameError}</p> : null}
      {errors.tagLine ? <p className="field-error" id="tag-line-error">{copy.tagLineError}</p> : null}
      <Button className="home-search-submit" loading={isNavigating} type="submit">
        {isNavigating ? copy.navigating : copy.submit}
        {!isNavigating ? <span aria-hidden="true">→</span> : null}
      </Button>
      {isNavigating ? <span className="sr-only" role="status">{copy.navigatingAnnouncement}</span> : null}
    </form>
  );
}
