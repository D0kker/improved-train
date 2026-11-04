import { cookies, headers } from "next/headers";
import { getRequestConfig } from "next-intl/server";
import { resolveLocale } from "./locales";
import { messages } from "./messages";

export default getRequestConfig(async () => {
  const cookieStore = await cookies();
  const requestHeaders = await headers();
  const preference = cookieStore.get("echo-locale")?.value;
  const locale = resolveLocale(
    preference,
    requestHeaders.get("accept-language"),
  );
  return { locale, messages: messages[locale] };
});
