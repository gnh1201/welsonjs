// webchat.js
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
// The web chat services (Copilot, ChatGPT, ...) that the launcher drives through the
// browser's DevTools protocol (WebChatRunner, behind POST /v1/chat/completions): it opens
// the service's page in the launcher's browser when none is open (sign in there if
// asked), puts the message into the message box, sends it, and reads the answer from the
// page.
//
// Each service lists the only page-specific parts (selectors and scripts evaluated in
// the chat page). The web UIs may change; update the service entry when they do.
//
// The launcher evaluates this file in its own JavaScript engine (ChakraCore) and calls
// describe() and turn(); the editor loads it for the list of services (services).
//
// ChakraCore has no DOM (no document, window, location, ...). This file only builds the
// page scripts as strings, which the launcher then runs in the chat page through the
// DevTools protocol (Runtime.evaluate). So:
// - DOM code goes only inside the template strings (backticks) of the page scripts.
// - Outside them, including inside ${...}, use plain JavaScript only (for example
//   JSON.stringify to embed a selector or the text). A service entry such as
//   url: location.origin, or a builder that calls document.querySelector directly,
//   fails in the launcher ("location is not defined").
// - The launcher embeds this file: rebuild the launcher after changing it.
var WelsonJSWebChat = (function () {
    // Evaluated in the chat page: replaces the content of a Lexical message box
    // with the text, one paragraph per line, through the editor instance that
    // Lexical attaches to its root element. Returns false when that is not
    // possible (not a Lexical editor, or the state was rejected).
    const buildSetLexicalTextScript = (inputSelector, text) => `(() => {
        const el = document.querySelector(${JSON.stringify(inputSelector)});
        const editor = el && el.__lexicalEditor;
        if (!editor || typeof editor.parseEditorState !== 'function') return false;
        const paragraph = line => ({
            type: 'paragraph', version: 1, direction: line ? 'ltr' : null, format: '', indent: 0, textFormat: 0, textStyle: '',
            children: line ? [{ type: 'text', version: 1, text: line, detail: 0, format: 0, mode: 'normal', style: '' }] : []
        });
        const lines = ${JSON.stringify(text)}.split('\\n');
        try {
            editor.setEditorState(editor.parseEditorState(JSON.stringify({
                root: { type: 'root', version: 1, direction: 'ltr', format: '', indent: 0, children: lines.map(paragraph) }
            })));
        } catch (e) {
            return false;
        }
        el.focus();
        return true;
    })()`;

    // Evaluated in the chat page: pastes the text as plain text into an (empty)
    // contenteditable message box, the way a clipboard paste does. Editors such
    // as ProseMirror turn the lines into paragraphs and do not apply typing
    // shortcuts (Markdown input rules) to pasted text; ProseMirror's default
    // parser drops empty lines. Returns false when the box is not a
    // contenteditable.
    const buildPasteTextScript = (inputSelector, text) => `(() => {
        const el = document.querySelector(${JSON.stringify(inputSelector)});
        if (!el || !el.isContentEditable) return false;
        el.focus();
        const data = new DataTransfer();
        data.setData('text/plain', ${JSON.stringify(text)});
        el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
        return true;
    })()`;

    // Evaluated in the chat page: replaces the content of a Tiptap message box
    // with the text, one paragraph per line (empty lines kept), through the
    // editor instance that Tiptap attaches to its root element (el.editor).
    // Without such an instance it pastes the text, or, with pasteFallback false,
    // returns false so that the text is typed instead.
    const buildSetTiptapTextScript = (inputSelector, text, pasteFallback = true) => `(() => {
        const el = document.querySelector(${JSON.stringify(inputSelector)});
        const editor = el && el.editor;
        if (!editor || !editor.commands || typeof editor.commands.setContent !== 'function')
            return ${pasteFallback ? buildPasteTextScript(inputSelector, text) : 'false'};
        const lines = ${JSON.stringify(text)}.split('\\n');
        try {
            editor.commands.setContent({
                type: 'doc',
                content: lines.map(line => line ? { type: 'paragraph', content: [{ type: 'text', text: line }] } : { type: 'paragraph' })
            }, true);
            editor.commands.focus('end');
        } catch (e) {
            return false;
        }
        return true;
    })()`;

    // Evaluated in the chat page: replaces the content of a Quill message box
    // (.ql-editor) with the text. Uses the Quill instance that Quill 1.x attaches
    // to its container (__quill); otherwise inserts the text as typed input
    // (execCommand insertText), which Quill picks up from the DOM. Text is not
    // pasted, because a long paste may be turned into an attachment.
    const buildSetQuillTextScript = (inputSelector, text) => `(() => {
        const el = document.querySelector(${JSON.stringify(inputSelector)});
        if (!el || !el.isContentEditable) return false;
        const text = ${JSON.stringify(text)};
        const container = el.closest('.ql-container') || el.parentElement;
        const quill = container && container.__quill;
        try {
            if (quill && typeof quill.setText === 'function') {
                quill.setText(text, 'user');
                quill.setSelection(quill.getLength(), 0, 'user');
                return true;
            }
            el.focus();
            document.execCommand('selectAll', false, null);
            return document.execCommand('insertText', false, text);
        } catch (e) {
            return false;
        }
    })()`;

    // Source of a function, evaluated in the chat page, that returns an answer
    // element as text: each code block (options.block) becomes a Markdown fence
    // with the language from its header (options.label, without its buttons)
    // and the code (the options.codeAttribute attribute on the block or inside
    // it, when the page keeps the exact code there; otherwise the line elements
    // options.lines, or the text of options.code); the elements matched by options.remove
    // (citations and the like) and all buttons are left out. A copy is measured
    // off-screen so that innerText keeps the line breaks of the rendered answer.
    const buildAnswerToTextFunction = (options) => `((root) => {
        const options = ${JSON.stringify(options)};
        const each = (node, selector, fn) => Array.prototype.forEach.call(node.querySelectorAll(selector), fn);
        const clone = root.cloneNode(true);
        if (options.remove) each(clone, options.remove, e => e.remove());
        if (options.block) each(clone, options.block, block => {
            const header = options.label ? block.querySelector(options.label) : null;
            let language = '';
            if (header) {
                const label = header.cloneNode(true);
                each(label, '[role="button"], button', b => b.remove());
                language = (label.textContent.trim().split(/\\s+/)[0] || '').toLowerCase();
                // A label that is not a language name (such as "일반", plain text in
                // the page's language) is left out.
                if (!/^[a-z0-9+#._-]+$/.test(language)) language = '';
                header.remove();
            }
            // The code is read before buttons are removed: some pages wrap a collapsed
            // code block in a role=button element ("See all 13 lines").
            const source = options.codeAttribute
                ? (block.hasAttribute(options.codeAttribute) ? block : block.querySelector('[' + options.codeAttribute + ']'))
                : null;
            // options.monaco: a read-only Monaco editor ([data-uri] of its model);
            // the page's monaco global, when it has one, holds the whole code.
            let monacoCode = null;
            if (options.monaco && window.monaco && window.monaco.editor) {
                const host = block.querySelector('[data-uri]');
                try {
                    const model = host && window.monaco.editor.getModel(window.monaco.Uri.parse(host.getAttribute('data-uri')));
                    if (model) monacoCode = model.getValue();
                } catch (e) {}
            }
            // options.lines: a code viewer that shows each line as its own element
            // (empty lines hold a no-break space), in the order of their position
            // (Monaco places its lines with style.top).
            const lines = options.lines
                ? Array.prototype.slice.call(block.querySelectorAll(options.lines))
                    .map((l, i) => ({ l: l, i: i, top: parseFloat(l.style.top) }))
                    .sort((a, b) => (isNaN(a.top) || isNaN(b.top) ? 0 : a.top - b.top) || a.i - b.i)
                    .map(x => x.l)
                : [];
            const codeElement = options.code ? block.querySelector(options.code) : null;
            let code;
            if (source) {
                code = source.getAttribute(options.codeAttribute);
            } else if (monacoCode !== null) {
                code = monacoCode;
            } else if (lines.length) {
                code = Array.prototype.map.call(lines, l => l.textContent.replace(/\\u00a0/g, ' ').replace(/^\\s+$/, '')).join('\\n');
            } else if (codeElement) {
                code = codeElement.textContent;
            } else {
                each(block, '[role="button"], button', b => b.remove());
                code = block.textContent;
            }
            const fence = document.createElement('pre');
            fence.textContent = '\`\`\`' + language + '\\n' + code.replace(/\\n$/, '') + '\\n\`\`\`';
            block.parentNode.replaceChild(fence, block);
        });
        each(clone, '[role="button"], button', b => b.remove());
        const holder = document.createElement('div');
        holder.style.cssText = 'position:fixed;left:-100000px;top:0;width:800px;';
        holder.appendChild(clone);
        document.body.appendChild(holder);
        const text = clone.innerText;
        document.body.removeChild(holder);
        return text.trim();
    })`;

    const WEB_CHATS = {
        // Microsoft 365 Copilot chat (m365.cloud.microsoft/chat):
        // - message box: span#m365-chat-editor-target-element, a Lexical
        //   contenteditable. Enter sends; the send button is button[type=submit]
        //   near the box (there is no form).
        // - each turn: [data-testid=m365-chat-llm-web-ui-chat-message], holding the
        //   question ([data-testid=chatQuestion]) and the answer
        //   ([data-testid=copilot-message-div] > ... [data-testid=markdown-reply]).
        // - a finished answer shows its copy/feedback bar
        //   ([data-testid=CopyButtonContainerTestId]).
        // The consumer site (copilot.microsoft.com: textarea and
        // [data-content=ai-message]) is still recognized as a fallback.
        // Text is set through Lexical rather than pasted, because Copilot may turn
        // long pasted text into an attachment.
        copilot: {
            name: 'Copilot',
            url: 'https://m365.cloud.microsoft/chat',
            hosts: ['m365.cloud.microsoft', 'copilot.cloud.microsoft', 'copilot.microsoft.com'],
            preferredPath: /\/chat\b/,
            inputSelector: '#m365-chat-editor-target-element, textarea, [contenteditable="true"][role="textbox"]',
            submitSelector: 'button[type="submit"]',
            setText: buildSetLexicalTextScript,
            stateScript: `(() => {
                const turns = document.querySelectorAll('[data-testid="m365-chat-llm-web-ui-chat-message"]');
                if (turns.length || document.getElementById('m365-chat-editor-target-element')) {
                    const turn = turns.length ? turns[turns.length - 1] : null;
                    const reply = turn ? turn.querySelector('[data-testid="copilot-message-div"]') : null;
                    // A code block (#code-block-host-...) has a header with the language
                    // ([data-testid=one-copilot-code-identity]) and buttons, then a code
                    // viewer with one element per line ([data-line-index]).
                    const toText = ${buildAnswerToTextFunction({ block: '[id^="code-block-host-"]', label: '[data-testid="one-copilot-code-identity"]', lines: '[data-line-index]' })};
                    const parts = reply ? Array.prototype.map.call(reply.querySelectorAll('[data-testid="markdown-reply"]'), toText).filter(t => t) : [];
                    return {
                        count: turns.length,
                        text: parts.join('\\n\\n'),
                        done: !!(reply && reply.querySelector('[data-testid="CopyButtonContainerTestId"], [data-testid="FeedbackContainerTestId"]'))
                    };
                }
                const answers = document.querySelectorAll('[data-content=ai-message]');
                return { count: answers.length, text: answers.length ? answers[answers.length - 1].innerText.trim() : '', done: false };
            })()`
        },

        // ChatGPT (chatgpt.com):
        // - message box: a ProseMirror contenteditable (role=textbox) inside
        //   form[data-chatgpt-composer]. Enter sends; the send button,
        //   button[type=submit] (aria-label "보내기" in Korean), appears inside the
        //   form once the box has text (a voice button is shown while it is empty).
        // - each turn: [data-turn-key] in [data-thread-find-target=conversation];
        //   [data-talvt-turn-state] becomes "complete" when the answer is finished.
        // - the answer: [data-markdown-text-style=assistant-message] in the turn
        //   (after the [data-conversation-role=assistant] heading).
        chatgpt: {
            name: 'ChatGPT',
            url: 'https://chatgpt.com/',
            hosts: ['chatgpt.com', 'chat.openai.com'],
            preferredPath: null,
            inputSelector: 'form[data-chatgpt-composer] [contenteditable="true"], #prompt-textarea, form [contenteditable="true"][role="textbox"]',
            submitSelector: 'button[type="submit"], #composer-submit-button, [data-testid="send-button"]',
            setText: buildPasteTextScript,
            stateScript: `(() => {
                // A code block ([data-client-defined-widget=code_block]) has a header
                // with the language and buttons ([data-markdown-copy=exclude]) and keeps
                // the exact code in data-chatgpt-copy-code.
                const toText = ${buildAnswerToTextFunction({ block: '[data-client-defined-widget="code_block"]', label: '[data-markdown-copy="exclude"]', code: 'pre', codeAttribute: 'data-chatgpt-copy-code' })};
                const turns = document.querySelectorAll('[data-turn-key]');
                const turn = turns.length ? turns[turns.length - 1] : null;
                const parts = turn ? Array.prototype.map.call(turn.querySelectorAll('[data-markdown-text-style="assistant-message"]'), toText).filter(t => t) : [];
                const state = turn ? turn.querySelector('[data-talvt-turn-state]') : null;
                return {
                    count: turns.length,
                    text: parts.join('\\n\\n'),
                    done: !!(state && state.getAttribute('data-talvt-turn-state') === 'complete')
                };
            })()`
        },

        // Grok (grok.com):
        // - message box: a Tiptap (ProseMirror) contenteditable (role=textbox)
        //   inside form[data-composer]. Enter sends; the send button is
        //   button[type=submit][data-composer-action=send] in the form.
        // - each question: [data-testid=user-message] (in [data-user-message-row]);
        //   each answer: [data-testid=assistant-message], text in [data-markdown-row].
        // - a finished answer shows its action bar,
        //   [data-testid=response-footer-actions], next to the answer.
        // Text is set through Tiptap rather than pasted, so that long text is not
        // turned into an attachment and empty lines are kept.
        grok: {
            name: 'Grok',
            url: 'https://grok.com/',
            hosts: ['grok.com'],
            preferredPath: null,
            inputSelector: 'form[data-composer] [contenteditable="true"], form [contenteditable="true"][role="textbox"], form textarea:not([aria-hidden="true"])',
            submitSelector: 'button[data-composer-action="send"], button[type="submit"]',
            setText: buildSetTiptapTextScript,
            stateScript: `(() => {
                const questions = document.querySelectorAll('[data-testid="user-message"]');
                const answers = document.querySelectorAll('[data-testid="assistant-message"]');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                // A code block ([data-markdown-code]) has a header (language and copy
                // button) as its first child, then a pre.
                const toText = ${buildAnswerToTextFunction({ block: '[data-markdown-code]', label: ':scope > div:first-child', code: 'pre' })};
                const parts = answer ? Array.prototype.map.call(answer.querySelectorAll('[data-markdown-row]'), toText).filter(t => t) : [];
                return {
                    count: questions.length,
                    text: parts.join('\\n\\n'),
                    done: !!(answer && answer.parentElement && answer.parentElement.querySelector('[data-testid="response-footer-actions"]'))
                };
            })()`
        },

        // Gemini (gemini.google.com/app):
        // - message box: a Quill editor, div.ql-editor (role=textbox) in
        //   rich-textarea, inside input-area-v2 (there is no form). Enter sends;
        //   the send button is in [data-test-id=send-button-container]
        //   (gem-icon-button.send-button), shown once the box has text.
        // - each question: user-query; each answer: model-response, text in
        //   message-content.
        // - a finished answer has .response-footer.complete (and its
        //   message-actions bar).
        gemini: {
            name: 'Gemini',
            url: 'https://gemini.google.com/app',
            hosts: ['gemini.google.com'],
            preferredPath: /^\/app\b/,
            inputSelector: 'rich-textarea .ql-editor[contenteditable="true"], .ql-editor[contenteditable="true"][role="textbox"]',
            submitSelector: '[data-test-id="send-button-container"] button, .send-button button, button.send-button',
            setText: buildSetQuillTextScript,
            stateScript: `(() => {
                const questions = document.querySelectorAll('user-query');
                const answers = document.querySelectorAll('model-response');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                // A code block (code-block) has a header with the language and
                // download/copy buttons (.code-block-decoration), then the code.
                const toText = ${buildAnswerToTextFunction({ block: 'code-block', label: '.code-block-decoration', code: 'code[data-test-id="code-content"], pre' })};
                const parts = answer ? Array.prototype.map.call(answer.querySelectorAll('message-content'), toText).filter(t => t) : [];
                return {
                    count: questions.length,
                    text: parts.join('\\n\\n'),
                    done: !!(answer && answer.querySelector('.response-footer.complete, message-actions'))
                };
            })()`
        },

        // DeepSeek (chat.deepseek.com):
        // - message box: textarea[name=search]. Enter sends; the send button is a
        //   div[role=button].ds-button--primary.ds-button--circle, disabled with
        //   the ds-button--disabled class while the box is empty.
        // - options: .ds-toggle-button (aria-pressed) under the box: DeepThink
        //   ("깊은 생각" in Korean) and Search ("검색").
        // - messages: a virtual list of [data-virtual-list-item-key] items, each
        //   with a .ds-message. The key of a question is -N and of its answer N,
        //   increasing with each turn (older items may be removed from the DOM, so
        //   the largest key counts the turns). An answer has
        //   .ds-assistant-message-main-content (the DeepThink reasoning is
        //   outside it); its copy/regenerate buttons appear next to the message
        //   when it is finished.
        deepseek: {
            name: 'DeepSeek',
            url: 'https://chat.deepseek.com/',
            hosts: ['chat.deepseek.com'],
            preferredPath: null,
            inputSelector: 'textarea[name="search"], textarea#chat-input, textarea:not([aria-hidden="true"])',
            submitSelector: '[role="button"].ds-button--primary.ds-button--circle',
            setText: buildPasteTextScript,   // not used: a textarea is typed into
                maxWaitMs: 600000,
            // Turns on DeepThink when it is available and off. It is found by its
            // label (English, Chinese, Korean), so that another option such as
            // Search is never switched by mistake. Returns true when it was clicked.
            prepareScript: `(() => {
                const toggles = Array.prototype.slice.call(document.querySelectorAll('.ds-toggle-button'));
                const deepThink = toggles.find(t => /deep\\s*think|深度思考|깊은\\s*생각/i.test(t.innerText || ''));
                if (!deepThink || deepThink.getAttribute('aria-pressed') === 'true' || deepThink.getAttribute('aria-disabled') === 'true')
                    return false;
                deepThink.click();
                return true;
            })()`,
            stateScript: `(() => {
                const items = Array.prototype.slice.call(document.querySelectorAll('[data-virtual-list-item-key]'));
                const keys = items.map(i => Math.abs(parseInt(i.getAttribute('data-virtual-list-item-key'), 10))).filter(k => !isNaN(k));
                const messages = Array.prototype.slice.call(document.querySelectorAll('.ds-message'));
                const isAnswer = m => !!m.querySelector('.ds-assistant-message-main-content');
                const questions = messages.filter(m => !isAnswer(m));
                const answers = messages.filter(isAnswer);
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const content = answer ? answer.querySelector('.ds-assistant-message-main-content') : null;
                const item = answer ? (answer.closest('[data-virtual-list-item-key]') || answer.parentElement) : null;
                const actions = item ? Array.prototype.filter.call(item.querySelectorAll('[role="button"]'), b => !answer.contains(b)) : [];
                // A code block (.md-code-block) is a banner with the language and
                // copy/download buttons, then a pre.
                const toText = ${buildAnswerToTextFunction({ block: '.md-code-block', label: '.md-code-block-banner', code: 'pre' })};
                return {
                    count: keys.length ? Math.max.apply(null, keys) : questions.length,
                    text: content ? toText(content) : '',
                    done: actions.length > 0
                };
            })()`
        },

        // Perplexity (www.perplexity.ai):
        // - message box: a Lexical contenteditable ([data-lexical-editor=true],
        //   role=textbox). Enter sends; the send button is labelled "Submit"
        //   ("제출" in Korean) and is disabled while the box is empty.
        // - each question: [data-workflow-entry=N] with [data-message-bubble=right].
        // - each answer: [data-workflow-final-text] with the text in
        //   [data-renderer=lm]; [data-workflow-text-footer] (copy, share, ...)
        //   appears when it is finished.
        // - citations: [data-pplx-citation] chips inside the text, left out.
        // - code blocks: pre > figure > figcaption (language, wrap and copy
        //   buttons) and the code.
        perplexity: {
            name: 'Perplexity',
            url: 'https://www.perplexity.ai/',
            hosts: ['www.perplexity.ai', 'perplexity.ai'],
            preferredPath: null,
            inputSelector: '#ask-input, [data-lexical-editor="true"][contenteditable="true"]',
            submitSelector: 'button[data-testid="submit-button"], button[aria-label="Submit"], button[aria-label="제출"]',
            setText: buildSetLexicalTextScript,
            stateScript: `(() => {
                const toText = ${buildAnswerToTextFunction({ remove: '[data-pplx-citation], .citation-nbsp', block: 'pre', label: 'figcaption', code: 'code' })};
                const questions = document.querySelectorAll('[data-message-bubble="right"]');
                const entries = Array.prototype.map.call(document.querySelectorAll('[data-workflow-entry]'), e => parseInt(e.getAttribute('data-workflow-entry'), 10)).filter(n => !isNaN(n));
                const question = questions.length ? questions[questions.length - 1] : null;
                // The final text of the answer; without one yet, the last rendered
                // text that is not a question.
                const finals = document.querySelectorAll('[data-workflow-final-text] [data-renderer="lm"]');
                const texts = finals.length ? finals : Array.prototype.filter.call(document.querySelectorAll('[data-renderer="lm"]'), e => !e.closest('[data-message-bubble]'));
                let answer = texts.length ? texts[texts.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const container = answer ? answer.closest('[data-workflow-final-text]') : null;
                return {
                    count: Math.max(questions.length, entries.length ? Math.max.apply(null, entries) + 1 : 0),
                    text: answer ? toText(answer) : '',
                    done: !!(container && container.querySelector('[data-workflow-text-footer] button'))
                };
            })()`
        },

        // Meta AI (www.meta.ai):
        // - message box: a Lexical contenteditable,
        //   div[data-testid=composer-input][data-lexical-editor=true] (a textarea
        //   with the same test id stands in for it until the page is hydrated).
        //   Enter sends; the send button is [data-testid=composer-send-button].
        // - messages: [data-message-role=user|assistant] in the feed; an answer's
        //   [data-testid=assistant-message] has data-streaming-complete=true when
        //   it is finished. Its "Show thinking" status is left out.
        // - code blocks: [data-server-code-block]: a header (language, copy and
        //   download buttons), then a pre; a long one is collapsed ("See all N
        //   lines", button[aria-expanded=false]) and has only its first lines in
        //   the DOM until it is expanded.
        // - sometimes it offers two responses side by side ("Which response do you
        //   prefer?"), each in a column with a [data-scroll-container=true] and an
        //   "I prefer this response" button. The first response is read, and
        //   chosen once it is taken, so that the conversation goes on with it.
        metaai: {
            name: 'Meta AI',
            url: 'https://www.meta.ai/',
            hosts: ['www.meta.ai', 'meta.ai'],
            preferredPath: null,
            // Not the stand-in textarea: it comes first in the page, and text typed
            // into it is not sent.
            inputSelector: 'div[data-testid="composer-input"][contenteditable="true"]',
            submitSelector: '[data-testid="composer-send-button"]',
            setText: buildSetLexicalTextScript,
            stateScript: `(() => {
                const toText = ${buildAnswerToTextFunction({
                    remove: '[data-testid="thinking-status"], [data-testid="subagent-cot-list"], [data-testid^="sbs-response-actions"], img',
                    block: '[data-server-code-block]', label: ':scope > div > div:first-child', code: 'pre'
                })};
                const questions = document.querySelectorAll('[data-message-role="user"]');
                const answers = document.querySelectorAll('[data-message-role="assistant"]');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const message = answer ? (answer.querySelector('[data-testid="assistant-message"]') || answer) : null;
                // Two responses side by side: the first (visible) one.
                const columns = message ? Array.prototype.slice.call(message.querySelectorAll('[data-scroll-container="true"]')) : [];
                const column = columns.find(c => c.offsetParent !== null) || columns[0];
                const done = !!(message && message.getAttribute('data-streaming-complete') === 'true');
                // A finished answer: expand its collapsed code blocks ("See all N
                // lines"), which hold only their first lines until then. The text then
                // changes, so it is read again on the next poll.
                let expanded = 0;
                if (done)
                    Array.prototype.forEach.call(message.querySelectorAll('[data-server-code-block] button[aria-expanded="false"]'), b => { b.click(); expanded++; });
                return {
                    count: questions.length,
                    text: message ? toText(column || message) : '',
                    done: done && expanded === 0
                };
            })()`,
            finishScript: `(() => {
                const answers = document.querySelectorAll('[data-message-role="assistant"]');
                const answer = answers.length ? answers[answers.length - 1] : null;
                const columns = answer ? Array.prototype.slice.call(answer.querySelectorAll('[data-scroll-container="true"]')) : [];
                const column = columns.find(c => c.offsetParent !== null) || columns[0];
                // The "I prefer this response" button is a text button next to the
                // column (the icon buttons have an aria-label).
                const choose = column && column.parentElement && column.parentElement.querySelector('button[data-slot="button"]:not([aria-label])');
                if (!choose) return false;
                choose.click();
                return true;
            })()`
        },

        // Claude (claude.ai):
        // - message box: a contenteditable, [data-testid=chat-input]. Enter sends;
        //   the send button is [data-testid=chat-input-send], disabled while the box
        //   is empty.
        // - messages: a virtual list of [data-testid=transcript-row] items
        //   (data-index counts the messages; older rows may be removed from the
        //   DOM). A question is [data-testid=user-message]; an answer is
        //   [data-testid=assistant-message], with data-is-streaming=false when it
        //   is finished and its text in [data-perf-reply-text].
        // - code blocks: [data-not-prose] > div[role=group]: a copy button, the
        //   language ([data-find-omitted]), then a pre.
        // Text is typed rather than pasted (unless the box is a Tiptap editor),
        // because Claude turns long pasted text into an attachment.
        claude: {
            name: 'Claude',
            url: 'https://claude.ai/new',
            hosts: ['claude.ai'],
            preferredPath: null,
            inputSelector: '[data-testid="chat-input"][contenteditable="true"], div.ProseMirror[contenteditable="true"]',
            submitSelector: '[data-testid="chat-input-send"], button[aria-label="Send message"]',
            setText: (inputSelector, text) => buildSetTiptapTextScript(inputSelector, text, false),
            stateScript: `(() => {
                const toText = ${buildAnswerToTextFunction({ block: '[data-not-prose]:has(pre)', label: '[data-find-omitted]', code: 'pre' })};
                const rows = Array.prototype.map.call(document.querySelectorAll('[data-testid="transcript-row"][data-index]'), r => parseInt(r.getAttribute('data-index'), 10)).filter(n => !isNaN(n));
                const questions = document.querySelectorAll('[data-testid="user-message"]');
                const answers = document.querySelectorAll('[data-testid="assistant-message"]');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const parts = answer ? Array.prototype.map.call(answer.querySelectorAll('[data-perf-reply-text]'), toText).filter(t => t) : [];
                return {
                    count: Math.max(questions.length, rows.length ? Math.max.apply(null, rows) + 1 : 0),
                    text: parts.join('\\n\\n'),
                    done: !!(answer && answer.getAttribute('data-is-streaming') === 'false')
                };
            })()`
        },

        // Mistral Le Chat (chat.mistral.ai):
        // - message box: a ProseMirror contenteditable (div.rich-text-editor) in a
        //   form. Enter sends; the send button is the visible
        //   button[type=submit] of the form (a hidden one is also there).
        // - messages: [data-message-author-role=user|assistant]. An answer has
        //   reasoning parts ([data-message-part-type=reasoning], left out) and
        //   answer parts ([data-message-part-type=answer]); its action buttons
        //   (like, rewrite, copy, ...) appear below them when it is finished.
        // - code blocks: div[data-testid=code-block]: a header (language and copy
        //   button), then a pre. Inline code is code[data-testid=code-block], kept
        //   as text.
        // Text is typed rather than pasted (unless the box is a Tiptap editor), so
        // that long text is not turned into an attachment and empty lines are kept.
        mistral: {
            name: 'Mistral',
            url: 'https://chat.mistral.ai/chat',
            hosts: ['chat.mistral.ai'],
            preferredPath: null,
            inputSelector: 'div.rich-text-editor[contenteditable="true"], form [contenteditable="true"].ProseMirror',
            submitSelector: 'button[type="submit"]:not([hidden])',
            setText: (inputSelector, text) => buildSetTiptapTextScript(inputSelector, text, false),
            stateScript: `(() => {
                const toText = ${buildAnswerToTextFunction({ block: 'div[data-testid="code-block"]', label: ':scope > div > div:first-child', code: 'pre' })};
                const questions = document.querySelectorAll('[data-message-author-role="user"]');
                const answers = document.querySelectorAll('[data-message-author-role="assistant"]');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const parts = answer ? Array.prototype.map.call(answer.querySelectorAll('[data-message-part-type="answer"]'), toText).filter(t => t) : [];
                // The action buttons: outside the message parts, and not the triggers
                // of the collapsible reasoning steps.
                const actions = answer ? Array.prototype.filter.call(answer.querySelectorAll('button'), b => !b.closest('[data-message-part-type]') && !b.hasAttribute('aria-expanded')) : [];
                return {
                    count: questions.length,
                    text: parts.join('\\n\\n'),
                    done: actions.length > 0
                };
            })()`
        },

        // Qwen Chat (chat.qwen.ai):
        // - message box: textarea.message-input-textarea ("Ask Qwen"). Enter sends;
        //   the send button is button.send-button (aria-label "Send"), disabled
        //   while the box is empty.
        // - messages: .qwen-chat-message-user and .qwen-chat-message-assistant. An
        //   answer has a thinking status card (left out) and its text in
        //   .response-message-content.phase-answer; its footer actions
        //   (.response-message-footer-actions) appear when it is finished.
        // - code blocks: pre.qwen-markdown-code: a header (.qwen-markdown-code-header:
        //   language, copy, download and collapse buttons), then a read-only Monaco
        //   editor; the code is read from Monaco's model when the page exposes it,
        //   otherwise from its rendered lines (.view-line).
        qwen: {
            name: 'Qwen',
            url: 'https://chat.qwen.ai/',
            hosts: ['chat.qwen.ai'],
            preferredPath: null,
            inputSelector: 'textarea.message-input-textarea, #message-input-container textarea, textarea[placeholder="Ask Qwen"]',
            submitSelector: 'button.send-button, button[aria-label="Send"]',
            setText: buildPasteTextScript,   // not used: a textarea is typed into
            stateScript: `(() => {
                const toText = ${buildAnswerToTextFunction({ block: 'pre.qwen-markdown-code', label: '.qwen-markdown-code-header', monaco: true, lines: '.view-line', code: '.qwen-markdown-code-body' })};
                const questions = document.querySelectorAll('.qwen-chat-message-user');
                const answers = document.querySelectorAll('.qwen-chat-message-assistant');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const parts = answer ? Array.prototype.map.call(answer.querySelectorAll('.response-message-content.phase-answer'), toText).filter(t => t) : [];
                return {
                    count: questions.length,
                    text: parts.join('\\n\\n'),
                    done: !!(answer && answer.querySelector('.response-message-footer-actions'))
                };
            })()`
        },

        // Kimi (www.kimi.ai):
        // - message box: a Lexical contenteditable (.chat-input-editor). Enter
        //   sends; the send button is div.send-button-container, with the class
        //   "disabled" while the box is empty.
        // - messages: .chat-content-item-user and .chat-content-item-assistant. An
        //   answer is a .toolcall-rollup: the thinking and tool calls in
        //   .toolcall-flow (left out), then the text in .toolcall-rollup__tail
        //   .markdown; its actions (.segment-assistant-actions: read aloud, copy,
        //   ...) appear when it is finished.
        // - code blocks: .segment-code: a header with the language
        //   (.segment-code-lang) and a copy button, then a pre.
        kimi: {
            name: 'Kimi',
            // Opens the global site; the other domains have the same page and are
            // recognized when one of them is already open.
            url: 'https://www.kimi.ai/',
            hosts: ['www.kimi.ai', 'kimi.ai', 'www.kimi.com', 'kimi.com', 'kimi.moonshot.cn'],
            preferredPath: null,
            inputSelector: '.chat-input-editor[contenteditable="true"], [data-testid="chat-editor"] [contenteditable="true"]',
            submitSelector: '.send-button-container',
            setText: buildSetLexicalTextScript,
            stateScript: `(() => {
                const toText = ${buildAnswerToTextFunction({ block: '.segment-code', label: '.segment-code-lang', code: 'pre' })};
                const questions = document.querySelectorAll('.chat-content-item-user');
                const answers = document.querySelectorAll('.chat-content-item-assistant');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const texts = answer ? Array.prototype.filter.call(answer.querySelectorAll('.markdown'), m => !m.closest('.toolcall-flow') && !m.parentElement.closest('.markdown')) : [];
                const parts = texts.map(toText).filter(t => t);
                return {
                    count: questions.length,
                    text: parts.join('\\n\\n'),
                    done: !!(answer && answer.querySelector('.segment-assistant-actions-content .icon-button, .segment-assistant-actions-content button'))
                };
            })()`
        },

        // Z.ai (chat.z.ai):
        // - message box: textarea#chat-input. Enter sends; the send button is
        //   #send-message-button (button[type=submit]), disabled while the box is
        //   empty.
        // - messages: [id^=message-] items with .chat-user or .chat-assistant. An
        //   answer has its thinking in .thinking-chain-container (left out) and its
        //   text in .chat-markdown; its copy/regenerate buttons
        //   (.copy-response-button) appear below it when it is finished.
        // - code blocks: div[dir=ltr] around .app-code: the language and a copy
        //   button (.copy-code-button), then the code (pre.app-code-pre).
        zai: {
            name: 'Z.ai',
            url: 'https://chat.z.ai/',
            hosts: ['chat.z.ai'],
            preferredPath: null,
            inputSelector: 'textarea#chat-input',
            submitSelector: '#send-message-button, button[type="submit"]',
            setText: buildPasteTextScript,   // not used: a textarea is typed into
            stateScript: `(() => {
                const toText = ${buildAnswerToTextFunction({ block: 'div[dir="ltr"]:has(> div > .app-code)', label: ':scope > div:first-child', code: 'pre' })};
                const questions = document.querySelectorAll('.chat-user');
                const answers = document.querySelectorAll('.chat-assistant');
                const question = questions.length ? questions[questions.length - 1] : null;
                let answer = answers.length ? answers[answers.length - 1] : null;
                // Only an answer after the last question belongs to it.
                if (answer && question && !(question.compareDocumentPosition(answer) & Node.DOCUMENT_POSITION_FOLLOWING))
                    answer = null;
                const parts = answer ? Array.prototype.map.call(answer.querySelectorAll('.chat-markdown'), toText).filter(t => t) : [];
                const message = answer ? (answer.closest('[id^="message-"]') || answer.parentElement) : null;
                return {
                    count: questions.length,
                    text: parts.join('\\n\\n'),
                    done: !!(message && message.querySelector('.copy-response-button, .regenerate-response-button'))
                };
            })()`
        }
    };

    // The services without their page scripts: {key: {name, url, hosts, preferredPath,
    // maxWaitMs}}. preferredPath is the source of a regular expression for the path of
    // the chat page (the launcher prefers an open page whose path matches it).
    const describe = () => {
        const result = {};
        Object.keys(WEB_CHATS).forEach(key => {
            const service = WEB_CHATS[key];
            result[key] = {
                name: service.name,
                url: service.url,
                hosts: service.hosts,
                preferredPath: service.preferredPath ? service.preferredPath.source : null,
                maxWaitMs: service.maxWaitMs || null
            };
        });
        return result;
    };

    // The page scripts of one turn: the selectors, the script that puts the text into the
    // message box (setTextScript), and the scripts that prepare the page, read its state
    // ({count, text, done}) and finish the turn.
    const turn = (key, text) => {
        const service = WEB_CHATS[key];
        if (!service) throw new Error('Unknown web chat service: ' + key);
        return {
            inputSelector: service.inputSelector,
            submitSelector: service.submitSelector,
            setTextScript: service.setText(service.inputSelector, text),
            prepareScript: service.prepareScript || null,
            stateScript: service.stateScript,
            finishScript: service.finishScript || null
        };
    };

    return { services: WEB_CHATS, describe: describe, turn: turn };
})();
