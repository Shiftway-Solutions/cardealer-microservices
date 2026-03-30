from __future__ import annotations

import json

from playwright.sync_api import sync_playwright


def _clean(value: object) -> str:
    return " ".join(str(value or "").split())


def _describe_js() -> str:
    return r"""() => {
        const clean = (value) => (value || '').toString().replace(/\s+/g, ' ').trim();
        const describe = (el) => {
            if (!el) return null;
            const rect = el.getBoundingClientRect();
            return {
                tag: (el.tagName || '').toLowerCase(),
                className: clean(typeof el.className === 'string' ? el.className : el.className?.baseVal || ''),
                id: clean(el.id),
                role: clean(el.getAttribute?.('role')),
                aria: clean(el.getAttribute?.('aria-label')),
                placeholder: clean(el.getAttribute?.('placeholder')),
                title: clean(el.getAttribute?.('title')),
                contenteditable: clean(el.getAttribute?.('contenteditable')),
                text: clean(el.innerText || el.textContent).slice(0, 200),
                focusWithin: !!el.matches?.(':focus-within'),
                x: rect.x,
                y: rect.y,
                width: rect.width,
                height: rect.height,
            };
        };

        const textarea = document.querySelector('textarea.ime-text-area');
        const bottomCandidates = Array.from(document.querySelectorAll([
            'textarea.ime-text-area',
            '[contenteditable="true"]',
            '.monaco-editor',
            '.interactive-input-part',
            '.interactive-input-box',
            '.chat-input-container',
            '[class*="chat-input"]',
            '[class*="interactive-input"]',
            '[class*="chat"][role="textbox"]',
            '[aria-label*="chat" i]',
            '[placeholder*="build" i]',
            '[placeholder*="ask" i]'
        ].join(',')))
            .filter((el) => {
                const rect = el.getBoundingClientRect();
                const style = window.getComputedStyle(el);
                return rect.width > 0 && rect.height > 0 && rect.y > window.innerHeight - 280 && style.display !== 'none' && style.visibility !== 'hidden';
            })
            .slice(0, 80)
            .map(describe);

        const chain = [];
        let node = textarea;
        for (let i = 0; node && i < 10; i += 1, node = node.parentElement) {
            chain.push(describe(node));
        }

        return {
            documentTitle: document.title,
            activeElement: describe(document.activeElement),
            textarea: describe(textarea),
            textareaIsActive: document.activeElement === textarea,
            textareaClosestMonaco: describe(textarea?.closest('.monaco-editor, .interactive-input-part, .interactive-input-box, .chat-input-container, [class*="interactive-input"], [class*="chat-input"]')),
            textareaAncestors: chain,
            bottomCandidates,
        };
    }"""


def main() -> int:
    with sync_playwright() as playwright:
        browser = playwright.chromium.connect_over_cdp("http://localhost:9222")
        try:
            page = None
            for context in browser.contexts:
                for candidate in context.pages:
                    if "workbench" in candidate.url:
                        page = candidate
                        break
                if page:
                    break

            if page is None:
                fallback_context = browser.contexts[0] if browser.contexts else None
                page = fallback_context.pages[0] if fallback_context and fallback_context.pages else None

            if page is None:
                raise RuntimeError("No VS Code workbench page found over CDP")

            probe = page.evaluate(_describe_js())
            print(json.dumps(probe, indent=2, ensure_ascii=False))
            return 0
        finally:
            browser.close()


if __name__ == "__main__":
    raise SystemExit(main())