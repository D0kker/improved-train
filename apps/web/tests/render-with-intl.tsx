import type { ReactElement } from "react";
import { render } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { messages } from "../src/i18n/messages.ts";
import type { Locale } from "../src/i18n/locales.ts";

export function renderWithIntl(element: ReactElement, locale: Locale = "es") {
  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <NextIntlClientProvider locale={locale} messages={messages[locale]}>
      {children}
    </NextIntlClientProvider>
  );
  return render(element, { wrapper: Wrapper });
}
