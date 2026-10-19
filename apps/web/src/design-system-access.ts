export function isDesignSystemGalleryEnabled(
  nodeEnv: string | undefined,
  explicitFlag: string | undefined,
): boolean {
  if (nodeEnv === "development") return true;
  return explicitFlag === "true";
}
