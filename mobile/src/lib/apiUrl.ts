const privateHost = /^(localhost|127\.0\.0\.1|0\.0\.0\.0|10\.\d+\.\d+\.\d+|192\.168\.\d+\.\d+|172\.(1[6-9]|2\d|3[0-1])\.\d+\.\d+|10\.0\.2\.2)$/i;

export function isPublicHttpsApiUrl(value: string | undefined): boolean {
  if (!value || !value.startsWith("https://")) return false;
  try {
    const host = new URL(value).hostname;
    return !privateHost.test(host) && !host.endsWith(".local");
  } catch {
    return false;
  }
}
