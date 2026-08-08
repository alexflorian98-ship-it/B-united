import DOMPurify from "dompurify";

/** Rich-text content is admin-authored, but still rendered via `dangerouslySetInnerHTML` for
 * every viewer — sanitize on render (not just on save) so a compromised/mistaken admin account
 * can't become a stored-XSS vector against every client. */
export function sanitizeRichTextHtml(html: string): string {
  return DOMPurify.sanitize(html, { USE_PROFILES: { html: true } });
}
