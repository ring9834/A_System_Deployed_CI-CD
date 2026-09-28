import type { Metadata } from "next";
import { Providers } from "./providers";
import "./globals.css";

export const metadata: Metadata = {
  title: "Todo Planner",
  description: "Plan today's jobs — for gardeners working across multiple properties",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <head>
        {/* next/font/google fetches font files at BUILD time, and Turbopack has a real,
            currently-unresolved bug/fragility handling that fetch (see the exact error
            "next/font/google queries have exactly one entry" — widely reported against
            this Next.js version). Loading via <link> instead happens at runtime in the
            browser, removing the build's dependency on reaching Google's servers at all. */}
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link
          rel="stylesheet"
          href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@500;600;700&family=IBM+Plex+Sans:wght@400;500;600&display=swap"
        />
      </head>
      <body className="font-sans antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
